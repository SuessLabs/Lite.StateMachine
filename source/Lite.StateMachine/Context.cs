// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine;

using System;

/// <summary>Context passed to every state. Provides a "Parameter" and a NextState(Result) trigger.</summary>
/// <typeparam name="TState">Type of state enum.</typeparam>
public sealed class Context<TState>
  where TState : struct, Enum
{
  private readonly StateMachine<TState> _machine;

  internal Context(StateMachine<TState> machine)
  {
    _machine = machine;
    ////EventAggregator = eventAggregator;
  }

  /// <summary>Gets or sets an arbitrary collection of errors to pass along to the next state.</summary>
  public PropertyBag ErrorStack { get; set; } = [];

  /// <summary>Gets the event aggregator for command states (optional).</summary>
  ////public IEventAggregator? Events { get; }

  /// <summary>Gets the previous state's enum value.</summary>
  public TState LastState { get; internal set; }

  /// <summary>Gets or sets an arbitrary parameter provided by caller to the current action.</summary>
  public PropertyBag Parameters { get; set; } = [];

  /// <summary>
  ///   Signals transitioning by outcome. This uses the current state's mapping,
  ///   and if none exists locally (composite sub-state machine exhausted),
  ///   it bubbles to the parent state's OnExit and applies the parent's mapping.
  /// </summary>
  /// <param name="result">The result outcome to trigger transition.</param>
  /// <remarks>
  ///   NOTE (2025-12-25 DS):
  ///     Consider NOT calling 'InternalNextState()' and allow the 'StateMachine.EnterState's state.OnEnter(Context)
  ///     to finish, then call, "InternalNextState" after substates run their corse.
  /// </remarks>
  public void NextState(Result result) =>
    _machine.InternalNextState(result);

  /// <summary>Get default parameter value as <see cref="int"/> or default.</summary>
  /// <param name="key">Parameter Key.</param>
  /// <param name="defaultInt">Default int (default=0).</param>
  /// <returns>Integer or default.</returns>
  public int ParameterAsInt(string key, int defaultInt = 0)
  {
    if (Parameters.TryGetValue(key, out var value) && value is int intValue)
      return intValue;
    else
      return defaultInt;
  }
}
