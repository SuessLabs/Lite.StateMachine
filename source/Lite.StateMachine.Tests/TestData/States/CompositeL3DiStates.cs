// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.Logging;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable IDE0130 // Namespace does not match folder structure

/// <summaryAdded "CompositeL3DiStates" to namespace to reduce class naming collisions</summary>
namespace Lite.StateMachine.Tests.TestData.States.CompositeL3DiStates;

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
  : DiStateBase<State1, CompositeL3>(msg, log)
{
  public override Task OnEnter(Context<CompositeL3> context)
  {
    context.Parameters.Add(context.CurrentStateId.ToString(), Guid.NewGuid());
    MessageService.AddMessage($"[Keys-{context.CurrentStateId}]: {string.Join(",", context.Parameters.Keys)}");
    return base.OnEnter(context);
  }
}

/// <summary>Level-1: Composite.</summary>
public class State2(IMessageService msg, ILogger<State2> log)
  : CommonDiStateBase<State2, CompositeL3>(msg, log)
{
  public override Task OnExit(Context<CompositeL3> context)
  {
    // Expected Count: 4 - State2_Sub2 is composite; therefore, discarded.
    // State1,State2,State2_Sub1,State2_Sub3
    MessageService.Counter2 = context.Parameters.Count;
    return base.OnExit(context);
  }
}

/// <summary>Sublevel-2: State.<summary>
public class State2_Sub1(IMessageService msg, ILogger<State2_Sub1> log)
  : CommonDiStateBase<State2_Sub1, CompositeL3>(msg, log);

/// <summary>Sublevel-2: Composite.</summary>
public class State2_Sub2(IMessageService msg, ILogger<State2_Sub2> log)
  : CommonDiStateBase<State2_Sub2, CompositeL3>(msg, log)
{
  public override Task OnExit(Context<CompositeL3> context)
  {
    // Expected Count: 7
    MessageService.Counter3 = context.Parameters.Count;
    return base.OnExit(context);
  }
}

/// <summary>Sublevel-3: State.</summary>
public class State2_Sub2_Sub1(IMessageService msg, ILogger<State2_Sub2_Sub1> log)
  : CommonDiStateBase<State2_Sub2_Sub1, CompositeL3>(msg, log);

/// <summary>Sublevel-3: State.</summary>
public class State2_Sub2_Sub2(IMessageService msg, ILogger<State2_Sub2_Sub2> log)
  : CommonDiStateBase<State2_Sub2_Sub2, CompositeL3>(msg, log);

/// <summary>Sublevel-3: Last State.</summary>
public class State2_Sub2_Sub3(IMessageService msg, ILogger<State2_Sub2_Sub3> log)
  : CommonDiStateBase<State2_Sub2_Sub3, CompositeL3>(msg, log);

/// <summary>Sublevel-2: Last State.</summary>
public class State2_Sub3(IMessageService msg, ILogger<State2_Sub3> log)
  : DiStateBase<State2_Sub3, CompositeL3>(msg, log)
{
  public override Task OnEnter(Context<CompositeL3> context)
  {
    context.Parameters.Add(context.CurrentStateId.ToString(), Guid.NewGuid());
    MessageService.AddMessage($"[Keys-{context.CurrentStateId}]: {string.Join(",", context.Parameters.Keys)}");
    return base.OnEnter(context);
  }
}

/// <summary>Make sure not child-created context is there.</summary>
public class State3(IMessageService msg, ILogger<State3> log)
  : DiStateBase<State3, CompositeL3>(msg, log)
{
  public override Task OnEnter(Context<CompositeL3> context)
  {
    context.Parameters.Add(context.CurrentStateId.ToString(), Guid.NewGuid());
    MessageService.AddMessage($"[Keys-{context.CurrentStateId}]: {string.Join(",", context.Parameters.Keys)}");
    return base.OnEnter(context);
  }
}

#pragma warning restore IDE0130 // Namespace does not match folder structure
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
