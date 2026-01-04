// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Services;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace Lite.StateMachine.Tests.TestData.States.CustomBasicStates;

public class State1 : StateBase<State1, CustomBasicStateId>
{
  public State1() => HasDebugLogging = true;

  public override async Task OnEnter(Context<CustomBasicStateId> ctx)
  {
    int cnt = ctx.ParameterAsInt(ParameterType.Counter);

    if (ctx.ParameterAsBool(ParameterType.TestUnregisteredTransition))
      ctx.NextStates.OnSuccess = CustomBasicStateId.State2_Unregistered;
    else
      ctx.NextStates.OnSuccess = CustomBasicStateId.State2_Success;

    await base.OnEnter(ctx);
  }
}

/// <summary>This state should NEVER be transitioned into.</summary>
public class State2Dummy : StateBase<State2Dummy, CustomBasicStateId>
{
  private readonly IMessageService _msgService;

  public State2Dummy(IMessageService msg)
  {
    _msgService = msg;
    HasDebugLogging = true;
  }

  public override Task OnEnter(Context<CustomBasicStateId> context)
  {
    Assert.Fail("Overridden state transitions should not all us to be here.");
    _msgService.Counter2++;
    return base.OnEnter(context);
  }
}

public class State2SuccessA : StateBase<State2SuccessA, CustomBasicStateId>
{
  private readonly IMessageService _msgService;

  public State2SuccessA(IMessageService msg)
  {
    _msgService = msg;
    HasDebugLogging = true;
  }

  public override Task OnEnter(Context<CustomBasicStateId> ctx)
  {
    _msgService.Counter1++;

    if (ctx.ParameterAsBool(ParameterType.TestExitEarly))
      ctx.NextStates.OnSuccess = null;

    return base.OnEnter(ctx);
  }
}

public class State3 : StateBase<State3, CustomBasicStateId>
{
  private readonly IMessageService _msgService;

  public State3(IMessageService msg)
  {
    _msgService = msg;
    HasDebugLogging = true;
  }

  public override Task OnEnter(Context<CustomBasicStateId> ctx)
  {
    _msgService.Counter3++;
    return base.OnEnter(ctx);
  }
}

#pragma warning restore IDE0130 // Namespace does not match folder structure
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
