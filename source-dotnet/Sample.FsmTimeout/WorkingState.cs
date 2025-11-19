using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteState;

namespace Sample.FsmTimeout;

public class WorkingState : IStateWithTimeout
{
  // Example: if we stay in this state for > 3 seconds, OnTimeout will be called.
  public string Name => nameof(WorkingState);
  public TimeSpan Timeout { get; }

  public WorkingState(TimeSpan? timeout = null)
  {
    Timeout = timeout ?? TimeSpan.FromSeconds(3);
  }

  public void OnEnter(StateMachine fsm)
  {
    Console.WriteLine("[Working] Started work; a timeout is scheduled in " + Timeout);
    // Simulate non-blocking work or start tasks as desired.
  }

  public void OnExit(StateMachine fsm)
  {
    Console.WriteLine("[Working] Stopped work (leaving WorkingState).");
  }

  public void OnTimeout(StateMachine fsm)
  {
    Console.WriteLine("[Working] Timeout expired while in WorkingState. Transitioning to TimedOutState.");
    // Example automatic transition on timeout:
    fsm.TransitionTo(new TimedOutState());
  }
}
