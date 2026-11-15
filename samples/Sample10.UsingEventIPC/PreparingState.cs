// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lite.StateMachine;

namespace Sample10.UsingEventIPC;

internal sealed class PreparingState : IState<StateId>
{
  public Task OnEnter(Context<StateId> context)
  {
    Console.WriteLine("Preparing: starting the sample.");
    context.NextState(Result.Success);
    return Task.CompletedTask;
  }

  public Task OnEntering(Context<StateId> context) => Task.CompletedTask;

  public Task OnExit(Context<StateId> context) => Task.CompletedTask;
}
