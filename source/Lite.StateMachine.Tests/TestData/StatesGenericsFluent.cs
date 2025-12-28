// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine.Tests.TestData;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

public class StateGenerics1() : BaseState<GenericStateId>()
{
  public override void OnEnter(Context<GenericStateId> context) =>
    context.NextState(Result.Ok);

  public override void OnEntering(Context<GenericStateId> context) =>
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;

  public override void OnExit(Context<GenericStateId> context) =>
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
}

public class StateGenerics2() : BaseState<GenericStateId>()
{
  public override void OnEnter(Context<GenericStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    context.NextState(Result.Ok);
  }

  public override void OnEntering(Context<GenericStateId> context) =>
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;

  public override void OnExit(Context<GenericStateId> context) =>
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
}

public class StateGenerics3() : BaseState<GenericStateId>()
{
  public override void OnEnter(Context<GenericStateId> context)
  {
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
    context.Parameters[ParameterType.KeyTest] = ExpectedData.StringSuccess;
    context.NextState(Result.Ok);
  }

  public override void OnEntering(Context<GenericStateId> context) =>
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;

  public override void OnExit(Context<GenericStateId> context) =>
    context.Parameters[ParameterType.Counter] = context.ParameterAsInt(ParameterType.Counter) + 1;
}

#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
