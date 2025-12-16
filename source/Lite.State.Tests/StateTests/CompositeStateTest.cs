// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.State.Tests.StateTests;

[TestClass]
public class CompositeStateTest
{
  public const string PARAM_SUB_ENTERED = "SubEntered";
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
  public void RegisterState_TransitionWithError_ToSuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>();
    machine.RegisterState(new State1(StateId.State1));

    // Sub-state machine (Composite State + children
    // POC - Register Initial: var comState2 = new State2(StateId.State2, StateId.State2_Sub1);
    var comState2 = new State2(StateId.State2);
    machine.RegisterState(comState2);
    var subMachine = comState2.Submachine;
    subMachine.RegisterState(new State2_Sub1(StateId.State2_Sub1));
    subMachine.RegisterState(new State2_Sub2(StateId.State2_Sub2));

    machine.RegisterState(new State3(StateId.State3));

    // Configure initial states
    machine.SetInitial(StateId.State1);
    subMachine.SetInitial(StateId.State2_Sub1);

    // Act
    machine.Start();

    // Assert
    var ctxFinal = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinal);
    Assert.AreEqual(SUCCESS, ctxFinal[PARAM_SUB_ENTERED]);
  }

  [TestMethod]
  public void RegisterStateEx_Fluent_SuccessTest()
  {
    // Assemble
    var comState2 = new StateEx2(StateId.State2);

    var machine = new StateMachine<StateId>()
      .RegisterStateEx(new StateEx1(StateId.State1), StateId.State2)
      .RegisterStateEx(comState2, StateId.State3)
      .RegisterStateEx(new StateEx3(StateId.State3))
      .SetInitialEx(StateId.State1);

    comState2.Submachine
      .RegisterStateEx(new StateEx2_Sub1(StateId.State2_Sub1))
      .RegisterStateEx(new StateEx2_Sub2(StateId.State2_Sub2))
      .SetInitial(StateId.State2_Sub1);

    // Act
    machine.Start();

    // Assert
    var ctxFinal = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinal);
    Assert.AreEqual(SUCCESS, ctxFinal[PARAM_SUB_ENTERED]);
  }

  #region State Machine - Regular

  private class State1 : BaseState<StateId>
  {
    public State1(StateId id) : base(id)
    {
      AddTransition(Result.Ok, StateId.State2);
    }

    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);
  }

  /// <summary>Composite Parent State.</summary>
  private class State2 : CompositeState<StateId>
  {
    /// <param name="id"></param>
    public State2(StateId id) : base(id)
    {
      AddTransition(Result.Ok, StateId.State3);
    }

    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);
  }

  private class State2_Sub1 : BaseState<StateId>
  {
    public State2_Sub1(StateId id) : base(id)
    {
      AddTransition(Result.Ok, StateId.State2_Sub2);
    }

    public override void OnEnter(Context<StateId> context)
    {
      context.Parameters.Add(PARAM_SUB_ENTERED, SUCCESS);
      context.NextState(Result.Ok);
    }
  }

  private class State2_Sub2 : BaseState<StateId>
  {
    public State2_Sub2(StateId id) : base(id)
    {
      // NOTE: We're not defining the 'NextState' intentionally
      // to demonstrate the bubble-up
    }

    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);
  }

  private class State3(StateId id)
    : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context)
    {
      // NOTE: Not needed, as this is the "last state"
      // FUTURE CONSIDERATIONS:
      //  1. Sit at this state, as it could be intended or an error
      //  2. Or continue to allow the system to auto-exit (possible undesirable outcomes)
      // context.NextState(Result.Ok);
    }
  }

  #endregion State Machine - Regular

  #region State Machine - Fluent

  private class StateEx1(StateId id) : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);
  }

  /// <summary>Composite Parent State.</summary>
  /// <param name="id"></param>
  private class StateEx2(StateId id) : CompositeState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);
  }

  private class StateEx2_Sub1(StateId id) : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context)
    {
      context.Parameters.Add(PARAM_SUB_ENTERED, SUCCESS);
      context.NextState(Result.Ok);
    }
  }

  private class StateEx2_Sub2(StateId id) : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);
  }

  private class StateEx3(StateId id)
    : BaseState<StateId>(id);

  #endregion State Machine - Fluent
}
