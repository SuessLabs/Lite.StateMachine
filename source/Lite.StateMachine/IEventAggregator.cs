// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.StateMachine;

/// <summary>Simple event aggregator for delivering messages to the current command state.</summary>
public interface IEventAggregator
{
  /// <summary>Publish a message to subscribers.</summary>
  /// <param name="message">Message object to publish.</param>
  void Publish(object message);

  /// <summary>Subscribe to all messages (wildcard).</summary>
  /// <param name="handler">Subscription listener method.</param>
  /// <returns>Disposable subscription.</returns>
  IDisposable Subscribe(Action<object> handler);

  /// <summary>
  ///   Subscribe to one or more specific message types. Only messages whose
  ///   runtime type matches one of the provided <paramref name="messageTypes"/>
  ///   will be delivered to <paramref name="handler"/>.
  /// </summary>
  /// <param name="handler">Subscription listener method.</param>
  /// <param name="messageTypes">Message type(s) to subscribe to.</param>
  /// <returns>Disposable subscription.</returns>
  IDisposable Subscribe(Action<object> handler, params Type[] messageTypes);

  /// <summary>Generic convenience overload.</summary>
  /// <param name="message">Message object to publish (not null).</param>
  /// <typeparam name="T">Type to publish.</typeparam>
  void Publish<T>(T message) => Publish((object)message!);
}
