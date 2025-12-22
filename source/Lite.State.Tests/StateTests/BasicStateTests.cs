// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace Lite.State.Tests.StateTests;

[TestClass]
public class BasicStateTests
{
  public const string ParameterKeyTest = "TestKey";
  public const string TestValue = "success";

  /// <summary>State definitions.</summary>
  public enum StateId
  {
    State1,
    State2,
    State3,
  }

  /// <summary>Standard basic state registration with fall-through exiting.</summary>
  [TestMethod]
  public void RegisterState_Transition_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>();
    machine.RegisterState(StateId.State1, () => new State1());
    machine.RegisterState(StateId.State2, () => new State2());
    machine.RegisterState(StateId.State3, () => new State3());

    // Set starting point
    machine.SetInitial(StateId.State1);

    // Act - Start your engine!
    var ctxProperties = new PropertyBag() { { ParameterKeyTest, "not-finished" }, };
    machine.Start(ctxProperties);

    // Assert Results
    var ctxFinalParams = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinalParams);
    Assert.AreEqual(TestValue, ctxFinalParams[ParameterKeyTest]);

    var enums = Enum.GetValues(typeof(StateId)).Cast<StateId>();

    // Ensure all states are hit
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're in order
    Assert.IsTrue(enums.SequenceEqual(machine.States));
  }

  /// <summary>Defines State Enum ID and `OnSuccess` transitions from the `RegisterStateEx` method.</summary>
  [TestMethod]
  public void RegisterStateEx_Transitions_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterState(StateId.State1, () => new StateEx1(StateId.State1), StateId.State2)
      .RegisterState(StateId.State2, () => new StateEx2(StateId.State2), StateId.State3)
      .RegisterState(StateId.State3, () => new StateEx3(StateId.State3));

    // Set starting point
    machine.SetInitial(StateId.State1);

    // Act - Start your engine!
    var ctxProperties = new PropertyBag() { { ParameterKeyTest, "not-finished" }, };
    machine.Start(ctxProperties);

    // Assert Results
    var ctxFinalParams = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinalParams);
    Assert.AreEqual(TestValue, ctxFinalParams[ParameterKeyTest]);
  }

  /// <summary>Defines State Enum ID and `OnSuccess` transitions from the `RegisterStateEx` method.</summary>
  [TestMethod]
  public void RegisterStateEx_WithoutInitialContextTransitions_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterState(StateId.State1, () => new StateEx1(StateId.State1), StateId.State2)
      .RegisterState(StateId.State2, () => new StateEx2(StateId.State2), StateId.State3)
      .RegisterState(StateId.State3, () => new StateEx3(StateId.State3))
      .SetInitialEx(StateId.State1);

    // Act - Start your engine!
    machine.Start();

    // Assert Results
    var ctxFinalParams = machine.Context.Parameters;

    Assert.IsNotNull(ctxFinalParams);
    Assert.AreEqual(TestValue, ctxFinalParams[ParameterKeyTest]);
  }

  private class State1 : BaseState<StateId>
  {
    public State1()
      : base(StateId.State1)
    {
      AddTransition(Result.Ok, StateId.State2);
    }

    public override void OnEntering(Context<StateId> context)
    {
      context.Parameters[ParameterKeyTest] = TestValue;
      Console.WriteLine("[State3] OnEntering - Add/Update parameter");
    }

    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("[State1] OnEnter");
      context.NextState(Result.Ok);
    }

    public override void OnExit(Context<StateId> context)
    {
      context.Parameters[ParameterKeyTest] = TestValue;
      Console.WriteLine("[State3] OnEntering - Add/Update parameter");
    }
  }

  private class State2 : BaseState<StateId>
  {
    public State2()
      : base(StateId.State2) =>
      AddTransition(Result.Ok, StateId.State3);

    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("[State2] OnEnter");
      context.NextState(Result.Ok);
    }
  }

  private class State3 : BaseState<StateId>
  {
    public State3()
      : base(StateId.State3)
    {
    }

    public override void OnEntering(Context<StateId> context)
    {
      context.Parameters[ParameterKeyTest] = TestValue;
      Console.WriteLine("[State3] OnEntering - Add/Update parameter");
    }
  }

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
      context.Parameters[ParameterKeyTest] = TestValue;
      context.NextState(Result.Ok);
    }
  }
}
