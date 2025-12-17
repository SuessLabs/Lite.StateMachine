// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.State;

/// <summary>Command-state interface: receives messages and can time out.</summary>
public interface ICommandState<TState> : IState<TState> where TState : struct, Enum
{
  /// <summary>
  /// Optional message filter; return true to deliver to this state, false to ignore.
  /// Default: accept all messages.
  /// </summary>
  Func<object, bool> MessageFilter => _ => true;

  /// <summary>Optional override of timeout for this state; null uses machine default.</summary>
  int? TimeoutMs => null;

  /// <summary>Receives a message from the event aggregator.</summary>
  void OnMessage(Context<TState> context, object message);

  /// <summary>Fires when no messages are received within the timeout window.</summary>
  void OnTimeout(Context<TState> context);
}
