// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.TestData.States;

/// <summary>Command Class (doesn't auto-success, you must implement `context.NextState(Result.xxx);`.</summary>
/// <typeparam name="TStateClass">State class object.</typeparam>
/// <typeparam name="TStateId">State Id.</typeparam>
public class CommandStateBase<TStateClass, TStateId>(IMessageService msg, ILogger<TStateClass> logger)
  : DiStateBase<TStateClass, TStateId>(msg, logger), ICommandState<TStateId>
  where TStateId : struct, Enum
{
  //// NEEDS TESTED: public virtual IReadOnlyCollection<ICustomCommand> SubscribedMessageTypes => [];
  ////public virtual IReadOnlyCollection<Type> SubscribedMessageTypes => Array.Empty<Type>();
  public virtual IReadOnlyCollection<Type> SubscribedMessageTypes => [];

  public override Task OnEnter(Context<TStateId> context)
  {
    MessageService.Counter1++;
    Log.LogInformation("[OnEnter]");

    if (HasExtraLogging)
      Debug.WriteLine($"[{GetType().Name}] [{context.CurrentStateId}] [OnEnter]");

    // DO NOT AUTO-SUCCESS!  Placed here as a note
    ////context.NextState(Result.Success);
    return Task.CompletedTask;
  }

  public virtual Task OnMessage(Context<TStateId> context, object message)
  {
    // Note: Cannot supply our own object type
    //// public virtual Task OnMessage(Context<TStateId> context, OpenResponse message)

    MessageService.Counter1++;
    Log.LogInformation("[OnMessage]");

    if (HasExtraLogging)
      Debug.WriteLine($"[{GetType().Name}] [{context.CurrentStateId}] [OnMessage]");

    // DO NOT AUTO-SUCCESS!  Placed here as a note
    ////context.NextState(Result.Success);
    return Task.CompletedTask;
  }

  public virtual Task OnTimeout(Context<TStateId> context)
  {
    MessageService.Counter1++;
    Log.LogInformation("[OnTimeout] => ");

    if (HasExtraLogging)
      Debug.WriteLine($"[{GetType().Name}] [{context.CurrentStateId}] [OnTimeout]");

    // DO NOT AUTO-SUCCESS!  Placed here as a note
    ////context.NextState(Result.Success);
    return Task.CompletedTask;
  }
}
