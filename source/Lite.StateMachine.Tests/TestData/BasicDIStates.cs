// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

/*
using Lite.StateMachine.Tests.TestData.Services;

namespace Lite.StateMachine.Tests.TestData;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

public class StateDi1(IMessageService msg) : BaseState<FlatStateId>()
{
  private readonly IMessageService _msg = msg;

  public override void OnEnter(Context<FlatStateId> context) =>
    context.NextState(Result.Ok);

  public override void OnEntering(Context<FlatStateId> context) =>
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;

  public override void OnExit(Context<FlatStateId> context) =>
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
}

public class StateDi2() : BaseState<FlatStateId>()
{
  public override void OnEnter(Context<FlatStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    context.NextState(Result.Ok);
  }

  public override void OnEntering(Context<FlatStateId> context) =>
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;

  public override void OnExit(Context<FlatStateId> context) =>
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
}

public class StateDi3() : BaseState<FlatStateId>()
{
  public override void OnEnter(Context<FlatStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    context.Parameters[ParameterType.KeyTest] = ExpectedData.StringSuccess;
    context.NextState(Result.Ok);
  }

  public override void OnEntering(Context<FlatStateId> context) =>
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;

  public override void OnExit(Context<FlatStateId> context) =>
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
}

#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
*/
