// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Lite.State;

public sealed class EventAggregator : IEventAggregator
{
  private readonly List<Func<object, bool>> _subscribers = new();

  public void Publish(object message)
  {
    // Fan-out; handlers decide whether to consume or ignore.
    foreach (var sub in _subscribers.ToArray())
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
    _subscribers.Add(handler);

    return new Subscription(() => _subscribers.Remove(handler));
  }

  private sealed class Subscription : IDisposable
  {
    private readonly Action _unsubscribe;
    private int _disposed;

    public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;

    public void Dispose()
    {
      if (Interlocked.Exchange(ref _disposed, 1) == 0)
        _unsubscribe();
    }
  }
}
