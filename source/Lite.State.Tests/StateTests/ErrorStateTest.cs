// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.State.Tests.StateTests;

[TestClass]
public class ErrorStateTest
{
  public const string PARAM_TEST = "param1";
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
    // Assemble
    var machine = new StateMachine<BasicFsm>();

    machine.RegisterState(new State1());
    machine.RegisterState(new State2());
    machine.RegisterState(new State2Error(BasicFsm.State2Error));
    machine.RegisterState(new State3(BasicFsm.State3));

    // Set starting point
    machine.SetInitial(BasicFsm.State1);

    // Start your engine!
    var ctxProperties = new PropertyBag() { { PARAM_TEST, "not-finished" }, };
    machine.Start(ctxProperties);

    // Assert Results
    var ctxFinalParams = machine.Context.Parameters;

    Assert.IsNotNull(ctxFinalParams);
    Assert.AreEqual(SUCCESS, ctxFinalParams[PARAM_TEST]);
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
      context.Parameters[PARAM_TEST] = SUCCESS;
      Console.WriteLine("[State3] OnEntering");
    }
  }
}
