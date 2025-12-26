// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.State;

using System;

/// <summary>A base class for composite states. The submachine is injected/assigned externally.</summary>
/// <typeparam name="TState">Type of state.</typeparam>
public abstract class CompositeState<TState> : BaseState<TState>, ICompositeState<TState>
  where TState : struct, Enum
{
  protected CompositeState()
    : base()
  {
  }

  protected CompositeState(TState id)
    : base(id)
  {
  }

  public override bool IsComposite => true;

  /// <inheritdoc/>
  ////public StateMachine<TState> Submachine { get; internal set; } = default!;
  public StateMachine<TState> Submachine { get; set; } = default!;
}
