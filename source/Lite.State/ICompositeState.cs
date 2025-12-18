// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.State;

/// <summary>Composite (hierarchical) state interface: has an owned submachine.</summary>
public interface ICompositeState<TState> : IState<TState> where TState : struct, Enum
{
  /// <summary>Gets the sub-states (<see cref="StateMachine{TState}"/>) of the composite state.</summary>
  /// <remarks>
  ///   TODO (2025-12-04 DS): Consider renaming to 'SubStates', 'Substates', or 'States'
  ///   <![CDATA[
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
  ///   ]]>
  /// </remarks>
  StateMachine<TState> Submachine { get; internal set; }
}
