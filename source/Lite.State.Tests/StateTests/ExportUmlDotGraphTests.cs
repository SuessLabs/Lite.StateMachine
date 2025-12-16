// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.State.Tests.StateTests;

[TestClass]
public class ExportUmlDotGraphTests
{
  private const string ExpectedUml = """
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

  /// <summary>State definitions.</summary>
  public enum StateId
  {
    State1,
    State2,
    State3,
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
    AssertExtensions.AreEqualIgnoreLines(ExpectedUml, uml);
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
    AssertExtensions.AreEqualIgnoreLines(ExpectedUml, uml);
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

  private class StateEx1(StateId id) : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context) => context.NextState(Result.Ok);
  }

  private class StateEx2(StateId id) : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context) => context.NextState(Result.Ok);
  }

  private class StateEx3(StateId id) : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context)
    {
    }
  }

  #endregion State Machine Ex
}
