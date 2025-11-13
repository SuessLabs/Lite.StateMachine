// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using LiteState;

namespace Sample.Phone;

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
