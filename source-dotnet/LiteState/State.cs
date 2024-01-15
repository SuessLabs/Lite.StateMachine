namespace LiteState;

public abstract class State
{
  /// <summary>State is about to enter.</summary>
  public virtual void OnEntry()
  {
  }

  /// <summary>State entered and is being handled.</summary>
  /// <returns></returns>
  public virtual State? OnEnter()
  {
    return null;
  }

  ///// <summary>Message received.</summary>
  // void OnMessage(Context eventContext);
}