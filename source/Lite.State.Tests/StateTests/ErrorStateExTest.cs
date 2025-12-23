// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.State.Tests.StateTests;

[TestClass]
public class ErrorStateExTest
{
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
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterState(StateId.State1, () => new State1(StateId.State1), StateId.State2)
      .RegisterState(StateId.State2, () => new State2(StateId.State2), StateId.State3, StateId.State2Error)
      .RegisterState(StateId.State2Error, () => new State2Error(StateId.State2Error), StateId.State2)
      .RegisterState(StateId.State3, () => new State3(StateId.State3))
      .SetInitial(StateId.State1);

    // Act - Start your engine!
    var ctxProperties = new PropertyBag() { { PARAM_TEST, "not-finished" }, };
    machine.Start(ctxProperties);

    // Assert Results
    var ctxFinalParams = machine.Context.Parameters;

    Assert.IsNotNull(ctxFinalParams);
    Assert.AreEqual(SUCCESS, ctxFinalParams[PARAM_TEST]);
  }

  //// private class State1 : IState<BasicStateTest.BasicFsm>
  private class State1(StateId id)
    : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("[State1] OnEntering");
      context.NextState(Result.Ok);
    }
  }

  private class State2(StateId id)
    : BaseState<StateId>(id)
  {
    private int _counter = 0;

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
  private class State2Error(StateId id)
    : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("[State2Error] OnEntering");
      context.NextState(Result.Ok);
    }
  }

  private class State3(StateId id)
    : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);

    public override void OnEntering(Context<StateId> context)
    {
      context.Parameters[PARAM_TEST] = SUCCESS;
      Console.WriteLine("[State3] OnEntering");
    }
  }
}
