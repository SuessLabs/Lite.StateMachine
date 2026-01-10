// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData;
using Lite.StateMachine.Tests.TestData.States;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class BasicStateTests : TestBase
{
  /// <summary>Standard synchronous state registration exiting to completion.</summary>
  [TestMethod]
  public void Basic_RegisterState_Executes123_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<BasicStateId>();
    machine.RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2);
    machine.RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3);
    machine.RegisterState<BasicState3>(BasicStateId.State3);
    machine.AddContext(new()
    {
      { ParameterType.Counter, 0 },
      { ParameterType.TestExecutionOrder, true },
    });

    // Act - Non async Start your engine!
    var task = machine.RunAsync(BasicStateId.State1);
    task.GetAwaiter().GetResult();

    // Assert Results
    AssertMachineNotNull(machine);

    Assert.IsNotEmpty(machine.Context.Parameters);
    Assert.AreEqual(9, machine.Context.ParameterAsInt(ParameterType.Counter));
    Assert.IsTrue(machine.Context.ParameterAsBool(ParameterType.TestExecutionOrder));

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.HasCount(enums.Count(), machine.States);
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
    var machine = new StateMachine<BasicStateId>();
    machine.RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2);
    machine.RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3);
    machine.RegisterState<BasicState3>(BasicStateId.State3);

    machine.Context.Parameters = new()
    {
      { ParameterType.Counter, 0 },
      { ParameterType.TestExecutionOrder, true },
    };

    // Act - Start your engine!
    await machine.RunAsync(BasicStateId.State1);

    // Assert Results
    AssertMachineNotNull(machine);

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.HasCount(enums.Count(), machine.States);
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
    machine.Context.Parameters = new() { { ParameterType.TestExecutionOrder, false } };
    await machine.RunAsync(BasicStateId.State1);

    // Assert Results
    AssertMachineNotNull(machine);

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.HasCount(enums.Count(), machine.States);
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
    var ctxProperties = new PropertyBag()
    {
      { ParameterType.Counter, 0 },
      { ParameterType.TestExecutionOrder, true },
    };

    // Assemble/Act - Start your engine!
    var machine = await new StateMachine<BasicStateId>()
      .RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2)
      .RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3)
      .RegisterState<BasicState3>(BasicStateId.State3)
      .AddContext(ctxProperties)
      .RunAsync(BasicStateId.State1, cancellationToken: TestContext.CancellationToken);

    // Assert Results
    AssertMachineNotNull(machine);

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.HasCount(enums.Count(), machine.States);
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
    var ctxProperties = new PropertyBag()
    {
      { ParameterType.TestHungStateAvoidance, true },
      { ParameterType.TestExecutionOrder, true },
    };

    machine.DefaultStateTimeoutMs = 1000;

    machine.RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2);
    machine.RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3);
    machine.RegisterState<BasicState3>(BasicStateId.State3);
    machine.AddContext(ctxProperties);

    // Act - Start your engine!
    await machine.RunAsync(BasicStateId.State1);

    // Assert Results
    AssertMachineNotNull(machine);

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.HasCount(enums.Count(), machine.States);
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));
  }
}
