// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData;
using Lite.StateMachine.Tests.TestData.States;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class BasicStateTests
{
  public const string ParameterCounter = "Counter";
  public const string ParameterKeyTest = "TestKey";
  public const string TestValue = "success";

  public TestContext TestContext { get; set; }

  /// <summary>Standard synchronous state registration exiting to completion.</summary>
  [TestMethod]
  public void Basic_RegisterState_Executes123_SuccessTest()
  {
    // Assemble
    var counter = 0;
    var ctxProperties = new PropertyBag() { { ParameterCounter, counter } };

    var machine = new StateMachine<BasicStateId>();
    machine.RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2);
    machine.RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3);
    machine.RegisterState<BasicState3>(BasicStateId.State3);

    // Act - Non async Start your engine!
    var task = machine.RunAsync(BasicStateId.State1, ctxProperties);
    task.GetAwaiter().GetResult();

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're registered in order
    Assert.IsTrue(enums.SequenceEqual(machine.States), "States should be registered for execution in the same order as the defined enums, StateId 1 => 2 => 3.");
  }

  /// <summary>Standard async state registration exiting to completion.</summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
  [TestMethod]
  public async Task Basic_RegisterState_Executes123_SuccessTestAsync()
  {
    // Assemble
    var counter = 0;
    var ctxProperties = new PropertyBag() { { ParameterCounter, counter } };

    var machine = new StateMachine<BasicStateId>();
    machine.RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2);
    machine.RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3);
    machine.RegisterState<BasicState3>(BasicStateId.State3);

    // Act - Start your engine!
    await machine.RunAsync(BasicStateId.State1, ctxProperties);

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're registered in order
    Assert.IsTrue(enums.SequenceEqual(machine.States), "States should be registered for execution in the same order as the defined enums, StateId 1 => 2 => 3.");
  }

  [TestMethod]
  public async Task Basic_RegisterState_Executes132_SuccessTestAsync()
  {
    // Assemble
    var machine = new StateMachine<BasicStateId>();
    machine.RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State3);
    machine.RegisterState<BasicState3>(BasicStateId.State3, BasicStateId.State2);
    machine.RegisterState<BasicState2>(BasicStateId.State2);

    // Act - Start your engine!
    await machine.RunAsync(BasicStateId.State1);

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're NOT registered in order
    Assert.IsFalse(enums.SequenceEqual(machine.States), "States should NOT be registered for execution in the same order as the defined enums, StateId 1 => 2 => 3.");
  }

  /// <summary>Basic async fluent pattern state registration exiting to completion.</summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
  [TestMethod]
  public async Task Basic_RegisterState_Fluent_SuccessTestAsync()
  {
    // Assemble
    var counter = 0;
    var ctxProperties = new PropertyBag() { { ParameterCounter, counter } };

    // Assemble/Act - Start your engine!
    var machine = await new StateMachine<BasicStateId>()
      .RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2)
      .RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3)
      .RegisterState<BasicState3>(BasicStateId.State3)
      .RunAsync(BasicStateId.State1, ctxProperties, cancellationToken: TestContext.CancellationToken);

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're registered in order
    Assert.IsTrue(enums.SequenceEqual(machine.States), "States should be registered for execution in the same order as the defined enums, StateId 1 => 2 => 3.");
  }

  [TestMethod]
  public void ExportUml_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<BasicStateId>()
      .RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2)
      .RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3)
      .RegisterState<BasicState3>(BasicStateId.State3);

    // Act
    var umlBasic = machine.ExportUml([BasicStateId.State1], includeLegend: false, graphName: "BasicStateMachine");
    var umlLegend = machine.ExportUml([BasicStateId.State1], includeLegend: true, graphName: "BasicStateMachine");

    // Assert Results
    Assert.IsNotNull(umlBasic);
    Assert.IsNotNull(umlLegend);

    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.BasicStates123(false), umlBasic);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.BasicStates123(true), umlLegend);

    Assert.Contains("digraph \"BasicStateMachine\"", umlBasic);
    Assert.Contains("State1", umlBasic);
    Assert.Contains("State2", umlBasic);
    Assert.Contains("State3", umlBasic);

    Console.WriteLine(umlLegend);
  }

  [TestMethod]
  public async Task HungState_Proceeds_DefaultStateTimeout_SuccessTestAsync()
  {
    // TODO (2025-12-29 DS): Add test for ensuring the hung state was captured (i.e. State3 was skipped; state history).
    // Assemble
    var machine = new StateMachine<BasicStateId>();

    // This test will take 1 full second to complete versus
    var paramStack = new PropertyBag() { { ParameterType.HungStateAvoidance, true } };
    machine.DefaultStateTimeoutMs = 1000;

    machine.RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2);
    machine.RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3);
    machine.RegisterState<BasicState3>(BasicStateId.State3);

    // Act - Start your engine!
    await machine.RunAsync(BasicStateId.State1, paramStack);

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));
  }

  /// <summary>Context is returned at the end.</summary>
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
