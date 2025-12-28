// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using Lite.State;

namespace Lite.State.Tests.StateTests;

/// <summary>
///   Proof of concept
///   Define transition at state creation and transitions
/// </summary>
[TestClass]
public class StateTransitionPocTest
{
  /*
  public const string PARAM_TEST = "param1";
  public const string SUCCESS = "success";

  public enum StateId
  {
    State1,
    State2,
    State2Error,
    State3,
  }

  [TestMethod]
  public void TransitionWithErrorToSuccessTest()
  {
    var machine = new StateMachine<StateId>();
    // NOTE: We pass in the state Enum ID separately
    machine.RegisterStateEx(stateId: StateId.State1,       stateClass: new State1(),       onSuccess: StateId.State2,  onError: null,               onFailure: null);
    machine.RegisterStateEx(stateId: StateId.State2,       stateClass: new State2(),       onSuccess: StateId.State3, onError: StateId.State2Error, onFailure: null);
    machine.RegisterStateEx(stateId: StateId.State2Error,  stateClass: new State2Error(),  onSuccess: StateId.State2);
    machine.RegisterStateEx(stateId: StateId.State3,       stateClass: new State3(),       onSuccess: null);

    // ALT-2: Lazy-loaded classes (preferred)
    // machine.RegisterStateEx<State1>(stateId: StateId.State1,           onSuccess: StateId.State2, onError: null,                onFailure: null);
    // machine.RegisterStateEx<State2>(stateId: StateId.State2,           onSuccess: StateId.State3, onError: StateId.State2Error, onFailure: null);
    // machine.RegisterStateEx<State2Error>(stateId: StateId.State2Error, onSuccess: StateId.State2);
    // machine.RegisterStateEx<State3>(stateId: StateId.State3,           onSuccess: null);


    // Set starting point
    machine.SetInitial(StateId.State1);

    // Start your engine!
    machine.Start();

    var ctxFinalParams = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinalParams);
    Assert.AreEqual(SUCCESS, ctxFinalParams[PARAM_TEST]);
  }

  //// private class State1 : IState<BasicStateTest.BasicFsm>
  private class State1 : BaseState<StateId>
  {
    public State1(StateId id) : base(id) { }

    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("[State1] OnEntering");
      context.NextState(Result.Ok);
    }
  }

  private class State2 : BaseState<StateId>
  {
    private int _counter = 0;

    public State2(StateId id) : base(id) { }

    public override void OnEnter(Context<StateId> context)
    {
      _counter++;
      Console.WriteLine($"[State2] OnEntering: Counter={_counter}");

      // On first pass, simulate an "error"
      // We'll come back again a second time and succeed.
      if (_counter == 1)
        context.NextState(Result.Error);
      else
        context.NextState(Result.Ok);
    }
  }

  /// <summary>Simulated error state handler, goes back to State2.</summary>
  private class State2Error : BaseState<StateId>
  {
    public State2Error(StateId id) : base(id) { }

    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("[State2Error] OnEntering");
      context.NextState(Result.Ok);
    }
  }

  private class State3 : BaseState<StateId>
  {
    public State3(StateId id) : base(id) { }

    public override void OnEntering(Context<StateId> context)
    {
      context.Parameter = SUCCESS;
      Console.WriteLine("[State3] OnEntering");
    }
  }
  */
}
