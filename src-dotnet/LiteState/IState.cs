namespace LiteState;

public interface IState
{
  /// <summary>State is about to enter.</summary>
  void OnEntry();

  /// <summary>State entered and is being handled.</summary>
  /// <returns></returns>
  IState OnEnter();

  ///// <summary>Message received.</summary>
  // void OnMessage(Context eventContext);
}