// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using Lite.StateMachine.Tests.TestData;

namespace Lite.StateMachine.Tests.StateTests;

/// <summary>Tests Exporting of UML to DOT Graph format.</summary>
/// <remarks>
///   TODO (2025-12-22 DS): Make outputting legend optional via parameter.
/// </remarks>
[TestClass]
public class ExportUmlDotGraphTests
{
  /// <summary>State definitions.</summary>
  public enum StateId
  {
    State1,
    State2,
    State2e,
    State2f,
    State3,
    State4,
    State4_Sub1,
    State4_Sub2,
    State5,
  }

  /*
  [TestMethod]
  public void Generates_BasicState_RegisterState_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>();
    machine.RegisterState(StateId.State1, () => new State1());
    machine.RegisterState(StateId.State2, () => new State2());
    machine.RegisterState(StateId.State3, () => new State3());
    machine.SetInitial(StateId.State1);

    // Act/Assert
    var uml = machine.ExportUml(appendLegend: true);
    Assert.IsNotNull(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.BasicStates(true), uml);

    uml = machine.ExportUml();
    Assert.IsNotNull(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.BasicStates(), uml);
  }
  */

  [TestMethod]
  public void Generates_BasicState_RegisterStateGenerics_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>();
    machine.RegisterState<GenericsState1>(StateId.State1);
    machine.RegisterState<GenericsState2>(StateId.State2);
    machine.RegisterState<GenericsState3>(StateId.State3);
    machine.SetInitial(StateId.State1);

    // Act/Assert
    var uml = machine.ExportUml();
    Assert.IsNotNull(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.BasicStates(), uml);
  }

  /*
  [TestMethod]
  public void Generates_BasicState_RegisterState_WithError_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterState(StateId.State1, () => new StateEx1(StateId.State1), StateId.State2)
      .RegisterState(
        stateId: StateId.State2,
        state: () => new StateEx2(StateId.State2),
        onSuccess: StateId.State3,
        onError: StateId.State2e)
      .RegisterState(StateId.State2e, () => new StateEx2e(StateId.State2e), StateId.State2)
      .RegisterState(StateId.State3, () => new StateEx3(StateId.State3))
      .SetInitial(StateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml(appendLegend: true);

    // Assert
    Assert.IsNotNull(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.BasicStatesWithError(true), uml);
  }

  [TestMethod]
  public void Generates_BasicState_RegisterState_WithErrorAndFailure_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterState(StateId.State1, () => new StateEx1(StateId.State1), StateId.State2)
      .RegisterState(
        stateId: StateId.State2,
        state: () => new StateEx2(StateId.State2),
        onSuccess: StateId.State3,
        onError: StateId.State2e,
        onFailure: StateId.State2f)
      .RegisterState(StateId.State2e, () => new StateEx2e(StateId.State2e), StateId.State2)
      .RegisterState(StateId.State2f, () => new StateEx2e(StateId.State2f), StateId.State1)
      .RegisterState(StateId.State3, () => new StateEx3(StateId.State3))
      .SetInitial(StateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml(appendLegend: true);

    // Assert
    Assert.IsNotNull(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.BasicStatesWithErrorFailure(true), uml);
  }

  [TestMethod]
  public void Generates_BasicState_RegisterStateWithAndWithLegend_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterState(StateId.State1, () => new StateEx1(StateId.State1), StateId.State2)
      .RegisterState(StateId.State2, () => new StateEx2(StateId.State2), StateId.State3)
      .RegisterState(StateId.State3, () => new StateEx3(StateId.State3))
      .SetInitial(StateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml(appendLegend: false);
    Assert.IsNotNull(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.BasicStates(), uml);

    uml = machine.ExportUml(appendLegend: true);
    Assert.IsNotNull(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.BasicStates(true), uml);
  }
  [TestMethod]
  [Ignore("Example coming soon")]
  public void Generates_CommandState_RegisterStateEx_SuccessTest()
  {
  }

  [TestMethod]
  public void Generates_CompositeState_RegisterState_WithErrorAndFailure_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterState(StateId.State1, () => new StateEx1(StateId.State1), StateId.State2)
      .RegisterState(
        stateId: StateId.State2,
        state: () => new StateEx2(StateId.State2),
        onSuccess: StateId.State3,
        onError: StateId.State2e,
        onFailure: StateId.State2f)
      .RegisterState(StateId.State2e, () => new StateEx2e(StateId.State2e), StateId.State2)
      .RegisterState(StateId.State2f, () => new StateEx2e(StateId.State2f), StateId.State1)
      .RegisterState(StateId.State3, () => new StateEx3(StateId.State3), StateId.State4)
      .RegisterState(
        StateId.State4,
        () => new StateEx4(StateId.State4),
        onSuccess: StateId.State5,
        subStates: (sub) =>
      {
        sub.RegisterState(StateId.State4_Sub1, () => new StateEx4_Sub1(StateId.State4_Sub1), StateId.State4_Sub2)
           .RegisterState(StateId.State4_Sub2, () => new StateEx4_Sub1(StateId.State4_Sub2))
           .SetInitial(StateId.State4_Sub1);
      })
      .RegisterState(StateId.State5, () => new StateEx5(StateId.State5))
      .SetInitial(StateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml(appendLegend: true);

    // Assert
    Assert.IsNotNull(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.Composite(true), uml);
  }
  */

  #region State Machine - Basic

  [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Allowed for this test")]
  private class State1 : BaseState<StateId>
  {
    public State1()
      : base(StateId.State1)
    {
      AddTransition(Result.Ok, StateId.State2);
    }
  }

  private class State2 : BaseState<StateId>
  {
    public State2()
      : base(StateId.State2) =>
      AddTransition(Result.Ok, StateId.State3);
  }

  private class State3 : BaseState<StateId>
  {
    public State3()
      : base(StateId.State3)
    {
    }
  }

  #endregion State Machine - Basic

  #region State Machine - Generics

  [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Allowed for this test")]
  private class GenericsState1 : BaseState<StateId>
  {
    public GenericsState1()
    {
      AddTransition(Result.Ok, StateId.State2);
    }
  }

  private class GenericsState2 : BaseState<StateId>
  {
    public GenericsState2() =>
      AddTransition(Result.Ok, StateId.State3);
  }

  private class GenericsState3 : BaseState<StateId>
  {
    public GenericsState3()
    {
    }
  }

  #endregion State Machine - Generics

  #region State Machine - Fluent

  [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Allowed for this test")]
  private class StateEx1(StateId id) : BaseState<StateId>(id);

  private class StateEx2(StateId id) : BaseState<StateId>(id);

  /// <summary>Error state tests.</summary>
  /// <param name="id">StateId.</param>
  private class StateEx2e(StateId id) : BaseState<StateId>(id);

  /// <summary>Failure state tests.</summary>
  /// <param name="id">StateId.</param>
  private class StateEx2f(StateId id) : BaseState<StateId>(id);

  private class StateEx3(StateId id) : BaseState<StateId>(id);

  private class StateEx4(StateId id) : CompositeState<StateId>(id);

  private class StateEx4_Sub1(StateId id) : BaseState<StateId>(id);

  private class StateEx4_Sub2(StateId id) : BaseState<StateId>(id);

  private class StateEx5(StateId id) : BaseState<StateId>(id);

  #endregion State Machine - Fluent
}
