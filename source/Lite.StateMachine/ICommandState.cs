// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lite.StateMachine;

/// <summary>Command-state interface: receives messages from subscriptions and can timeout.</summary>
/// <typeparam name="TStateId">Type of State Id.</typeparam>
public interface ICommandState<TStateId> : IState<TStateId>
  where TStateId : struct, Enum
{
  /// <summary>
  ///   Gets the declared the message types this command state wants to receive.
  ///   Return multiple types to subscribe to all of them.
  ///   Return an empty collection (default) to receive all messages (wildcard).
  /// </summary
  /// <remarks>Initialize to <![CDATA[Array.Empty<Type>();]]> or NULL when not in use.</remarks>
  IReadOnlyCollection<Type> SubscribedMessageTypes => [];

  /// <summary>Gets optional override of timeout for this state; null uses machine default.</summary>
  int? TimeoutMs => null;

  /// <summary>Receives a message from the event aggregator.</summary>
  /// <param name="context">State context.</param>
  /// <param name="message">Message object.</param>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task OnMessage(Context<TStateId> context, object message);

  /// <summary>Fires when no messages are received within the timeout window.</summary>
  /// <param name="context">State context.</param>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task OnTimeout(Context<TStateId> context);
}
