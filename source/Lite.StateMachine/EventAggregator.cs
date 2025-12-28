// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Lite.StateMachine;

public sealed class EventAggregator : IEventAggregator
{
  private readonly object _lockGate = new();
  private readonly List<Func<object, bool>> _subscribers = new();

  public void Publish(object message)
  {
    Func<object, bool>[] snapshot;
    lock (_lockGate)
      snapshot = _subscribers.ToArray();

    // Fan-out; handlers decide whether to consume or ignore.
    foreach (var sub in snapshot) // _subscribers.ToArray())
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

  public IDisposable Subscribe(Func<object, bool> handler)
  {
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
