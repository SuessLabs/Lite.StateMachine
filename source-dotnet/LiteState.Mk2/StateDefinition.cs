// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace LiteState.Mk2;


public enum StateId
{
  Root,
  Loading,
  Processing,
  SubProcessing,
  Completed,
  Error
}

public class StateDefinition
{
  public StateId Id { get; }
  public Func<Dictionary<string, object>, Task>? OnEntering { get; set; }
  public Func<Dictionary<string, object>, Task>? OnEnter { get; set; }
  public Func<string, Dictionary<string, object>, Task>? OnMessage { get; set; }
  public Func<Dictionary<string, object>, Task>? OnTimeout { get; set; }
  public Func<Dictionary<string, object>, Task>? OnExit { get; set; }

  public StateDefinition(StateId id) => Id = id;
}

public class CompositeState : StateDefinition
{
  public Dictionary<StateId, StateDefinition> SubStates { get; } = new();
  public StateId? InitialSubState { get; set; }

  public CompositeState(StateId id) : base(id)
  {
  }

  public void AddSubState(StateDefinition state) => SubStates[state.Id] = state;
}
