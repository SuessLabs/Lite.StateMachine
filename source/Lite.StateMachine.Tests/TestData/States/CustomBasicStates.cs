// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Services;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace Lite.StateMachine.Tests.TestData.States.CustomStates;

public class State1 : StateBase<State1, CustomStateId>
{
  public State1() => HasDebugLogging = true;

  public override async Task OnEnter(Context<CustomStateId> ctx)
  {
    if (ctx.ParameterAsBool(ParameterType.TestUnregisteredTransition))
      ctx.NextStates.OnSuccess = CustomStateId.State2_Unregistered;
    else
      ctx.NextStates.OnSuccess = CustomStateId.State2_Success;

    await base.OnEnter(ctx);
  }
}

/// <summary>This state should NEVER be transitioned into.</summary>
public class State2Dummy : StateBase<State2Dummy, CustomStateId>
{
  private readonly IMessageService _msgService;

  public State2Dummy(IMessageService msg)
  {
    _msgService = msg;
    HasDebugLogging = true;
  }

  public override Task OnEnter(Context<CustomStateId> context)
  {
    Assert.Fail("Overridden state transitions should not all us to be here.");
    _msgService.Counter2++;
    return base.OnEnter(context);
  }
}

public class State2Success : StateBase<State2Success, CustomStateId>
{
  private readonly IMessageService _msgService;

  public State2Success(IMessageService msg)
  {
    _msgService = msg;
    HasDebugLogging = true;
  }

  public override Task OnEnter(Context<CustomStateId> ctx)
  {
    _msgService.Counter1++;

    Assert.AreEqual(CustomStateId.State1, ctx.PreviousStateId);

    if (ctx.ParameterAsBool(ParameterType.TestExitEarly))
      ctx.NextStates.OnSuccess = null;

    return base.OnEnter(ctx);
  }

  public override Task OnExit(Context<CustomStateId> context)
  {
    // When operating as a Composite, we need to pass back Success.
    context.NextState(Result.Success);
    return base.OnExit(context);
  }
}

public class State2Success_Sub1 : StateBase<State2Success_Sub1, CustomStateId>
{
  private readonly IMessageService _msgService;

  public State2Success_Sub1(IMessageService msg)
  {
    _msgService = msg;
    HasDebugLogging = true;
  }

  public override Task OnEnter(Context<CustomStateId> ctx)
  {
    _msgService.Counter1++;

    // Skip Sub2 and goto Sub3
    if (ctx.ParameterAsBool(ParameterType.TestExitEarly2))
      ctx.NextStates.OnSuccess = CustomStateId.State2_Sub3;

    return base.OnEnter(ctx);
  }
}

public class State2Success_Sub2 : StateBase<State2Success_Sub2, CustomStateId>
{
  private readonly IMessageService _msgService;

  public State2Success_Sub2(IMessageService msg)
  {
    _msgService = msg;
    HasDebugLogging = true;
  }

  public override Task OnEnter(Context<CustomStateId> ctx)
  {
    _msgService.Counter1++;
    return base.OnEnter(ctx);
  }
}

public class State2Success_Sub3 : StateBase<State2Success_Sub3, CustomStateId>
{
  private readonly IMessageService _msgService;

  public State2Success_Sub3(IMessageService msg)
  {
    _msgService = msg;
    HasDebugLogging = true;
  }

  public override Task OnEnter(Context<CustomStateId> ctx)
  {
    if (ctx.ParameterAsBool(ParameterType.TestExitEarly2))
      Assert.AreEqual(CustomStateId.State2_Sub1, ctx.PreviousStateId);
    else
      Assert.AreEqual(CustomStateId.State2_Sub2, ctx.PreviousStateId);

    _msgService.Counter1++;
    return base.OnEnter(ctx);
  }
}

public class State3 : StateBase<State3, CustomStateId>
{
  private readonly IMessageService _msgService;

  public State3(IMessageService msg)
  {
    _msgService = msg;
    HasDebugLogging = true;
  }

  public override Task OnEnter(Context<CustomStateId> ctx)
  {
    Assert.AreEqual(CustomStateId.State2_Success, ctx.PreviousStateId);

    _msgService.Counter3++;
    return base.OnEnter(ctx);
  }
}

#pragma warning restore IDE0130 // Namespace does not match folder structure
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
