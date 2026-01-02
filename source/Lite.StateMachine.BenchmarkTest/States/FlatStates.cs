// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lite.StateMachine.BenchmarkTest.States;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

public enum ParameterType
{
  MaxCounter,
  Counter,
}

public class FlatState1 : StateBase<FlatState1, BasicStateId>
{
  public override Task OnEnter(Context<BasicStateId> context)
  {
    ////context.Parameters.SafeAdd(ParameterType.Param1, "1st Item");
    return base.OnEnter(context);
  }
}

public class FlatState2 : StateBase<FlatState2, BasicStateId>
{
  public override Task OnEnter(Context<BasicStateId> context)
  {
    ////context.Parameters.SafeAdd(ParameterType.Param2, "2nd Item");
    return base.OnEnter(context);
  }
}

public class FlatState3 : StateBase<FlatState3, BasicStateId>
{
  public override Task OnEnter(Context<BasicStateId> context)
  {
    ////context.Parameters.SafeAdd(ParameterType.Param3, "3rd Item)");
    var max = context.ParameterAsInt(ParameterType.MaxCounter);
    var cnt = context.ParameterAsInt(ParameterType.Counter);
    cnt++;
    context.Parameters.SafeAdd(ParameterType.Counter, cnt);

    if (cnt > max)
      context.NextState(Result.Success);
    else
      context.NextState(Result.Error);

    return Task.CompletedTask;
  }
}

#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
