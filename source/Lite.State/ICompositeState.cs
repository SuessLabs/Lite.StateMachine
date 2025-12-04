// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.State;

/// <summary>
/// Composite (hierarchical) state interface: has an owned submachine.
/// </summary>
public interface ICompositeState<TState> : IState<TState> where TState : struct, Enum
{
  // TODO: Rename to 'SubStates'
  StateMachine<TState> Submachine { get; }
}
