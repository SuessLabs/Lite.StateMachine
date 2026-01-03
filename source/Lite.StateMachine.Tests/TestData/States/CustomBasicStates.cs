// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace Lite.StateMachine.Tests.TestData.States.CustomBasicStates;

public class State1 : StateBase<State1, CustomBasicStateId>
{
  public State1() => HasDebugLogging = true;

  public override async Task OnEnter(Context<CustomBasicStateId> context)
  {
    int cnt = context.ParameterAsInt(ParameterType.Counter);

    context.NextStates[Result.Success] = CustomBasicStateId.State2_SuccessA;

    // vNext: Cycle through each of the OnSuccess/OnExit/OnFailure
    ////if (cnt == 0)
    ////  context.NextStates[Result.Success] = CustomBasicStateId.State2_SuccessA;
    ////else if (cnt == 1)
    ////  context.NextStates[Result.Success] = CustomBasicStateId.State2_SuccessB;

    await base.OnEnter(context);
  }
}

public class State2Dummy : StateBase<State2Dummy, CustomBasicStateId>
{
  public State2Dummy() => HasDebugLogging = true;
}

public class State2SuccessA : StateBase<State2SuccessA, CustomBasicStateId>
{
  public State2SuccessA() => HasDebugLogging = true;
}

public class State3 : StateBase<State3, CustomBasicStateId>
{
  public State3() => HasDebugLogging = true;
}

#pragma warning restore IDE0130 // Namespace does not match folder structure
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
