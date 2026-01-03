// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Lite.StateMachine.BenchmarkTests.States;

public class StateBase<TStateClass, TStateId> : IState<TStateId>
  where TStateId : struct, Enum
{
  public virtual Task OnEnter(Context<TStateId> context)
  {
    context.NextState(Result.Success);
    return Task.CompletedTask;
  }

  public virtual Task OnEntering(Context<TStateId> context) =>
    Task.CompletedTask;

  public virtual Task OnExit(Context<TStateId> context) =>
    Task.CompletedTask;
}
