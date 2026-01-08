// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.TestData.States;

public class CommandStateBase<TStateClass, TStateId>(IMessageService msg, ILogger<TStateClass> logger)
  : DiStateBase<TStateClass, TStateId>(msg, logger), ICommandState<TStateId>
  where TStateId : struct, Enum
{
  public Task OnMessage(Context<TStateId> context, object message)
  {
    Log.LogInformation("[OnEnter]");

    // DO NOT AUTO-SUCCESS!  Placed here as a note
    ////context.NextState(Result.Success);
    return Task.CompletedTask;
  }

  public Task OnTimeout(Context<TStateId> context)
  {
    Log.LogInformation("[OnMessage]");

    // DO NOT AUTO-SUCCESS!  Placed here as a note
    ////context.NextState(Result.Success);
    return Task.CompletedTask;
  }
}
