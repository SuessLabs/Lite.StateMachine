// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class BasicStateTests
{
  public const string ParameterCounter = "Counter";
  public const string ParameterKeyTest = "TestKey";
  public const string TestValue = "success";

  /// <summary>Standard basic state registration with fall-through exiting.</summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
  [TestMethod]
  public async Task RegisterState_Executes123_SuccessTestAsync()
  {
    // Assemble
    var counter = 0;

    var machine = new StateMachine<BasicStateId>();
    machine.RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2);
    machine.RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3);
    machine.RegisterState<BasicState3>(BasicStateId.State3);

    // Act - Start your engine!
    var ctxProperties = new PropertyBag() { { ParameterCounter, counter } };
    var task = machine.RunAsync(BasicStateId.State1, ctxProperties);
    await task;   // Non async method: task.GetAwaiter().GetResult();

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are hit
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're in order
    Assert.IsTrue(enums.SequenceEqual(machine.States), "States should execute in the same order as the defined enums, StateId 1 => 2 => 3.");
  }

  [TestMethod]
  public async Task RegisterState_ExecutesDifferentOrderThanEnum_SuccessTestAsync()
  {
    // Assemble
    var machine = new StateMachine<BasicStateId>();
    machine.RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State3);
    machine.RegisterState<BasicState3>(BasicStateId.State3, BasicStateId.State2);
    machine.RegisterState<BasicState2>(BasicStateId.State2);

    // Act - Start your engine!
    var task = machine.RunAsync(BasicStateId.State1);
    await task;

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are hit
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're NOT in order
    Assert.IsFalse(enums.SequenceEqual(machine.States), "States should NOT execute in the same order as the defined enums, StateId 1 => 2 => 3.");
  }

  /// <summary>Standard basic state registration with fall-through exiting.</summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
  [TestMethod]
  [Ignore("vNext - Currently StateMachine destroys context after run completes.")]
  public async Task RegisterState_ReturnsContext_SuccessTestAsync()
  {
    // Assemble
    var counter = 0;

    var machine = new StateMachine<BasicStateId>();
    machine.RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2);
    machine.RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3);
    machine.RegisterState<BasicState3>(BasicStateId.State3);

    // Act - Start your engine!
    var ctxProperties = new PropertyBag() { { ParameterCounter, counter } };
    var task = machine.RunAsync(BasicStateId.State1, ctxProperties);
    await task;   // Non async method: task.GetAwaiter().GetResult();

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNotNull(machine.Context);

    var ctxFinalParams = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinalParams);
    Assert.AreEqual(TestValue, ctxFinalParams[ParameterKeyTest]);

    // NOTE: This should be 9 because each state has 3 hooks that increment the counter
    // TODO (2025-12-22 DS): Fix last state not calling OnExit.
    Assert.AreEqual(9, ctxFinalParams[ParameterCounter]);
  }
}
