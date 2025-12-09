// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.State;

using System;

/// <summary>
/// A base class for composite states. The submachine is injected/assigned externally.
/// </summary>
public abstract class CompositeState<TState> : BaseState<TState>, ICompositeState<TState> where TState : struct, Enum
{
  protected CompositeState(TState id) : base(id)
  {
    // : base(name, logger)
  }

  public override bool IsComposite => true;

  public StateMachine<TState> Submachine { get; internal set; } = default!;
}
