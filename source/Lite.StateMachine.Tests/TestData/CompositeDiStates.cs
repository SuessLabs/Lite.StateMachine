// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.TestData;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

public class EntryState(IMessageService msg, ILogger<EntryState> log)
  : BaseStateDI<EntryState, CompositeMsgStateId>(msg, log)
{
}

public class ParentState(IMessageService msg, ILogger<ParentState> log)
  : BaseStateDI<ParentState, CompositeMsgStateId>(msg, log)
{
  /// <summary>Handle the result from our last child state.</summary>
  /// <param name="context">Context data.</param>
  /// <returns>Async task.</returns>
  public override Task OnExit(Context<CompositeMsgStateId> context)
  {
    MessageService.Number++;
    MessageService.AddMessage(GetType().Name + " OnExit");
    Log.LogInformation("[{StateName}] [OnExit]", GetType().Name);

    if (context.LastChildResult == Result.Failure)
      context.NextState(Result.Failure);
    else
      context.NextState(Result.Ok);

    return Task.CompletedTask;
  }
}

public class ParentSub_FetchState(IMessageService msg, ILogger<ParentSub_FetchState> log)
  : BaseStateDI<ParentSub_FetchState, CompositeMsgStateId>(msg, log)
{
}

public class ParentSub_WaitMessageState(IMessageService msg, ILogger<ParentSub_WaitMessageState> log)
  : BaseStateDI<ParentSub_WaitMessageState, CompositeMsgStateId>(msg, log),
    ICommandState<CompositeMsgStateId>
{
  public override Task OnEnter(Context<CompositeMsgStateId> context)
  {
    MessageService.Number++;
    MessageService.AddMessage(GetType().Name + " OnEnter");
    Log.LogInformation("[OnEnter] => OK");
    return Task.CompletedTask;
  }

  public Task OnMessage(Context<CompositeMsgStateId> context, object message)
  {
    MessageService.Number++;
    MessageService.AddMessage(GetType().Name + " OnEnter");

    if (message is string s && s.Equals(ExpectedData.StringSuccess, StringComparison.OrdinalIgnoreCase))
    {
      context.NextState(Result.Ok);
      Log.LogInformation("[OnMessage] => OK");
    }
    else
    {
      context.NextState(Result.Error);
      Log.LogInformation("[OnMessage] => Error");
    }

    return Task.CompletedTask;
  }

  public Task OnTimeout(Context<CompositeMsgStateId> context)
  {
    MessageService.Number++;
    MessageService.AddMessage(GetType().Name + " OnEnter");
    context.NextState(Result.Failure);

    Log.LogInformation("[OnTimeout] => Failure");
    return Task.CompletedTask;
  }
}

public class Workflow_DoneState(IMessageService msg, ILogger<Workflow_DoneState> log)
  : BaseStateDI<Workflow_DoneState, CompositeMsgStateId>(msg, log)
{
}

public class Workflow_ErrorState(IMessageService msg, ILogger<Workflow_ErrorState> log)
  : BaseStateDI<Workflow_ErrorState, CompositeMsgStateId>(msg, log)
{
}

public class Workflow_FailureState(IMessageService msg, ILogger<Workflow_FailureState> log)
  : BaseStateDI<Workflow_FailureState, CompositeMsgStateId>(msg, log)
{
}

#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
