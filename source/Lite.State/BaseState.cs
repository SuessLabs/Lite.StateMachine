// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Lite.State;

/// <summary>A simple base implementation for states with convenient transition builder.</summary>
/// <typeparam name="TState">Type of state.</typeparam>
/// <remarks>Consider renaming to, 'State'.</remarks>
public abstract class BaseState<TState> : IState<TState>
  where TState : struct, Enum
{
  private readonly Dictionary<Result, TState> _transitions = new();

  protected BaseState()
  {
  }

  protected BaseState(TState id) => Id = id;

  /// <inheritdoc/>
  public TState Id { get; private set; }

  /// <inheritdoc/>
  public virtual bool IsComposite => false;

  /// <inheritdoc/>
  public IReadOnlyDictionary<Result, TState> Transitions => _transitions;

  public void AddTransition(Result outcome, TState target)
  {
    _transitions[outcome] = target;
  }

  /// <inheritdoc/>
  public virtual void OnEnter(Context<TState> context)
  {
  }

  /// <inheritdoc/>
  public virtual void OnEntering(Context<TState> context)
  {
  }

  /// <inheritdoc/>
  public virtual void OnExit(Context<TState> context)
  {
  }

  public void SetStateId(TState id) => Id = id;
}
