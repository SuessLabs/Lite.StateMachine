// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.StateMachine;

/// <summary>Command-state interface: receives messages and can time out.</summary>
/// <typeparam name="TStateId">Type of State Id.</typeparam>
public interface ICommandState<TStateId> : IState<TStateId>
  where TStateId : struct, Enum
{
  /// <summary>Gets optional override of timeout for this state; null uses machine default.</summary>
  int? TimeoutMs => null;

  /// <summary>Receives a message from the event aggregator.</summary>
  /// <param name="context">State context.</param>
  /// <param name="message">Message object.</param>
  void OnMessage(Context<TStateId> context, object message);

  /// <summary>Fires when no messages are received within the timeout window.</summary>
  /// <param name="context">State context.</param>
  void OnTimeout(Context<TStateId> context);
}
