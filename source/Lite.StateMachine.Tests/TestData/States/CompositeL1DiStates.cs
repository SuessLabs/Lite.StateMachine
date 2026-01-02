// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.TestData.States;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

public class EntryState(IMessageService msg, ILogger<EntryState> log)
  : DiStateBase<EntryState, CompositeMsgStateId>(msg, log)
{
}

public class ParentState(IMessageService msg, ILogger<ParentState> log)
  : DiStateBase<ParentState, CompositeMsgStateId>(msg, log)
{
  /// <summary>Handle the result from our last child state.</summary>
  /// <param name="context">Context data.</param>
  /// <returns>Async task.</returns>
  public override Task OnExit(Context<CompositeMsgStateId> context)
  {
    MessageService.Counter1++;
    MessageService.AddMessage(GetType().Name + " OnExit");
    Log.LogInformation("[OnExit] => {result}", context.LastChildResult);

    context.NextState(context.LastChildResult switch
    {
      Result.Failure => Result.Failure,
      Result.Error => Result.Error,
      _ => Result.Success,
    });

    return Task.CompletedTask;
  }
}

public class ParentSub_FetchState(IMessageService msg, ILogger<ParentSub_FetchState> log)
  : DiStateBase<ParentSub_FetchState, CompositeMsgStateId>(msg, log)
{
}

public class ParentSub_WaitMessageState(IMessageService msg, ILogger<ParentSub_WaitMessageState> log)
  : DiStateBase<ParentSub_WaitMessageState, CompositeMsgStateId>(msg, log),
    ICommandState<CompositeMsgStateId>
{
  public override Task OnEnter(Context<CompositeMsgStateId> context)
  {
    MessageService.Counter1++;
    MessageService.AddMessage(GetType().Name + " OnEnter");

    Log.LogInformation("[OnEnter] (Counter2: {cnt})", MessageService.Counter2);
    switch (MessageService.Counter2)
    {
      case 0:
        // Do nothing, wait for message or timeout
        Log.LogInformation("[OnEnter] Forcibly timeout for Failure State");
        break;

      case 1:
        // Send "ErrorRequest" to go to error state
        Log.LogInformation("[OnEnter] [Publish '{msg}'] for Error State", MessageType.ErrorRequest);
        context.EventAggregator?.Publish(MessageType.ErrorRequest);
        break;

      case 2:
        // Send "SuccessRequest" to go to done state
        Log.LogInformation("[OnEnter] [Publish '{msg}'] for Done State", MessageType.SuccessRequest);
        context.EventAggregator?.Publish(MessageType.SuccessRequest);
        break;

      default:
        Log.LogError("[OnEnter] We REALLY shouldn't be here!!");
        break;
    }

    return Task.CompletedTask;
  }

  public Task OnMessage(Context<CompositeMsgStateId> context, object message)
  {
    MessageService.Counter1++;
    MessageService.AddMessage(GetType().Name + " OnEnter");

    if (message is not string response)
    {
      Log.LogInformation("[OnMessage] Invalid message response");
      Debug.WriteLine($"[{GetType().Name}] [OnMessage] Invalid message response");
      return Task.CompletedTask;
    }

    Log.LogInformation("[OnMessage] Received: {response}", response);

    switch (response)
    {
      case MessageType.BadResponse:
        // Do nothing, wait for timeout
        Log.LogError("[OnMessage] SHOULD BE HERE!");
        break;

      case MessageType.ErrorResponse:
        context.NextState(Result.Error);
        break;

      case MessageType.SuccessResponse:
        context.NextState(Result.Success);
        break;

      default:
        Debug.WriteLine($"[{GetType().Name}] [OnMessage] Unexpected response => Failure (shouldn't be here)");
        context.NextState(Result.Failure);
        break;
    }

    return Task.CompletedTask;
  }

  public Task OnTimeout(Context<CompositeMsgStateId> context)
  {
    MessageService.Counter1++;
    MessageService.AddMessage(GetType().Name + " OnEnter");
    context.NextState(Result.Failure);

    Log.LogInformation("[OnTimeout] => Failure; (Publishing: ReceivedTimeout)");

    // Publish timeout event
    context.EventAggregator?.Publish(NotificationType.Timeout);
    return Task.CompletedTask;
  }
}

public class Workflow_DoneState(IMessageService msg, ILogger<Workflow_DoneState> log)
  : DiStateBase<Workflow_DoneState, CompositeMsgStateId>(msg, log)
{
}

public class Workflow_ErrorState(IMessageService msg, ILogger<Workflow_ErrorState> log)
  : DiStateBase<Workflow_ErrorState, CompositeMsgStateId>(msg, log)
{
  public override Task OnEnter(Context<CompositeMsgStateId> context)
  {
    MessageService.Counter1++;
    MessageService.Counter2++;
    MessageService.AddMessage(GetType().Name + " OnEnter");

    Log.LogInformation("[{StateName}] [OnEnter] => OK; Counter2++", GetType().Name);
    Debug.WriteLine($"[{GetType().Name}] [OnEnter] => OK; Counter2++");

    context.EventAggregator?.Publish(NotificationType.Error);

    context.NextState(Result.Success);
    return Task.CompletedTask;
  }
}

public class Workflow_FailureState(IMessageService msg, ILogger<Workflow_FailureState> log)
  : DiStateBase<Workflow_FailureState, CompositeMsgStateId>(msg, log)
{
  public override Task OnEnter(Context<CompositeMsgStateId> context)
  {
    MessageService.Counter1++;
    MessageService.Counter2++;
    MessageService.AddMessage(GetType().Name + " OnEnter");

    Log.LogInformation("[{StateName}] [OnEnter] => OK; (Counter2++)", GetType().Name);
    Debug.WriteLine($"[{GetType().Name}] [OnEnter] => OK; (Counter2++)");

    context.EventAggregator?.Publish(NotificationType.Failure);

    context.NextState(Result.Success);
    return Task.CompletedTask;
  }
}

#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
