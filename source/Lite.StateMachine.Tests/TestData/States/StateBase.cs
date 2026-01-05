// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Lite.StateMachine.Tests.TestData.States;

public class StateBase<TStateClass, TStateId> : IState<TStateId>
  where TStateId : struct, Enum
{
  public bool HasDebugLogging { get; set; } = false;

  public virtual Task OnEnter(Context<TStateId> context)
  {
    if (HasDebugLogging)
      Debug.WriteLine($"[{GetType().Name}] [OnEnter]");

    context.NextState(Result.Success);
    return Task.CompletedTask;
  }

  public virtual Task OnEntering(Context<TStateId> context)
  {
    if (HasDebugLogging)
      Debug.WriteLine($"[{GetType().Name}] [OnEntering]");

    return Task.CompletedTask;
  }

  public virtual Task OnExit(Context<TStateId> context)
  {
    if (HasDebugLogging)
      Debug.WriteLine($"[{GetType().Name}] [OnExit]");

    return Task.CompletedTask;
  }
}
