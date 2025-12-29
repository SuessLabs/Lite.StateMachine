// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Lite.StateMachine.Tests.TestData;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

public class BasicState1() : IState<BasicStateId>
{
  public Task OnEnter(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    context.NextState(Result.Ok);
    Console.WriteLine($"[BasicState1][OnEnter] {context.Parameters[ParameterType.Counter]} => OK");
    return Task.CompletedTask;
  }

  public Task OnEntering(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    Console.WriteLine($"[BasicState1][OnEntering] {context.Parameters[ParameterType.Counter]}");
    return Task.CompletedTask;
  }

  public Task OnExit(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    Console.WriteLine($"[BasicState1][OnExit] {context.Parameters[ParameterType.Counter]}");
    return Task.CompletedTask;
  }
}

public class BasicState2() : IState<BasicStateId>
{
  public Task OnEnter(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;

    // Only move to the next state if we are not testing hanging state avoidance
    var testHangingState = context.ParameterAsBool(ParameterType.HungStateAvoidance);
    if (!testHangingState)
      context.NextState(Result.Ok);

    Console.WriteLine($"[BasicState2][OnEnter] {context.Parameters[ParameterType.Counter]} => OK");
    return Task.CompletedTask;
  }

  public Task OnEntering(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    Console.WriteLine($"[BasicState2][OnEntering] {context.Parameters[ParameterType.Counter]}");
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
  public Task OnEnter(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    context.Parameters[ParameterType.KeyTest] = ExpectedData.StringSuccess;
    context.NextState(Result.Ok);
    Console.WriteLine($"[BasicState3][OnEnter] {context.Parameters[ParameterType.Counter]}");
    return Task.CompletedTask;
  }

  public Task OnEntering(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    Console.WriteLine($"[BasicState3][OnEntering] {context.Parameters[ParameterType.Counter]}");
    return Task.CompletedTask;
  }

  public Task OnExit(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    Console.WriteLine($"[BasicState3][OnExit] {context.Parameters[ParameterType.Counter]}");
    return Task.CompletedTask;
  }
}

#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
