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
      subgraph cluster_legend {
        label="Legend"; style=rounded; color=gray; fontcolor=gray;
        rankdir=LR;
        legend_start [label="Start (initial marker)", shape=plaintext];
        legend_start_sym [shape=point, label=""];
        legend_start_sym -> legend_start [style=invis];
        legend_regular [label="Regular state", shape=plaintext];
        legend_regular_sym [shape=box, label=""];
        legend_regular_sym -> legend_regular [style=invis];
        legend_composite [label="Composite (has submachine)", shape=plaintext];
        legend_composite_sym [shape=box3d, style=rounded, label=""];
        legend_composite_sym -> legend_composite [style=invis];
        legend_command [label="Command state (message-driven, timeout)", shape=plaintext];
        legend_command_sym [shape=hexagon, label=""];
        legend_command_sym -> legend_command [style=invis];
        legend_terminal [label="Terminal state (no outgoing transitions)", shape=plaintext];
        legend_terminal_sym [shape=doublecircle, label=""];
        legend_terminal_sym -> legend_terminal [style=invis];
        legend_edge [label="Edges labeled by outcome: Ok, Error, Failure", shape=plaintext];
        legend_edge_a [shape=box, label="State A"];
        legend_edge_b [shape=box, label="State B"];
        legend_edge_a -> legend_edge_b [label="Ok"];
      }
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
      subgraph cluster_legend {
        label="Legend"; style=rounded; color=gray; fontcolor=gray;
        rankdir=LR;
        legend_start [label="Start (initial marker)", shape=plaintext];
        legend_start_sym [shape=point, label=""];
        legend_start_sym -> legend_start [style=invis];
        legend_regular [label="Regular state", shape=plaintext];
        legend_regular_sym [shape=box, label=""];
        legend_regular_sym -> legend_regular [style=invis];
        legend_composite [label="Composite (has submachine)", shape=plaintext];
        legend_composite_sym [shape=box3d, style=rounded, label=""];
        legend_composite_sym -> legend_composite [style=invis];
        legend_command [label="Command state (message-driven, timeout)", shape=plaintext];
        legend_command_sym [shape=hexagon, label=""];
        legend_command_sym -> legend_command [style=invis];
        legend_terminal [label="Terminal state (no outgoing transitions)", shape=plaintext];
        legend_terminal_sym [shape=doublecircle, label=""];
        legend_terminal_sym -> legend_terminal [style=invis];
        legend_edge [label="Edges labeled by outcome: Ok, Error, Failure", shape=plaintext];
        legend_edge_a [shape=box, label="State A"];
        legend_edge_b [shape=box, label="State B"];
        legend_edge_a -> legend_edge_b [label="Ok"];
      }
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
      subgraph cluster_legend {
        label="Legend"; style=rounded; color=gray; fontcolor=gray;
        rankdir=LR;
        legend_start [label="Start (initial marker)", shape=plaintext];
        legend_start_sym [shape=point, label=""];
        legend_start_sym -> legend_start [style=invis];
        legend_regular [label="Regular state", shape=plaintext];
        legend_regular_sym [shape=box, label=""];
        legend_regular_sym -> legend_regular [style=invis];
        legend_composite [label="Composite (has submachine)", shape=plaintext];
        legend_composite_sym [shape=box3d, style=rounded, label=""];
        legend_composite_sym -> legend_composite [style=invis];
        legend_command [label="Command state (message-driven, timeout)", shape=plaintext];
        legend_command_sym [shape=hexagon, label=""];
        legend_command_sym -> legend_command [style=invis];
        legend_terminal [label="Terminal state (no outgoing transitions)", shape=plaintext];
        legend_terminal_sym [shape=doublecircle, label=""];
        legend_terminal_sym -> legend_terminal [style=invis];
        legend_edge [label="Edges labeled by outcome: Ok, Error, Failure", shape=plaintext];
        legend_edge_a [shape=box, label="State A"];
        legend_edge_b [shape=box, label="State B"];
        legend_edge_a -> legend_edge_b [label="Ok"];
      }
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
      subgraph cluster_legend {
        label="Legend"; style=rounded; color=gray; fontcolor=gray;
        rankdir=LR;
        legend_start [label="Start (initial marker)", shape=plaintext];
        legend_start_sym [shape=point, label=""];
        legend_start_sym -> legend_start [style=invis];
        legend_regular [label="Regular state", shape=plaintext];
        legend_regular_sym [shape=box, label=""];
        legend_regular_sym -> legend_regular [style=invis];
        legend_composite [label="Composite (has submachine)", shape=plaintext];
        legend_composite_sym [shape=box3d, style=rounded, label=""];
        legend_composite_sym -> legend_composite [style=invis];
        legend_command [label="Command state (message-driven, timeout)", shape=plaintext];
        legend_command_sym [shape=hexagon, label=""];
        legend_command_sym -> legend_command [style=invis];
        legend_terminal [label="Terminal state (no outgoing transitions)", shape=plaintext];
        legend_terminal_sym [shape=doublecircle, label=""];
        legend_terminal_sym -> legend_terminal [style=invis];
        legend_edge [label="Edges labeled by outcome: Ok, Error, Failure", shape=plaintext];
        legend_edge_a [shape=box, label="State A"];
        legend_edge_b [shape=box, label="State B"];
        legend_edge_a -> legend_edge_b [label="Ok"];
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
    machine.RegisterState(StateId.State1, () => new State1());
    machine.RegisterState(StateId.State2, () => new State2());
    machine.RegisterState(StateId.State3, () => new State3());
    machine.SetInitial(StateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml(includeSubmachines: true);

    // Assert
    Assert.IsNotNull(uml);
    Console.WriteLine(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlBasicStates, uml);
  }

  [TestMethod]
  public void Generates_BasicState_RegisterStateEx_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterState(StateId.State1, () => new StateEx1(StateId.State1), StateId.State2)
      .RegisterState(StateId.State2, () => new StateEx2(StateId.State2), StateId.State3)
      .RegisterState(StateId.State3, () => new StateEx3(StateId.State3))
      .SetInitialEx(StateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml(includeSubmachines: true);

    // Assert
    Assert.IsNotNull(uml);
    Console.WriteLine(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlBasicStates, uml);
  }

  [TestMethod]
  public void Generates_BasicState_RegisterStateEx_WithError_SuccessTest()
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
      .SetInitialEx(StateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml(includeSubmachines: true);

    // Assert
    Assert.IsNotNull(uml);
    Console.WriteLine(uml);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlBasicStatesWithErrorFailure, uml);
  }

  [TestMethod]
  [Ignore("Example coming soon")]
  public void Generates_CommandState_RegisterStateEx_SuccessTest()
  {
  }

  [TestMethod]
  public void Generates_CompositeState_RegisterStateEx_WithErrorAndFailure_SuccessTest()
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
      .RegisterState(StateId.State4, () => new StateEx4(StateId.State4), StateId.State5);

    machine.RegisterState(StateId.State4, (sub) =>
    {
      sub.RegisterState(StateId.State4_Sub1, () => new StateEx4_Sub1(StateId.State4_Sub1), StateId.State4_Sub2)
         .RegisterState(StateId.State4_Sub2, () => new StateEx4_Sub1(StateId.State4_Sub2))
         .SetInitialEx (StateId.State4_Sub1);
    });

    machine
      .RegisterState(StateId.State5, () => new StateEx5(StateId.State5))
      .SetInitial(StateId.State1);

    // Register - State4 Sub-States
    ////state4.Submachine
    ////  .RegisterStateEx(new StateEx4_Sub1(StateId.State4_Sub1), StateId.State4_Sub2)
    ////  .RegisterStateEx(new StateEx4_Sub1(StateId.State4_Sub2))
    ////  .SetInitial(StateId.State4_Sub1);

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
