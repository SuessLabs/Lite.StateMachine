// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class CompositeStateTest
{
  public const string ParameterSubStateEntered = "SubEntered";
  public const string SUCCESS = "success";

  public TestContext TestContext { get; set; }

  [TestMethod]
  public async Task Level1_Basic_RegisterHelpers_SuccessTestAsync()
  {
    // Assemble
    var machine = new StateMachine<CompositeL1StateId>();

    machine.RegisterState<CompositeL1_State1>(CompositeL1StateId.State1, CompositeL1StateId.State2);

    machine.RegisterComposite<CompositeL1_State2>(
      stateId: CompositeL1StateId.State2,
      initialChildStateId: CompositeL1StateId.State2_Sub1,
      onSuccess: CompositeL1StateId.State3);

    machine.RegisterSubState<CompositeL1_State2_Sub1>(
      stateId: CompositeL1StateId.State2_Sub1,
      parentStateId: CompositeL1StateId.State2,
      onSuccess: CompositeL1StateId.State2_Sub2);

    machine.RegisterSubState<CompositeL1_State2_Sub2>(
      stateId: CompositeL1StateId.State2_Sub2,
      parentStateId: CompositeL1StateId.State2,
      onSuccess: null,
      onError: null,
      onFailure: null);

    machine.RegisterState<CompositeL1_State3>(CompositeL1StateId.State3);

    // Act
    await machine.RunAsync(CompositeL1StateId.State1);

    // Assert
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are hit
    var enums = Enum.GetValues<CompositeL1StateId>().Cast<CompositeL1StateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're in order
    Assert.IsTrue(enums.SequenceEqual(machine.States));
  }

  /// <summary>Same as <see cref="Level1_Basic_RegisterHelpers_SuccessTestAsync"/> but using only RegisterState.</summary>
  /// <returns>Async Task.</returns>
  [TestMethod]
  public async Task Level1_Basic_RegisterState_SuccessTestAsync()
  {
    // Assemble
    // RegisterState<TStateId>(TStateId stateId, TStateId? onSuccess, TStateId? onError, TStateId? onFailure, TStateId? parentStateId, bool isCompositeParent, TStateId? initialChildStateId)
    var machine = new StateMachine<CompositeL1StateId>();
    machine.RegisterState<CompositeL1_State1>(CompositeL1StateId.State1, CompositeL1StateId.State2);
    machine.RegisterState<CompositeL1_State2>(CompositeL1StateId.State2, CompositeL1StateId.State3, null, null, null, false, CompositeL1StateId.State2_Sub1);
    machine.RegisterState<CompositeL1_State2_Sub1>(CompositeL1StateId.State2_Sub1, CompositeL1StateId.State2_Sub2, null, null, CompositeL1StateId.State2, false, null);
    machine.RegisterState<CompositeL1_State2_Sub2>(CompositeL1StateId.State2_Sub2, null, null, null, CompositeL1StateId.State2);
    machine.RegisterState<CompositeL1_State3>(CompositeL1StateId.State3);
    await machine.RunAsync(CompositeL1StateId.State1);

    // Assert
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are hit
    var enums = Enum.GetValues<CompositeL1StateId>().Cast<CompositeL1StateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're in order
    Assert.IsTrue(enums.SequenceEqual(machine.States));
  }

  [TestMethod]
  public void Level1_ExportUml_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<CompositeL1StateId>()
      .RegisterState<CompositeL1_State1>(CompositeL1StateId.State1, CompositeL1StateId.State2)
      .RegisterState<CompositeL1_State2>(CompositeL1StateId.State2, CompositeL1StateId.State3, null, null, null, false, CompositeL1StateId.State2_Sub1)
      .RegisterState<CompositeL1_State2_Sub1>(CompositeL1StateId.State2_Sub1, CompositeL1StateId.State2_Sub2, null, null, CompositeL1StateId.State2, false, null)
      .RegisterState<CompositeL1_State2_Sub2>(CompositeL1StateId.State2_Sub2, null, null, null, CompositeL1StateId.State2)
      .RegisterState<CompositeL1_State3>(CompositeL1StateId.State3);

    // Act - Generate UML
    var umlBasic = machine.ExportUml([CompositeL1StateId.State1], includeLegend: false);
    var umlLegend = machine.ExportUml([CompositeL1StateId.State1], includeLegend: true);

    // Assert
    Assert.IsNotNull(umlBasic);
    Assert.IsNotNull(umlLegend);

    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.Composite(false), umlBasic);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.Composite(true), umlLegend);
  }

  [TestMethod]
  public async Task Level1_Fluent_RegisterHelpers_SuccessTestAsync()
  {
    // Assemble/Act
    var machine = await new StateMachine<CompositeL1StateId>()
      .RegisterState<CompositeL1_State1>(CompositeL1StateId.State1, CompositeL1StateId.State2)
      .RegisterComposite<CompositeL1_State2>(CompositeL1StateId.State2, CompositeL1StateId.State2_Sub1, CompositeL1StateId.State3)
      .RegisterSubState<CompositeL1_State2_Sub1>(CompositeL1StateId.State2_Sub1, CompositeL1StateId.State2, CompositeL1StateId.State2_Sub2)
      .RegisterSubState<CompositeL1_State2_Sub2>(CompositeL1StateId.State2_Sub2, CompositeL1StateId.State2)
      .RegisterState<CompositeL1_State3>(CompositeL1StateId.State3)
      .RunAsync(CompositeL1StateId.State1, cancellationToken: TestContext.CancellationToken);

    // Assert
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are hit
    var enums = Enum.GetValues<CompositeL1StateId>().Cast<CompositeL1StateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're in order
    Assert.IsTrue(enums.SequenceEqual(machine.States));
  }

  [TestMethod]
  public void Level1_Fluent_RegisterState_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<CompositeL1StateId>()
      .RegisterState<CompositeL1_State1>(CompositeL1StateId.State1, CompositeL1StateId.State2)
      .RegisterState<CompositeL1_State2>(CompositeL1StateId.State2, CompositeL1StateId.State3, null, null, null, false, CompositeL1StateId.State2_Sub1)
      .RegisterState<CompositeL1_State2_Sub1>(CompositeL1StateId.State2_Sub1, CompositeL1StateId.State2_Sub2, null, null, CompositeL1StateId.State2, false, null)
      .RegisterState<CompositeL1_State2_Sub2>(CompositeL1StateId.State2_Sub2, null, null, null, CompositeL1StateId.State2)
      .RegisterState<CompositeL1_State3>(CompositeL1StateId.State3)
      .RunAsync(CompositeL1StateId.State1, cancellationToken: TestContext.CancellationToken)
      .GetAwaiter()
      .GetResult();

    // Assert
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are hit
    var enums = Enum.GetValues<CompositeL1StateId>().Cast<CompositeL1StateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're in order
    Assert.IsTrue(enums.SequenceEqual(machine.States));
  }
}
