// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace LiteState;

public class StateMachine
{
  private State _currentState;

  public StateMachine()
  {
  }

  public StateMachine(State initialState)
  {
    InitialState(initialState);
  }

  public void InitialState(State initialState)
  {
    _currentState = initialState;
  }

  public void Update()
  {
    var nextState = _currentState.OnEnter();
    if (nextState != null)
    {
      _currentState = nextState;
    }
  }
}