// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using Lite.State;

namespace Lite.State.Tests;

[TestClass]
public class StateTransitionPocTest
{
  /*
  public const string SUCCESS = "success";

  public enum BasicFsm
  {
    State1,
    State2,
    State2Error,
    State3,
  }

  [TestMethod]
  public void TransitionWithErrorToSuccessTest()
  {
    var machine = new StateMachine<BasicFsm>();
    machine.RegisterState(stateId: BasicFsm.State1,       stateClass: new State1(),       onSuccess: BaseFsm.State2,  onError: null,                 onFailure: null);
    machine.RegisterState(stateid: BasicFsm.State2,       stateClass: new State2(),       onSuccess: BasicFsm.State3, onError: BasicFsm.State2Error, onFailure: null);
    machine.RegisterState(stateid: BasicFsm.State2Error,  stateClass: new State2Error(),  onSuccess: BasicFsm.State2);
    machine.RegisterState(stateid: BasicFsm.State3,       stateClass: new State3(),       onSuccess: null);

    // Set starting point
    machine.SetInitial(BasicFsm.State1);

    // Start your engine!
    machine.Start("param-test");

    var finalParam = machine.Context.Parameter;

    Assert.AreEqual(SUCCESS, finalParam);
  }

  //// private class State1 : IState<BasicStateTest.BasicFsm>
  private class State1 : BaseState<BasicFsm>
  {
    public State1() : base(BasicFsm.State1)
    {
      AddTransition(Result.Ok, BasicFsm.State2);
    }

    public override void OnEnter(Context<BasicFsm> context)
    {
      Console.WriteLine("[State1] OnEntering");
      context.NextState(Result.Ok);
    }
  }

  private class State2 : BaseState<BasicFsm>
  {
    private int _counter = 0;

    public State2() : base(BasicFsm.State2)
    {
      AddTransition(Result.Ok, BasicFsm.State3);
      AddTransition(Result.Error, BasicFsm.State2Error);
    }

    public override void OnEnter(Context<BasicFsm> context)
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
  private class State2Error : BaseState<BasicFsm>
  {
    public State2Error(BasicFsm id) : base(id)
    {
      AddTransition(Result.Ok, BasicFsm.State2);
    }

    public override void OnEnter(Context<BasicFsm> context)
    {
      Console.WriteLine("[State2Error] OnEntering");
      context.NextState(Result.Ok);
    }
  }

  private class State3 : BaseState<BasicFsm>
  {
    public State3(BasicFsm id) : base(id)
    {
    }

    public override void OnEntering(Context<BasicFsm> context)
    {
      context.Parameter = SUCCESS;
      Console.WriteLine("[State3] OnEntering");
    }
  }
  */
}
