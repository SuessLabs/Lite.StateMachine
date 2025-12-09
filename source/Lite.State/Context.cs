// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.State;

using System;

/// <summary>
/// Context passed to every state. Provides a "Parameter" and a NextState(Result) trigger.
/// </summary>
public sealed class Context<TState> where TState : struct, Enum
{
  private readonly StateMachine<TState> _machine;

  internal Context(StateMachine<TState> machine) => _machine = machine;

  /// <summary>Arbitrary collection of errors to pass along to the next state.</summary>
  public PropertyBag ErrorStack { get; set; } = [];

  /// <summary>The previous state's enum value.</summary>
  public TState LastState { get; internal set; }

  /// <summary>Arbitrary parameter provided by caller to the current action.</summary>
  public PropertyBag Parameters { get; set; } = [];

  /// <summary>
  ///   Signals transitioning by outcome. This uses the current state's mapping,
  ///   and if none exists locally (composite sub-state machine exhausted),
  ///   it bubbles to the parent state's OnExit and applies the parent's mapping.
  /// </summary>
  public void NextState(Result result) =>
    _machine.InternalNextState(result);
}
