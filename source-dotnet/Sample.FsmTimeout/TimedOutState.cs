// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using LiteState;

namespace Sample.FsmTimeout;

public class TimedOutState : IState
{
  public string Name => nameof(TimedOutState);

  public void OnEnter(StateMachine fsm)
  {
    Console.WriteLine("[TimedOut] Entered TimedOutState.");
  }

  public void OnExit(StateMachine fsm)
  {
    Console.WriteLine("[TimedOut] Exiting TimedOutState.");
  }
}
