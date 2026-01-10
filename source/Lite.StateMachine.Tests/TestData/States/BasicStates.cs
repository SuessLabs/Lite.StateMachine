// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Lite.StateMachine.Tests.TestData.States;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1124 // Do not use regions

public class BasicState1() : IState<BasicStateId>
{
  #region Suppress CodeMaid Method Sorting

  public Task OnEntering(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    Console.WriteLine($"[BasicState1][OnEntering] {context.Parameters[ParameterType.Counter]}");
    return Task.CompletedTask;
  }

  #endregion Suppress CodeMaid Method Sorting

  public async Task OnEnter(Context<BasicStateId> context)
  {
    // Some async work here...
    await Task.Yield();

    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    context.NextState(Result.Success);
    Console.WriteLine($"[BasicState1][OnEnter] {context.Parameters[ParameterType.Counter]} => OK");
    Console.WriteLine($"[BasicState1][OnEnter].OnSuccess '{context.NextStates.OnSuccess}'");
    Console.WriteLine($"[BasicState1][OnEnter].OnError '{context.NextStates.OnError}'");
    Console.WriteLine($"[BasicState1][OnEnter].OnFailure '{context.NextStates.OnFailure}'");
  }

  public Task OnExit(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    Console.WriteLine($"[BasicState1][OnExit] Params[Counter]: {context.Parameters[ParameterType.Counter]}");
    return Task.CompletedTask;
  }
}

public class BasicState2() : IState<BasicStateId>
{
  #region Suppress CodeMaid Method Sorting

  public Task OnEntering(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    Console.WriteLine($"[BasicState2][OnEntering] {context.Parameters[ParameterType.Counter]}");
    return Task.CompletedTask;
  }

  #endregion Suppress CodeMaid Method Sorting

  public Task OnEnter(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;

    // Assert origin of the previous state
    if (context.ParameterAsBool(ParameterType.TestExecutionOrder))
      Assert.AreEqual(BasicStateId.State1, context.PreviousStateId);
    else
      Assert.AreEqual(BasicStateId.State3, context.PreviousStateId);

    // Only move to the next state if we are not testing hanging state avoidance
    var testHangingState = context.ParameterAsBool(ParameterType.TestHungStateAvoidance);
    if (!testHangingState)
      context.NextState(Result.Success);

    Console.WriteLine($"[BasicState2][OnEnter] {context.Parameters[ParameterType.Counter]} => OK");
    return Task.CompletedTask;
  }

  public Task OnExit(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    Console.WriteLine($"[BasicState2][OnExit] {context.Parameters[ParameterType.Counter]}");
    return Task.CompletedTask;
  }
}

public class BasicState3() : IState<BasicStateId>
{
  #region Suppress CodeMaid Method Sorting

  public Task OnEntering(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    Console.WriteLine($"[BasicState3][OnEntering] {context.Parameters[ParameterType.Counter]}");
    return Task.CompletedTask;
  }

  #endregion Suppress CodeMaid Method Sorting

  public Task OnEnter(Context<BasicStateId> context)
  {
    // Assert origin of the previous state
    if (context.ParameterAsBool(ParameterType.TestExecutionOrder))
      Assert.AreEqual(BasicStateId.State2, context.PreviousStateId);
    else
      Assert.AreEqual(BasicStateId.State1, context.PreviousStateId);

    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    context.Parameters[ParameterType.KeyTest] = MessageType.SuccessResponse;
    context.NextState(Result.Success);
    Console.WriteLine($"[BasicState3][OnEnter] {context.Parameters[ParameterType.Counter]}");
    return Task.CompletedTask;
  }

  public Task OnExit(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    Console.WriteLine($"[BasicState3][OnExit] {context.Parameters[ParameterType.Counter]}");
    return Task.CompletedTask;
  }
}

#pragma warning restore SA1124 // Do not use regions
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
