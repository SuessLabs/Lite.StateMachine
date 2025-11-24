// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

public class Context : Dictionary<string, object>
{
  // Optional: Add convenience methods
  public T Get<T>(string key) => ContainsKey(key) ? (T)this[key] : default!;

  public void Set<T>(string key, T value) => this[key] = value!;
}

public class StateDefinition
{
  public StateId Id { get; }

  public Func<Context, Task>? OnEntering { get; set; }

  public Func<Context, Task>? OnEnter { get; set; }

  public Func<string, Context, Task>? OnMessage { get; set; }

  public Func<Context, Task>? OnTimeout { get; set; }

  public Func<Context, Task>? OnExit { get; set; }

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
