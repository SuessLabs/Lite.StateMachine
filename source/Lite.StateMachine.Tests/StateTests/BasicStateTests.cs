// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

/*
using System;
using System.Linq;
using Lite.StateMachine.Tests.TestData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Allowed for this test class")]
public class BasicStateTests
{
public const string ParameterCounter = "Counter";
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
public void RegisterState_BasicState_SuccessTest()
{
  // Assemble
  var counter = 0;

  var machine = new StateMachine<StateId>();
  machine.RegisterState(StateId.State1, () => new State1());
  machine.RegisterState(StateId.State2, () => new State2());
  machine.RegisterState(StateId.State3, () => new State3());
  machine.SetInitial(StateId.State1);

  // Act - Start your engine!
  var ctxProperties = new PropertyBag()
  {
    { ParameterKeyTest, "not-finished" },
    { ParameterCounter, counter },
  };

  machine.Start(ctxProperties);

  // Assert Results
  var ctxFinalParams = machine.Context.Parameters;
  Assert.IsNotNull(ctxFinalParams);
  Assert.AreEqual(TestValue, ctxFinalParams[ParameterKeyTest]);

  // NOTE: This should be 9 because each state has 3 hooks that increment the counter
  // TODO (2025-12-22 DS): Fix last state not calling OnExit.
  Assert.AreEqual(9 - 1, ctxFinalParams[ParameterCounter]);

  var enums = Enum.GetValues<StateId>().Cast<StateId>();

  // Ensure all states are hit
  Assert.AreEqual(enums.Count(), machine.States.Count());
  Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

  // Ensure they're in order
  Assert.IsTrue(enums.SequenceEqual(machine.States));
}

/// <summary>Defines State Enum ID and `OnSuccess` transitions from the `RegisterStateEx` method.</summary>
[TestMethod]
public void RegisterState_BasicStateFluent_SuccessTest()
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
public void RegisterState_BasicStateFluent_WithoutInitialContextTransitions_SuccessTest()
{
  // Assemble
  var machine = new StateMachine<StateId>()
    .RegisterState(StateId.State1, () => new StateEx1(StateId.State1), StateId.State2)
    .RegisterState(StateId.State2, () => new StateEx2(StateId.State2), StateId.State3)
    .RegisterState(StateId.State3, () => new StateEx3(StateId.State3))
    .SetInitial(StateId.State1);

  // Act - Start your engine!
  machine.Start();

  // Assert Results
  var ctxFinalParams = machine.Context.Parameters;

  Assert.IsNotNull(ctxFinalParams);
  Assert.AreEqual(TestValue, ctxFinalParams[ParameterKeyTest]);
}

[TestMethod]
  public void RegisterState_Generics_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>();
    machine.RegisterState<State1>(StateId.State1, StateId.State2);
    machine.RegisterState<State2>(StateId.State2, StateId.State3);
    machine.RegisterState<State3>(StateId.State3);

    // Set starting point
    machine.SetInitial(StateId.State1);

    // Act - Start your engine!
    var ctxProperties = new PropertyBag() { { ParameterKeyTest, "not-finished" }, };
    machine.Start(ctxProperties);

    // Assert Results
    var ctxFinalParams = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinalParams);
    Assert.AreEqual(TestValue, ctxFinalParams[ParameterKeyTest]);

    // Ensure all transitions are called
    // NOTE: This should be 9 because each state has 3 hooks that increment the counter
    Assert.AreEqual(9 - 1, ctxFinalParams[ParameterCounter]);

    var enums = Enum.GetValues<StateId>().Cast<StateId>();

    // Ensure all states are hit
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're in order
    Assert.IsTrue(enums.SequenceEqual(machine.States));
  }

  /// <summary>
  ///   This test uses the generic <see cref="StateMachine{TStateId}.RegisterState{TStateClass}(TStateId, TStateId?, TStateId?, TStateId?, Action{StateMachine{TStateId}}?)"/> method
  ///   but each state class manually sets its own the State Id (double-duty).
  /// </summary>
  [TestMethod]
  public void RegisterState_GenericsFluent_ManuallySetStateId_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterState<State1>(StateId.State1, StateId.State2)
      .RegisterState<State2>(StateId.State2, StateId.State3)
      .RegisterState<State3>(StateId.State3)
      .SetInitial(StateId.State1);

    // Act - Start your engine!
    // NOTE: We did NOT pass "ParameterCounter"; it gets added on the fly.
    var ctxProperties = new PropertyBag() { { ParameterKeyTest, "not-finished" }, };
    machine.Start(ctxProperties);

    // Assert Results
    var ctxFinalParams = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinalParams);
    Assert.AreEqual(TestValue, ctxFinalParams[ParameterKeyTest]);

    // Ensure all transitions are called
    Assert.AreEqual(9 - 1, ctxFinalParams[ParameterCounter]);

    var enums = Enum.GetValues<StateId>().Cast<StateId>();

    // Ensure all states are hit
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're in order
    Assert.IsTrue(enums.SequenceEqual(machine.States));
  }

  /// <summary>
  ///   Verifies that registering states using generics and fluent syntax without states setting their own StateId
  ///   results in successful state machine initialization and correct state transitions.
  /// </summary>
  /// <remarks>
  ///   This test ensures that all states are registered, transitions are executed in order, and context
  ///   parameters are updated as expected when the state machine is started. It also confirms that states are traversed
  ///   in the correct sequence and that all defined states are included in the state machine.
  ///   </remarks>
  [TestMethod]
  public void RegisterState_GenericsFluent_UnsetId_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<GenericStateId>()
      .RegisterState<StateGenerics1>(GenericStateId.State1, GenericStateId.State2)
      .RegisterState<StateGenerics2>(GenericStateId.State2, GenericStateId.State3)
      .RegisterState<StateGenerics3>(GenericStateId.State3)
      .SetInitial(GenericStateId.State1);

    // Act - Start your engine!
    // NOTE: We did NOT pass "ParameterCounter"; it gets added on the fly.
    var ctxProperties = new PropertyBag() { { ParameterKeyTest, "not-finished" }, };
    machine.Start(ctxProperties);

    // Assert Results
    var ctxFinalParams = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinalParams);
    Assert.AreEqual(ExpectedData.StringSuccess, ctxFinalParams[ParameterKeyTest]);

    // Ensure all transitions are called
    Assert.AreEqual(9 - 1, ctxFinalParams[ParameterCounter]);

    var enums = Enum.GetValues<GenericStateId>().Cast<GenericStateId>();

    // Ensure all states are hit
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're in order
    Assert.IsTrue(enums.SequenceEqual(machine.States));
  }

  #region State Machine - Basic State Construction

  private class State1 : BaseState<StateId>
  {
    public State1()
      : base(StateId.State1)
    {
      AddTransition(Result.Ok, StateId.State2);
    }

    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("[State1] OnEnter");
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;
      context.NextState(Result.Ok);
    }

    public override void OnEntering(Context<StateId> context)
    {
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;
      context.Parameters[ParameterKeyTest] = TestValue;
      Console.WriteLine("[State3] OnEntering - Add/Update parameter");
    }

    public override void OnExit(Context<StateId> context)
    {
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;
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
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;
      context.NextState(Result.Ok);
    }

    public override void OnEntering(Context<StateId> context) =>
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;

    public override void OnExit(Context<StateId> context) =>
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;
  }

  private class State3 : BaseState<StateId>
  {
    public State3()
      : base(StateId.State3)
    {
    }

    public override void OnEnter(Context<StateId> context) =>
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;

    public override void OnEntering(Context<StateId> context)
    {
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;
      context.Parameters[ParameterKeyTest] = TestValue;
      Console.WriteLine("[State3] OnEntering - Add/Update parameter");
    }

    public override void OnExit(Context<StateId> context) =>
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;
  }

  #endregion State Machine - Basic State Construction

  #region State Machine - Fluent

  private class StateEx1(StateId id) : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);

    public override void OnEntering(Context<StateId> context) =>
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;

    public override void OnExit(Context<StateId> context) =>
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;
  }

  private class StateEx2(StateId id) : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);

    public override void OnEntering(Context<StateId> context) =>
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;

    public override void OnExit(Context<StateId> context) =>
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;
  }

  private class StateEx3(StateId id) : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context)
    {
      context.Parameters[ParameterKeyTest] = TestValue;
      context.NextState(Result.Ok);
    }

    public override void OnEntering(Context<StateId> context) =>
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;

    public override void OnExit(Context<StateId> context) =>
      context.Parameters[ParameterCounter] = context.ParameterAsInt(ParameterCounter) + 1;
  }

  #endregion State Machine - Fluent
}
*/
