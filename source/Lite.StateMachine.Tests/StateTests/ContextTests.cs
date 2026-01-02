// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.States;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class ContextTests
{
  public const string ParameterCounter = "Counter";
  public const string ParameterKeyTest = "TestKey";
  public const string TestValue = "success";

  private enum CtxStateId
  {
    State1,
    State2,
    State3,
  }

  private enum ParameterType
  {
    Param1,
    Param2,
    Param3,
  }

  public TestContext TestContext { get; set; }

  /// <summary>Standard synchronous state registration exiting to completion.</summary>
  [TestMethod]
  public void Basic_RegisterState_Executes123_SuccessTest()
  {
    // Assemble
    var ctxProperties = new PropertyBag() { { "KeyString_ValueInt", 99 } };

    var machine = new StateMachine<CtxStateId>();
    machine.RegisterState<CtxState1>(CtxStateId.State1, CtxStateId.State2);
    machine.RegisterState<CtxState2>(CtxStateId.State2, CtxStateId.State3);
    machine.RegisterState<CtxState3>(CtxStateId.State3);

    // Act - Non async Start your engine!
    var task = machine.RunAsync(CtxStateId.State1, ctxProperties);
    task.GetAwaiter().GetResult();

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are registered
    var enums = Enum.GetValues<CtxStateId>().Cast<CtxStateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're registered in order
    Assert.IsTrue(enums.SequenceEqual(machine.States), "States should be registered for execution in the same order as the defined enums, StateId 1 => 2 => 3.");
  }

  private class CtxState1 : StateBase<CtxState1, CtxStateId>
  {
    public override Task OnEnter(Context<CtxStateId> context)
    {
      context.Parameters.SafeAdd(ParameterType.Param1, "KeyEnum_ValueString (2nd Item)");
      return base.OnEnter(context);
    }
  }

  private class CtxState2 : StateBase<CtxState2, CtxStateId>
  {
    public override Task OnEnter(Context<CtxStateId> context)
    {
      context.Parameters.SafeAdd(ParameterType.Param2, "KeyEnum_ValueString (3rd Item)");
      return base.OnEnter(context);
    }
  }

  private class CtxState3 : StateBase<CtxState3, CtxStateId>
  {
    public override Task OnEnter(Context<CtxStateId> context)
    {
      context.Parameters.SafeAdd(ParameterType.Param2, "KeyEnum_ValueString (3rd-Item UPDATED)");
      context.Parameters.SafeAdd("KeyString", "ValueString (4th Item)");
      context.Parameters.SafeAdd(1, "KeyInt_ValueString (Last Item)");
      context.Parameters.SafeAdd("Key6_NullValue", null);

      foreach (var x in context.Parameters)
        Console.WriteLine($"{x.Key}: '{x.Value}'");

      Assert.HasCount(6, context.Parameters);
      return base.OnEnter(context);
    }
  }
}
