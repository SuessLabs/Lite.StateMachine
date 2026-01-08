// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Lite.StateMachine;

public sealed class EventAggregator : IEventAggregator
{
  //// Pre .NET 9: private readonly object _lockGate = new();
  private readonly System.Threading.Lock _lockGate = new();

  // Typed subscribers keyed by exact runtime Type
  private readonly Dictionary<Type, List<Action<object>>> _typedSubscribers = [];

  // Wildcard subscribers (receive all messages)
  private readonly List<Action<object>> _wildcardSubscribers = [];

  public void Publish(object message)
  {
    if (message is null)
      return;

    Action<object>[] wildcardSnapshot;
    Action<object>[] typedSnapshot;

    var msgType = message.GetType();

    lock (_lockGate)
    {
      wildcardSnapshot = _wildcardSubscribers.ToArray();
      if (_typedSubscribers.TryGetValue(msgType, out var list))
        typedSnapshot = list.ToArray();
      else
        typedSnapshot = [];
    }

    // Deliver to typed subscribers first (exact matches), then wildcard
    // Handlers decide whether to consume or ignore; exceptions are swallowed.
#pragma warning disable SA1501 // Statement should not be on a single line
    foreach (var sub in typedSnapshot)
    {
      try { sub(message); }
      catch { /* Swallow to avoid breaking publication loop. */ }
    }

    foreach (var sub in wildcardSnapshot)
    {
      try { sub(message); }
      catch { /* Swallow to avoid breaking publication loop. */ }
    }
#pragma warning restore SA1501 // Statement should not be on a single line
  }

  public IDisposable Subscribe(Action<object> handler)
  {
    ArgumentNullException.ThrowIfNull(handler);
    lock (_lockGate)
      _wildcardSubscribers.Add(handler);

    return new Subscription(() =>
    {
      lock (_lockGate)
        _wildcardSubscribers.Remove(handler);
    });
  }

  public IDisposable Subscribe(Action<object> handler, params Type[] messageTypes)
  {
    ArgumentNullException.ThrowIfNull(handler);
    messageTypes ??= [];

    if (messageTypes.Length == 0)
    {
      // No types specified -> treat as wildcard to preserve backward compatibility
      return Subscribe(handler);
    }

    // Register handler under each provided type
    lock (_lockGate)
    {
      foreach (var t in messageTypes)
      {
        if (t is null)
          continue;

        if (!_typedSubscribers.TryGetValue(t, out var list))
        {
          list = [];
          _typedSubscribers[t] = list;
        }

        list.Add(handler);
      }
    }

    // Composite unsubscribe removes handler from each type list
    return new Subscription(() =>
    {
      lock (_lockGate)
      {
        foreach (var t in messageTypes)
        {
          if (t is null)
            continue;

          if (_typedSubscribers.TryGetValue(t, out var list))
          {
            list.Remove(handler);
            if (list.Count == 0)
              _typedSubscribers.Remove(t);
          }
        }
      }
    });
  }

  private sealed class Subscription : IDisposable
  {
    private readonly Action _unsubscribe;
    private bool _disposed;

    public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;

    public void Dispose()
    {
      if (_disposed) return;
      _disposed = true;
      _unsubscribe();
    }
  }
}
