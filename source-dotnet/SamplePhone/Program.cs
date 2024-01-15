using LiteState;

namespace SamplePhone;

public enum PhoneState
{
  Idle,
  Ringing,
  HangUp,
}

internal class Program
{
  static void Main(string[] args)
  {
    Console.WriteLine("Sample phone state machine!");

    var machine = new StateMachine();
  }
}

public class IdleState : State
{
  public State OnEnter()
  {
  }

  public void OnEntry()
  {
  }
}
