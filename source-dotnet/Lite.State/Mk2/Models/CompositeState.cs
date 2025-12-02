// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
/*
namespace LiteState.Mk2.Models;

using System.Collections.Generic;
using LiteState.Mk2.Interfaces;

public class CompositeState : StateDefinition, ICompositeState
{
  public Dictionary<StateId, IState> SubStates { get; } = new();

  public StateId? InitialSubState { get; set; }

  IReadOnlyDictionary<StateId, IState> ICompositeState.SubStates => SubStates;

  public CompositeState(StateId id) : base(id)
  {
  }

  public void AddSubState(IState state) => SubStates[state.Id] = state;
}
*/
