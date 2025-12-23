// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.State.Tests.StateTests;

[TestClass]
public class CompositeStateTest
{
  public const string ParameterSubStateEntered = "SubEntered";
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
    // NOTE: Composite state has 2 registrations, 1st for the state, 2nd for the children
    // POC - Register composite with initial; no longer require double-registration
    //  machine.RegisterState<State2>(stateId: StateId.State2, onSuccess: xxx, initState: StateId.State2_Sub1, subStates: (sub) => { ... });
    var machine = new StateMachine<StateId>();
    machine.RegisterState(StateId.State1, () => new State1(StateId.State1));

    machine.RegisterState(StateId.State2, () => new State2(StateId.State2), subStates: (sub) =>
    {
      sub.RegisterState(StateId.State2_Sub1, () => new State2_Sub1(StateId.State2_Sub1));
      sub.RegisterState(StateId.State2_Sub2, () => new State2_Sub2(StateId.State2_Sub2));
      sub.SetInitial(StateId.State2_Sub1);
    });

    machine.RegisterState(StateId.State3, () => new State3(StateId.State3));

    // Configure initial states
    machine.SetInitial(StateId.State1);

    // Act
    machine.Start();

    // Assert
    var ctxFinal = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinal);
    Assert.AreEqual(SUCCESS, ctxFinal[ParameterSubStateEntered]);
  }

  [TestMethod]
  public void RegisterStateEx_Fluent_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>()
      .RegisterState(StateId.State1, () => new StateEx1(StateId.State1), StateId.State2)
      .RegisterState(
        StateId.State2,
        () => new StateEx2(StateId.State2),
        onSuccess: StateId.State3,
        subStates: (sub) =>
      {
        sub
          .RegisterState(StateId.State2_Sub1, () => new StateEx2_Sub1(StateId.State2_Sub1))
          .RegisterState(StateId.State2_Sub2, () => new StateEx2_Sub2(StateId.State2_Sub2))
          .SetInitial(StateId.State2_Sub1);
      })
      .RegisterState(StateId.State3, () => new StateEx3(StateId.State3))
      .SetInitial(StateId.State1);

    // Act
    machine.Start();

    // Assert
    var ctxFinal = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinal);
    Assert.AreEqual(SUCCESS, ctxFinal[ParameterSubStateEntered]);
  }

  #region State Machine - Regular

  [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Allowed for this test")]
  private class State1 : BaseState<StateId>
  {
    public State1(StateId id)
      : base(id)
    {
      AddTransition(Result.Ok, StateId.State2);
    }

    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);
  }

  /// <summary>Composite Parent State.</summary>
  private class State2 : CompositeState<StateId>
  {
    /// <param name="id">State Id.</param>
    public State2(StateId id)
      : base(id)
    {
      AddTransition(Result.Ok, StateId.State3);
    }

    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);
  }

  private class State2_Sub1 : BaseState<StateId>
  {
    public State2_Sub1(StateId id)
      : base(id)
    {
      AddTransition(Result.Ok, StateId.State2_Sub2);
    }

    public override void OnEnter(Context<StateId> context)
    {
      context.Parameters.Add(ParameterSubStateEntered, SUCCESS);
      context.NextState(Result.Ok);
    }
  }

  private class State2_Sub2 : BaseState<StateId>
  {
    public State2_Sub2(StateId id)
      : base(id)
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

  #region State machine - Fluent

  [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Allowed for this test")]
  private class StateEx1(StateId id) : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);
  }

  /// <summary>Composite Parent State.</summary>
  /// <param name="id">State Id.</param>
  private class StateEx2(StateId id) : CompositeState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);
  }

  private class StateEx2_Sub1(StateId id) : BaseState<StateId>(id)
  {
    public override void OnEnter(Context<StateId> context)
    {
      context.Parameters.Add(ParameterSubStateEntered, SUCCESS);
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

  #endregion State machine - Fluent
}
