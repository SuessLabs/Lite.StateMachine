// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

/*
 * Make a state machine using C# and has state transitions named OnEntering, OnEnter, OnExit
 * that must be methods and returns a boolean to signify if the state passed or failed. Each
 * state is identified as an custom enum and the state machine is agnostic to the type of enum
 * provided. The state machine can have composite states, when the last sub-state exits it
 * goes to it's parent state's OnExit transition. Each state gets passed a Context object that
 * contains a string property named "Parameter" and a method named, "NextState" to trigger
 * moving to the next state.
 */
namespace LiteState.Mk4a;

using System;
using System.Collections.Generic;

/// <summary>
/// Contract for a state with lifecycle transitions.
/// Each state is identified by an enum value.
/// </summary>
public interface IState<TState> where TState : struct, Enum
{
  TState Id { get; }

  /// <summary>Called immediately after entering the state.</summary>
  bool OnEnter(Context<TState> ctx);

  /// <summary>Called right before entering the state.</summary>
  bool OnEntering(Context<TState> ctx);

  /// <summary>Called right before leaving the state.</summary>
  bool OnExit(Context<TState> ctx);
}

/// <summary>
/// A composite (hierarchical) state: parent transitions + an ordered list of sub-states.
/// Execution order:
///  1) Parent.OnEntering
///  2) Parent.OnEnter
///  3) For each child: Child.OnEntering -> Child.OnEnter -> Child.OnExit
///  4) Parent.OnExit (after the last child exits)
/// </summary>
public class CompositeState<TState> : State<TState> where TState : struct, Enum
{
  private readonly List<IState<TState>> _children = new();

  public CompositeState(TState id) : base(id)
  {
  }

  public IReadOnlyList<IState<TState>> Children => _children;

  public CompositeState<TState> AddChild(IState<TState> child)
  {
    if (child == null) throw new ArgumentNullException(nameof(child));
    _children.Add(child);
    return this;
  }
}

/// <summary>
/// Context passed to every transition hook.
/// Contains the required "Parameter" and a "NextState" trigger.
/// </summary>
public sealed class Context<TState> where TState : struct, Enum
{
  private readonly StateMachine<TState> _machine;

  internal Context(StateMachine<TState> machine, string parameter)
  {
    _machine = machine ?? throw new ArgumentNullException(nameof(machine));
    Parameter = parameter;
  }

  /// <summary>
  /// Arbitrary parameter supplied by caller for the run.
  /// </summary>
  public string Parameter { get; set; }

  /// <summary>
  /// Request a transition to the provided state.
  /// The transition occurs after the current state's OnExit (or the composite parent's OnExit).
  /// </summary>
  public void NextState(TState next) => _machine.RequestTransition(next);
}

/// <summary>
/// Convenience base class with default "true" transitions.
/// </summary>
public abstract class State<TState> : IState<TState> where TState : struct, Enum
{
  protected State(TState id) => Id = id;

  public TState Id { get; }

  public virtual bool OnEnter(Context<TState> ctx) => true;

  public virtual bool OnEntering(Context<TState> ctx) => true;

  public virtual bool OnExit(Context<TState> ctx) => true;
}

/// <summary>
/// Generic enum-driven state machine with hierarchical (composite) state support.
/// </summary>
public class StateMachine<TState> where TState : struct, Enum
{
  private readonly Dictionary<TState, IState<TState>> _states = new();
  private TState? _requestedNext;

  /// <summary>Register a state (simple or composite).</summary>
  public void Register(IState<TState> state)
  {
    if (state == null) throw new ArgumentNullException(nameof(state));
    _states[state.Id] = state;
  }

  /// <summary>
  /// Run the state machine starting at "initialState".
  /// Returns true if the run completes successfully (no transition returned false).
  /// </summary>
  public bool Run(TState initialState, string parameter)
  {
    if (!_states.TryGetValue(initialState, out var start))
      throw new InvalidOperationException($"State '{initialState}' is not registered.");

    var ctx = new Context<TState>(this, parameter);
    return ExecuteStateGraph(start, ctx);
  }

  internal void RequestTransition(TState next) => _requestedNext = next;

  private bool ExecuteStateGraph(IState<TState> state, Context<TState> ctx)
  {
    _requestedNext = null;

    // Call the parent's entering + enter hooks first.
    if (!state.OnEntering(ctx)) return false;
    if (!state.OnEnter(ctx)) return false;

    bool ok;

    if (state is CompositeState<TState> composite)
    {
      // Execute each child in order: OnEntering -> OnEnter -> OnExit
      foreach (var child in composite.Children)
      {
        // Note: Children don't need to be in the registry unless you intend to transition to them by enum.
        ok = child.OnEntering(ctx);
        if (!ok)
          return false;

        ok = child.OnEnter(ctx);
        if (!ok)
          return false;

        ok = child.OnExit(ctx);
        if (!ok)
          return false;
      }

      // After the last sub-state exits, go to the parent's OnExit.
      ok = state.OnExit(ctx);
      if (!ok)
        return false;
    }
    else
    {
      // Simple state: call OnExit.
      ok = state.OnExit(ctx);
      if (!ok)
        return false;
    }

    // If a transition was requested at any point, perform it now.
    if (_requestedNext is { } next)
    {
      if (!_states.TryGetValue(next, out var target))
        throw new InvalidOperationException($"Requested next state '{next}' is not registered.");

      return ExecuteStateGraph(target, ctx);
    }

    // No next state requested -> successful completion.
    return true;
  }
}
