// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Services;

namespace Lite.StateMachine.Tests.TestData;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

public class StateDi1(IMessageService msg) : IState<BasicStateId>
{
  private readonly IMessageService _msg = msg;

  public Task OnEnter(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }

  public Task OnEntering(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    return Task.CompletedTask;
  }

  public Task OnExit(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    return Task.CompletedTask;
  }
}

public class StateDi2() : IState<BasicStateId>
{
  public Task OnEnter(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }

  public Task OnEntering(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    return Task.CompletedTask;
  }

  public Task OnExit(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    return Task.CompletedTask;
  }
}

public class StateDi3() : IState<BasicStateId>
{
  public Task OnEnter(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    context.Parameters[ParameterType.KeyTest] = ExpectedData.StringSuccess;
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }

  public Task OnEntering(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    return Task.CompletedTask;
  }

  public Task OnExit(Context<BasicStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    return Task.CompletedTask;
  }
}

#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
