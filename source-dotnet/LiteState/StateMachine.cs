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