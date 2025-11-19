// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace LiteState;

/// <summary>
/// Base state interface: every state must implement OnEnter and OnExit.
/// </summary>
public interface IState
{
  string Name { get; }

  void OnEnter(StateMachine fsm);

  void OnExit(StateMachine fsm);
}

/// <summary>
/// Optional interface for states that want a timeout.
/// Implement OnTimeout and provide Timeout (TimeSpan).
/// The FSM starts a one-shot timer when the state is entered; when the timer expires
/// and the state is still active, OnTimeout is invoked.
/// </summary>
public interface IStateWithTimeout : IState
{
  TimeSpan Timeout { get; }

  void OnTimeout(StateMachine fsm);
}
