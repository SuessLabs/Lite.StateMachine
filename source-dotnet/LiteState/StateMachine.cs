namespace LiteState;

public class StateMachine
{
  private IState _currentState;

  public StateMachine(IState initialState)
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
