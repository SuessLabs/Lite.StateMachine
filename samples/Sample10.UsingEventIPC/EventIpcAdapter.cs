// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IpcEvents = Lite.EventIpc.IEventAggregator;

namespace Sample10.UsingEventIPC;

/// <summary>Adapts the sample's known event types to the state machine's event contract.</summary>
internal sealed class EventIpcAdapter(IpcEvents events) : Lite.StateMachine.IEventAggregator
{
  private readonly Dictionary<Type, MessageBinding> _bindings = [];

  public void Publish(object message)
  {
    if (message is null)
      return;

    // Publishing as object directly through EventIPC would select object
    // subscribers, so dispatch using the message's exact runtime type.
    GetBinding(message.GetType()).Publish(message);
  }

  // Register all message types before starting the machine. This also defines
  // the universe of messages delivered to a wildcard subscription.
  public EventIpcAdapter RegisterMessageType<T>()
  {
    _bindings.Add(typeof(T), new MessageBinding(
      message => events.Publish((T)message),
      handler =>
      {
        Action<T> forward = message => handler(message!);
        events.Subscribe(forward);

        // Keep the delegate alive: Lite.EventIpc stores weak references.
        return new Subscription(() => events.Unsubscribe(forward));
      }));
    return this;
  }

  public IDisposable Subscribe(Action<object> handler) => Subscribe(handler, []);

  public IDisposable Subscribe(Action<object> handler, params Type[] messageTypes)
  {
    ArgumentNullException.ThrowIfNull(handler);
    Type[] types1 = [.. _bindings.Keys];
    var types = messageTypes is null || messageTypes.Length == 0
      ? types1
      : [.. messageTypes.Where(type => type is not null).Distinct()];

    // Validate before subscribing so an unknown type cannot leak subscriptions.
    var bindings = types.Select(GetBinding).ToArray();
    var subscriptions = bindings.Select(binding => binding.Subscribe(message =>
    {
      // Match the built-in aggregator's exception isolation.
      try
      {
        handler(message);
      }
      catch
      {
        // One subscriber must not interrupt publication to other subscribers.
      }
    })).ToArray();

    return new Subscription(() =>
    {
      foreach (var subscription in subscriptions)
        subscription.Dispose();
    });
  }

  private MessageBinding GetBinding(Type type) =>
    _bindings.TryGetValue(type, out var binding)
      ? binding
      : throw new NotSupportedException($"Register message type '{type.Name}' with RegisterMessageType<T>() first.");

  private sealed record MessageBinding(Action<object> Publish, Func<Action<object>, IDisposable> Subscribe);

  private sealed class Subscription(Action unsubscribe) : IDisposable
  {
    private Action? _unsubscribe = unsubscribe;

    public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
  }
}
