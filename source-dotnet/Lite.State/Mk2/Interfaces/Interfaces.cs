// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace LiteState.Mk2.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;
using LiteState.Mk2;

public interface IState
{
  StateId Id { get; }
  Task OnEntering(Context context);
  Task OnEnter(Context context);
  Task OnExit(Context context);
  Task OnTimeout(Context context);
  Task OnMessage(string message, Context context);
}

public interface ICompositeState : IState
{
  IReadOnlyDictionary<StateId, IState> SubStates { get; }

  StateId? InitialSubState { get; }
}

public interface IStateMachine
{
  Task TransitionToAsync(StateId stateId, Context context);

  Task SendMessageAsync(string message, Context context);
}
