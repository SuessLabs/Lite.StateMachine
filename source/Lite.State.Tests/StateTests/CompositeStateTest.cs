// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

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
  public void RegisterStateEx_Flat_SuccessTest()
  {
    // Assemble
    var machine = new StateMachine<StateId>();
    machine.RegisterState<StateEx1>(StateId.State1, StateId.State2);
    machine.RegisterState<StateEx2>(
      StateId.State2,
      onSuccess: StateId.State3,
      subStates: (sub) =>
      {
        sub
          .RegisterState<StateEx2_Sub1>(StateId.State2_Sub1, StateId.State2_Sub2)
          .RegisterState<StateEx2_Sub2>(StateId.State2_Sub2)
          .SetInitial(StateId.State2_Sub1);
      });
    machine.RegisterState<StateEx3>(StateId.State3);
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
      .RegisterState<StateEx1>(StateId.State1, StateId.State2)
      .RegisterState<StateEx2>(
        StateId.State2,
        onSuccess: StateId.State3,
        subStates: (sub) =>
      {
        sub
          .RegisterState<StateEx2_Sub1>(StateId.State2_Sub1, StateId.State2_Sub2)
          .RegisterState<StateEx2_Sub2>(StateId.State2_Sub2)
          .SetInitial(StateId.State2_Sub1);
      })
      .RegisterState<StateEx3>(StateId.State3)
      .SetInitial(StateId.State1);

    // Act
    machine.Start();

    // Assert
    var ctxFinal = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinal);
    Assert.AreEqual(SUCCESS, ctxFinal[ParameterSubStateEntered]);
  }

  #region State machine - Fluent

  [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Allowed for this test")]
  private class StateEx1() : BaseState<StateId>()
  {
    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("State1 [OnEnter]");
      context.NextState(Result.Ok);
    }
  }

  /// <summary>Composite Parent State.</summary>
  /// <param name="id">State Id.</param>
  private class StateEx2() : CompositeState<StateId>()
  {
    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("State2 [OnEnter]");
      context.NextState(Result.Ok);
    }
  }

  private class StateEx2_Sub1() : BaseState<StateId>()
  {
    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("State2_Sub1 [OnEnter (CTX)]");
      context.Parameters.Add(ParameterSubStateEntered, SUCCESS);
      context.NextState(Result.Ok);
    }
  }

  private class StateEx2_Sub2() : BaseState<StateId>()
  {
    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("State2_Sub2 [OnEnter]");
      context.NextState(Result.Ok);
    }
  }

  private class StateEx3()
    : BaseState<StateId>()
  {
    public override void OnEnter(Context<StateId> context)
    {
      Console.WriteLine("State3 [OnEnter]");
      context.NextState(Result.Ok);
    }
  }

  #endregion State machine - Fluent
}
