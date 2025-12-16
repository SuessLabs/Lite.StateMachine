// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.State;

using System;

/// <summary>
/// A base class for composite states. The submachine is injected/assigned externally.
/// </summary>
public abstract class CompositeState<TState> : BaseState<TState>, ICompositeState<TState> where TState : struct, Enum
{
  protected CompositeState(TState id) : base(id)
  {
    // : base(name, logger)
  }

  public override bool IsComposite => true;

  /// <summary>Sub-states State Machine.</summary>
  /// <remarks>
  /// <![CDATA[
  ///   var comState2 = new StateEx2(StateId.State2);
  ///
  ///   var machine = new StateMachine<StateId>()
  ///     .RegisterStateEx(new StateEx1(StateId.State1), StateId.State2)
  ///     .RegisterStateEx(comState2, StateId.State3)
  ///     .RegisterStateEx(new StateEx3(StateId.State3))
  ///     .SetInitialEx(StateId.State1);
  ///
  ///   comState2.Submachine
  ///     .RegisterStateEx(new StateEx2_Sub1(StateId.State2_Sub1))
  ///     .RegisterStateEx(new StateEx2_Sub2(StateId.State2_Sub2))
  ///     .SetInitial(StateId.State2_Sub1);
  ///
  ///   machine.Start();
  /// ]]></remarks>
  public StateMachine<TState> Submachine { get; internal set; } = default!;
}
