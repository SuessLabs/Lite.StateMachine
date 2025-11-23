// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace LiteState;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class Context
{
  private readonly Func<Result, Task> _nextState;

  public Context(Func<Result, Task> nextState)
  {
    _nextState = nextState ?? throw new ArgumentNullException(nameof(nextState));
  }

  public Dictionary<string, object> Errors { get; } = new();

  /// <summary>The previous state's enum value.</summary>
  public State LastState { get; internal set; } = State.None;

  public Dictionary<string, object> Params { get; } = new();

  /// <summary>Triggers moving to the next state, based on a Result.</summary>
  public Task NextState(Result result) => _nextState(result);
}
