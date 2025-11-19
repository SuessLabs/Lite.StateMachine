using System;
using System.Threading;
using LiteState;
using Sample.FsmTimeout;

internal class Program
{
  private static void Main(string[] args)
  {
    using var fsm = new StateMachine();

    // Start in Idle
    fsm.EnterState(new IdleState());

    // Transition to Working: it has an automatic timeout (3s), after which it will transition to TimedOutState.
    Console.WriteLine("Transitioning to WorkingState (3s timeout)...");
    fsm.TransitionTo(new WorkingState(TimeSpan.FromSeconds(3)));

    // Wait to observe timeout behavior
    Thread.Sleep(4500);

    // From TimedOutState go back to Idle manually
    Console.WriteLine("Transitioning back to IdleState manually...");
    fsm.TransitionTo(new IdleState());

    // Another example: enter a WorkingState but leave it before timeout
    Console.WriteLine("Entering WorkingState again (we will leave before timeout)...");
    fsm.TransitionTo(new WorkingState(TimeSpan.FromSeconds(5)));
    // Leave early after 1s
    Thread.Sleep(1000);
    Console.WriteLine("Leaving WorkingState early (before timeout)...");
    fsm.TransitionTo(new IdleState());

    // Give any pending console output time to flush
    Thread.Sleep(500);
  }
}
