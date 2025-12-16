// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.State.Tests.StateTests;

[TestClass]
public class ExportUmlDotGraphTests
{
  private const string ExpectedUmlBasicStates = """
    digraph StateMachine {
      rankdir=LR;
      compound=true;
      node [fontname="Segoe UI", fontsize=10];
      edge [fontname="Segoe UI", fontsize=10];
      start [shape=point];
      start -> "State1";
      "State1" [shape=box];
      "State2" [shape=box];
      "State3" [shape=doublecircle];
      "State1" -> "State2" [label="Ok"];
      "State2" -> "State3" [label="Ok"];
    }
    """;

  private const string ExpectedUmlBasicStatesWithError = """
    digraph StateMachine {
      rankdir=LR;
      compound=true;
      node [fontname="Segoe UI", fontsize=10];
      edge [fontname="Segoe UI", fontsize=10];
      start [shape=point];
      start -> "State1";
      "State1" [shape=box];
      "State2" [shape=box];
      "State2e" [shape=box];
      "State3" [shape=doublecircle];
      "State1" -> "State2" [label="Ok"];
      "State2" -> "State3" [label="Ok"];
      "State2" -> "State2e" [label="Error"];
      "State2e" -> "State2" [label="Ok"];
    }
    """;

  private const string ExpectedUmlBasicStatesWithErrorFailure = """
    digraph StateMachine {
      rankdir=LR;
      compound=true;
      node [fontname="Segoe UI", fontsize=10];
      edge [fontname="Segoe UI", fontsize=10];
      start [shape=point];
      start -> "State1";
      "State1" [shape=box];
      "State2" [shape=box];
      "State2e" [shape=box];
      "State2f" [shape=box];
      "State3" [shape=doublecircle];
      "State1" -> "State2" [label="Ok"];
      "State2" -> "State3" [label="Ok"];
      "State2" -> "State2e" [label="Error"];
      "State2" -> "State2f" [label="Failure"];
      "State2e" -> "State2" [label="Ok"];
      "State2f" -> "State1" [label="Ok"];
    }
    """;

  private const string ExpectedUmlComposite = """
    digraph StateMachine {
      rankdir=LR;
      compound=true;
      node [fontname="Segoe UI", fontsize=10];
      edge [fontname="Segoe UI", fontsize=10];
      start [shape=point];
      start -> "State1";
      "State1" [shape=box];
      "State2" [shape=box];
      "State2e" [shape=box];
      "State2f" [shape=box];
      "State3" [shape=box];
      "State4" [shape=box3d, style=rounded];
      "State5" [shape=doublecircle];
      "State1" -> "State2" [label="Ok"];
      "State2" -> "State3" [label="Ok"];
      "State2" -> "State2e" [label="Error"];
      "State2" -> "State2f" [label="Failure"];
      "State2e" -> "State2" [label="Ok"];
      "State2f" -> "State1" [label="Ok"];
      "State3" -> "State4" [label="Ok"];
      "State4" -> "State5" [label="Ok"];
      subgraph cluster_State4 {
        label="State4"; style=rounded; color=lightgray; fontcolor=gray;
        rankdir=LR;
        "start_State4" [shape=point];
        "start_State4" -> "State4_Sub1";
        "State4_Sub1" [shape=box];
        "State4_Sub2" [shape=doublecircle];
        "State4_Sub1" -> "State4_Sub2" [label="Ok"];
      }
    }
    """;

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

  [TestMethod]
  public void Generates_BasicState_RegisterState_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>();
    machine.RegisterState(new State1());
    machine.RegisterState(new State2());
    machine.RegisterState(new State3());
    machine.SetInitial(StateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml(includeSubmachines: true);

    // Assert
    Assert.IsNotNull(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlBasicStates, uml);
  }

  [TestMethod]
  public void Generates_BasicState_RegisterStateEx_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterStateEx(new StateEx1(StateId.State1), StateId.State2)
      .RegisterStateEx(new StateEx2(StateId.State2), StateId.State3)
      .RegisterStateEx(new StateEx3(StateId.State3))
      .SetInitialEx(StateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml(includeSubmachines: true);

    // Assert
    Assert.IsNotNull(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlBasicStates, uml);
  }

  [TestMethod]
  public void Generates_BasicState_RegisterStateEx_WithError_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterStateEx(new StateEx1(StateId.State1), StateId.State2)
      .RegisterStateEx(
        state: new StateEx2(StateId.State2),
        onSuccess: StateId.State3,
        onError: StateId.State2e)
      .RegisterStateEx(new StateEx2e(StateId.State2e), StateId.State2)
      .RegisterStateEx(new StateEx3(StateId.State3))
      .SetInitialEx(StateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml(includeSubmachines: true);

    // Assert
    Assert.IsNotNull(uml);
    Console.WriteLine(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlBasicStatesWithError, uml);
  }

  [TestMethod]
  public void Generates_BasicState_RegisterStateEx_WithErrorAndFailure_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterStateEx(new StateEx1(StateId.State1), StateId.State2)
      .RegisterStateEx(
        state: new StateEx2(StateId.State2),
        onSuccess: StateId.State3,
        onError: StateId.State2e,
        onFailure: StateId.State2f)
      .RegisterStateEx(new StateEx2e(StateId.State2e), StateId.State2)
      .RegisterStateEx(new StateEx2e(StateId.State2f), StateId.State1)
      .RegisterStateEx(new StateEx3(StateId.State3))
      .SetInitialEx(StateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml(includeSubmachines: true);

    // Assert
    Assert.IsNotNull(uml);
    Console.WriteLine(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlBasicStatesWithErrorFailure, uml);
  }

  [TestMethod]
  public void Generates_CompositeState_RegisterStateEx_WithErrorAndFailure_SuccessTest()
  {
    // Assemble
    var state4 = new StateEx4(StateId.State4);

    var machine = new StateMachine<StateId>()
      .RegisterStateEx(new StateEx1(StateId.State1), StateId.State2)
      .RegisterStateEx(
        state: new StateEx2(StateId.State2),
        onSuccess: StateId.State3,
        onError: StateId.State2e,
        onFailure: StateId.State2f)
      .RegisterStateEx(new StateEx2e(StateId.State2e), StateId.State2)
      .RegisterStateEx(new StateEx2e(StateId.State2f), StateId.State1)
      .RegisterStateEx(new StateEx3(StateId.State3), StateId.State4)
      .RegisterStateEx(state4, StateId.State5)
      .RegisterStateEx(new StateEx5(StateId.State5))
      .SetInitialEx(StateId.State1);

    // Register - State4 Sub-States
    state4.Submachine.RegisterStateEx(new StateEx4_Sub1(StateId.State4_Sub1), StateId.State4_Sub2);
    state4.Submachine.RegisterStateEx(new StateEx4_Sub1(StateId.State4_Sub2));
    state4.Submachine.SetInitial(StateId.State4_Sub1);

    // Act - Generate UML
    var uml = machine.ExportUml(includeSubmachines: true);

    // Assert
    Assert.IsNotNull(uml);
    Console.WriteLine(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlComposite, uml);
  }

  #region State Machine - Generic

  private class State1 : BaseState<StateId>
  {
    public State1() : base(StateId.State1)
    {
      AddTransition(Result.Ok, StateId.State2);
    }
  }

  private class State2 : BaseState<StateId>
  {
    public State2() : base(StateId.State2) =>
      AddTransition(Result.Ok, StateId.State3);
  }

  private class State3 : BaseState<StateId>
  {
    public State3() : base(StateId.State3)
    {
    }
  }

  #endregion State Machine - Generic

  #region State Machine Ex

  private class StateEx1(StateId id) : BaseState<StateId>(id);

  private class StateEx2(StateId id) : BaseState<StateId>(id);

  /// <summary>Error state tests.</summary>
  /// <param name="id">StateId</param>
  private class StateEx2e(StateId id) : BaseState<StateId>(id);

  /// <summary>Failure state tests.</summary>
  /// <param name="id">StateId</param>
  private class StateEx2f(StateId id) : BaseState<StateId>(id);

  private class StateEx3(StateId id) : BaseState<StateId>(id);

  // Composite state tests

  private class StateEx4(StateId id) : CompositeState<StateId>(id);

  private class StateEx4_Sub1(StateId id) : BaseState<StateId>(id);

  private class StateEx4_Sub2(StateId id) : BaseState<StateId>(id);

  private class StateEx5(StateId id) : BaseState<StateId>(id);

  #endregion State Machine Ex
}
