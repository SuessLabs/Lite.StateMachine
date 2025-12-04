// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.State.Tests.StateTests;

[TestClass]
public class CompositStateTest
{
  public const string PARAM_TEST = "SubEntered";
  public const string SUCCESS = "success";

  public enum StateId
  {
    State1,
    State2,
    State2_Sub1,
    State2_Sub2,
    State3,
  }

  [TestMethod]
  public void TransitionWithErrorToSuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>();
    machine.RegisterState(new State1(StateId.State1));
    machine.RegisterState(new State2(StateId.State2));

    var machineState2 = new StateMachine<StateId>();
    machineState2.RegisterState(new State2_Sub1(StateId.State2_Sub1));
    machineState2.RegisterState(new State2_Sub2(StateId.State2_Sub2));

    machine.RegisterState(new State3(StateId.State3));

    // Act
    machine.SetInitial(StateId.State1);
    machine.Start();

    // Assert
    var ctxFinal = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinal);
    Assert.AreEqual(SUCCESS, ctxFinal[PARAM_TEST]);
  }

  private class State1(StateId id)
    : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context)
    {
      context.NextState(Result.Ok);
    }
  }

  private class State2(StateId id)
    : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context)
    {
      context.NextState(Result.Ok);
    }
  }

  private class State2_Sub1(StateId id)
    : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context)
    {
      context.NextState(Result.Ok);
    }
  }

  private class State2_Sub2(StateId id)
    : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context)
    {
      context.NextState(Result.Ok);
    }
  }

  private class State3(StateId id)
    : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context)
    {
      context.NextState(Result.Ok);
    }
  }
}
