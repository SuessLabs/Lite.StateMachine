// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using LiteState;

namespace Sample.FsmTimeout;

public class IdleState : IState
{
  public string Name => nameof(IdleState);

  public void OnEnter(StateMachine fsm)
  {
    Console.WriteLine("[Idle] Entered IdleState.");
  }

  public void OnExit(StateMachine fsm)
  {
    Console.WriteLine("[Idle] Exiting IdleState.");
  }
}
