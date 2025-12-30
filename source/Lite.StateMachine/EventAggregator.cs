// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Lite.StateMachine;

public sealed class EventAggregator : IEventAggregator
{
  //// Pre .NET 9: private readonly object _lockGate = new();
  private readonly System.Threading.Lock _lockGate = new();

  ////private readonly List<Func<object, bool>> _subscribers = new();
  private readonly List<Action<object>> _subscribers = [];

  public void Publish(object message)
  {
    Action<object>[] snapshot;
    lock (_lockGate)
      snapshot = _subscribers.ToArray();

    // Fan-out; handlers decide whether to consume or ignore.
    foreach (var sub in snapshot)
    {
      try
      {
        sub(message);
      }
      catch
      {
        // Swallow to avoid breaking publication loop.
      }
    }
  }

  public IDisposable Subscribe(Action<object> handler)
  {
    // Was: Func<object, bool> handler)
    ArgumentNullException.ThrowIfNull(handler);

    lock (_lockGate)
      _subscribers.Add(handler);

    return new Subscription(() => _subscribers.Remove(handler));
  }

  private sealed class Subscription : IDisposable
  {
    private readonly Action _unsubscribe;
    private bool _disposed;

    public Subscription(Action unsubscribe) =>
      _unsubscribe = unsubscribe;

    public void Dispose()
    {
      if (_disposed)
        return;

      _disposed = true;
      _unsubscribe();
    }
  }
}
