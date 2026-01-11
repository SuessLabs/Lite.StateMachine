// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Models;
using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.Logging;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable IDE0130 // Namespace does not match folder structure

/// <summaryAdded "CommandL3DiStates" to namespace to reduce class naming collisions</summary>
namespace Lite.StateMachine.Tests.TestData.States.CommandL3DiStates;

public enum StateId
{
  State1,
  State2,
  State2_Sub1,
  State2_Sub2,
  State2_Sub2_Sub1,
  State2_Sub2_Sub2,
  State2_Sub2_Sub3,
  State2_Sub3,
  State3,
  Done,
  Error,
}

public class CommonDiStateBase<TStateClass, TStateId>(IMessageService msg, ILogger<TStateClass> logger)
  : DiStateBase<TStateClass, TStateId>(msg, logger)
  where TStateId : struct, Enum
{
  // Helper so we don't have to keep rewriting the same "override Task OnEnter(...)"
  // 8 lines * 9 states.. useless
  public override Task OnEnter(Context<TStateId> context)
  {
    context.Parameters.Add(context.CurrentStateId.ToString(), Guid.NewGuid());
    MessageService.AddMessage($"[Keys-{context.CurrentStateId}]: {string.Join(",", context.Parameters.Keys)}");
    return base.OnEnter(context);
  }
}

public class State1(IMessageService msg, ILogger<State1> log)
  : CommandStateBase<State1, StateId>(msg, log)
{
  /// <summary>Gets message types for command state to subscribe to.</summary>
  public override IReadOnlyCollection<Type> SubscribedMessageTypes => new[]
  {
    //// typeof(OpenCommand),  // <---- NOTE: Not needed
    typeof(UnlockResponse),
  };

  public override Task OnEnter(Context<StateId> context)
  {
    context.Parameters.Add(context.CurrentStateId.ToString(), Guid.NewGuid());
    MessageService.AddMessage($"[Keys-{context.CurrentStateId}]: {string.Join(",", context.Parameters.Keys)}");

    context.EventAggregator?.Publish(new UnlockCommand { Counter = 1 });

    return base.OnEnter(context);
  }

  public override Task OnMessage(Context<StateId> context, object message)
  {
    // NOTE: Cannot supply our own types yet.
    ////public override Task OnMessage(Context<StateId> context, OpenResponse message)

    if (message is not UnlockResponse)
    {
      // SHOUD NEVER BE HERE!  As only 'OpenResponse' is in the filter list
      context.NextState(Result.Error);
      return Task.CompletedTask;
    }

    MessageService.Counter4++;

    context.NextState(Result.Success);
    return base.OnMessage(context, message);
  }

  public override Task OnTimeout(Context<StateId> context)
  {
    context.NextState(Result.Error);

    // Never gets thrown
    ////throw new TimeoutException();

    return base.OnTimeout(context);
  }
}

/// <summary>Level-1: Composite.</summary>
public class State2(IMessageService msg, ILogger<State2> log)
  : CommonDiStateBase<State2, StateId>(msg, log)
{
  #region CodeMaid - Suppress method sorting

  public override Task OnEntering(Context<StateId> context)
  {
    // Demonstrate NEW parameter that will carry forward
    context.Parameters.Add($"{context.CurrentStateId}!Anchor", Guid.NewGuid());
    return base.OnEntering(context);
  }

  #endregion CodeMaid - Suppress method sorting

  public override Task OnEnter(Context<StateId> context)
  {
    // Demonstrate temporary parameter that will be discarded after State2's OnExit
    context.Parameters.Add($"{context.CurrentStateId}!TEMP", Guid.NewGuid());
    return base.OnEnter(context);
  }

  public override Task OnExit(Context<StateId> context)
  {
    // Expected Count: 7 - State2_Sub2 is composite; therefore, discarded.
    // State1,State2!Anchor,State2!TEMP,State2,State2_Sub1,State2_Sub2!Anchor,State2_Sub3
    MessageService.Counter2 = context.Parameters.Count;
    return base.OnExit(context);
  }
}

/// <summary>Sublevel-2: State.<summary>
public class State2_Sub1(IMessageService msg, ILogger<State2_Sub1> log)
  : CommonDiStateBase<State2_Sub1, StateId>(msg, log)
{
  public override Task OnEnter(Context<StateId> context) => base.OnEnter(context);
}

/// <summary>Sublevel-2: Composite.</summary>
public class State2_Sub2(IMessageService msg, ILogger<State2_Sub2> log)
  : CommonDiStateBase<State2_Sub2, StateId>(msg, log)
{
  #region CodeMaid - DoNotReorder

  public override Task OnEntering(Context<StateId> context)
  {
    // Demonstrate NEW parameter that will carry forward
    context.Parameters.Add($"{context.CurrentStateId}!Anchor", Guid.NewGuid());
    return base.OnEntering(context);
  }

  #endregion CodeMaid - DoNotReorder

  public override Task OnEnter(Context<StateId> context)
  {
    // Demonstrate temporary parameter that will be discarded after State2_Sub2's OnExit
    context.Parameters.Add($"{context.CurrentStateId}!TEMP", Guid.NewGuid());

    Log.LogInformation($"[OnEnter] CurrentStateId: {context.CurrentStateId} PreviousStateId: {context.PreviousStateId}");
    Assert.AreEqual(StateId.State2_Sub2, context.CurrentStateId);
    return base.OnEnter(context);
  }

  public override Task OnExit(Context<StateId> context)
  {
    // Expected Count: 7
    MessageService.Counter3 = context.Parameters.Count;

    Log.LogInformation("[OnExit] CurrentStateId: {c} PreviousStateId: {p}", context.CurrentStateId, context.PreviousStateId);
    Assert.AreEqual(StateId.State2_Sub2, context.CurrentStateId);
    Assert.AreEqual(StateId.State2_Sub1, context.PreviousStateId);
    Assert.AreEqual(StateId.State2_Sub2_Sub3, context.LastChildStateId);
    Assert.AreEqual(Result.Success, context.LastChildResult);
    return base.OnExit(context);
  }
}

/// <summary>Sublevel-3: State.</summary>
public class State2_Sub2_Sub1(IMessageService msg, ILogger<State2_Sub2_Sub1> log)
  : CommonDiStateBase<State2_Sub2_Sub1, StateId>(msg, log)
{
  public override Task OnEnter(Context<StateId> context) => base.OnEnter(context);
}

/// <summary>Sublevel-3: State.</summary>
public class State2_Sub2_Sub2(IMessageService msg, ILogger<State2_Sub2_Sub2> log)
  : CommandStateBase<State2_Sub2_Sub2, StateId>(msg, log)
{
  /// <summary>Gets message types for command state to subscribe to.</summary>
  public override IReadOnlyCollection<Type> SubscribedMessageTypes =>
  [
    typeof(UnlockResponse),
    typeof(CloseResponse),
  ];

  public override Task OnEnter(Context<StateId> context)
  {
    context.Parameters.Add(context.CurrentStateId.ToString(), Guid.NewGuid());
    MessageService.AddMessage($"[Keys-{context.CurrentStateId}]: {string.Join(",", context.Parameters.Keys)}");

    // NOTE:
    //  1) We're sending the same OpenCommand to prove that State1's OnMessage isn't called a 2nd time.
    //  2) CloseResponse doesn't reached our OnMessage because we left already.
    context.EventAggregator?.Publish(new UnlockCommand { Counter = 200 });

    Log.LogInformation($"[OnEnter] CurrentStateId: {context.CurrentStateId} PreviousStateId: {context.PreviousStateId}");
    Assert.AreEqual(StateId.State2_Sub2_Sub2, context.CurrentStateId);
    return base.OnEnter(context);
  }

  public override Task OnMessage(Context<StateId> context, object message)
  {
    MessageService.Counter4++;

    context.NextState(Result.Success);

    Log.LogInformation($"[OnMessage] CurrentStateId: {context.CurrentStateId} PreviousStateId: {context.PreviousStateId}");
    Assert.AreEqual(StateId.State2_Sub2_Sub2, context.CurrentStateId);
    return base.OnMessage(context, message);
  }

  public override Task OnTimeout(Context<StateId> context)
  {
    context.NextState(Result.Error);
    return base.OnTimeout(context);
  }
}

/// <summary>Sublevel-3: Last State.</summary>
public class State2_Sub2_Sub3(IMessageService msg, ILogger<State2_Sub2_Sub3> log)
: CommonDiStateBase<State2_Sub2_Sub3, StateId>(msg, log)
{
  public override Task OnEnter(Context<StateId> context) => base.OnEnter(context);
}

/// <summary>Sublevel-2: Last State.</summary>
public class State2_Sub3(IMessageService msg, ILogger<State2_Sub3> log)
  : DiStateBase<State2_Sub3, StateId>(msg, log)
{
  public override Task OnEnter(Context<StateId> context)
  {
    context.Parameters.Add(context.CurrentStateId.ToString(), Guid.NewGuid());
    MessageService.AddMessage($"[Keys-{context.CurrentStateId}]: {string.Join(",", context.Parameters.Keys)}");

    // NOTE: We the state following the composite doesn't know about "LastChildXXX".
    Log.LogInformation($"[OnEnter] CurrentStateId: {context.CurrentStateId} PreviousStateId: {context.PreviousStateId}");
    Assert.AreEqual(StateId.State2_Sub3, context.CurrentStateId);
    Assert.AreEqual(StateId.State2_Sub2, context.PreviousStateId);
    Assert.IsNull(context.LastChildStateId);
    Assert.IsNull(context.LastChildResult);
    return base.OnEnter(context);
  }
}

/// <summary>Make sure not child-created context is there.</summary>
public class State3(IMessageService msg, ILogger<State3> log)
  : DiStateBase<State3, StateId>(msg, log)
{
  public override Task OnEnter(Context<StateId> context)
  {
    context.Parameters.Add(context.CurrentStateId.ToString(), Guid.NewGuid());
    MessageService.AddMessage($"[Keys-{context.CurrentStateId}]: {string.Join(",", context.Parameters.Keys)}");

    Log.LogInformation($"[OnEnter] CurrentStateId: {context.CurrentStateId} PreviousStateId: {context.PreviousStateId}");
    Assert.AreEqual(StateId.State3, context.CurrentStateId);
    return base.OnEnter(context);
  }
}

#pragma warning restore IDE0130 // Namespace does not match folder structure
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1124 // Do not use regions
