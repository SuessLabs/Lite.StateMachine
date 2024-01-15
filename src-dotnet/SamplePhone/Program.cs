using LiteState;

namespace SamplePhone;

public enum State
{
  Idle,
  Running,
}

internal class Program
{
  static void Main(string[] args)
  {
    Console.WriteLine("Sample phone state machine!");

    var machine = new StateMachine();
  }
}


public class IdleState: IState
{

}
