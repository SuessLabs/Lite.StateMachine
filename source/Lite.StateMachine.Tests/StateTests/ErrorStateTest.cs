// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class ErrorStateTest
{
  public const string ParameterTest = "param1";
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
    // Assemble
    var machine = new StateMachine<StateId>();

    machine.RegisterState<State1>(StateId.State1);
    machine.RegisterState<State2>(StateId.State2);
    machine.RegisterState<State2Error>(StateId.State2Error);
    machine.RegisterState<State3>(StateId.State3);

    // Set starting point
    machine.SetInitial(StateId.State1);

    // Act - Start your engine!
    var ctxProperties = new PropertyBag() { { ParameterTest, "not-finished" }, };
    machine.Start(ctxProperties);

    // Assert Results
    var ctxFinalParams = machine.Context.Parameters;

    Assert.IsNotNull(ctxFinalParams);
    Assert.AreEqual(SUCCESS, ctxFinalParams[ParameterTest]);
  }

  //// private class State1 : IState<BasicStateTest.BasicFsm>
  private class State1 : BaseState<StateId>
  {
    public State1()
     : base()
    {
      AddTransition(Result.Ok, StateId.State2);
    }

    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("[State1] OnEntering");
      context.NextState(Result.Ok);
    }
  }

  private class State2 : BaseState<StateId>
  {
    private int _counter = 0;

    public State2()
      : base()
    {
      AddTransition(Result.Ok, StateId.State3);
      AddTransition(Result.Error, StateId.State2Error);
    }

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
    public State2Error()
     : base()
    {
      AddTransition(Result.Ok, StateId.State2);
    }

    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("[State2Error] OnEntering");
      context.NextState(Result.Ok);
    }
  }

  private class State3() : BaseState<StateId>()
  {
    public override void OnEntering(Context<StateId> context)
    {
      // TODO: Wait a sec.. we never "said to exit"
      context.Parameters[ParameterTest] = SUCCESS;
      Console.WriteLine("[State3] OnEntering");
    }
  }
}
