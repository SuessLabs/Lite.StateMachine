// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace LiteState;

/// <summary>
/// A simple, thread-safe finite state machine.
/// - Call EnterState / TransitionTo to change states.
/// - If the entered state implements IStateWithTimeout, a one-shot timer is started.
///   When the timer expires and the state is still the active one, the state's OnTimeout is invoked.
/// </summary>
public class StateMachine : IDisposable
{
  private readonly object _lock = new();
  private IState? _currentState;
  private Timer? _timer;
  private bool _disposed;

  public IState? CurrentState
  {
    get
    {
      lock (_lock)
        return _currentState;
    }
  }

  public StateMachine()
  {
  }

  /// <summary>
  /// Enter the provided state immediately (calls OnExit on previous, OnEnter on new).
  /// If the new state provides a timeout (IStateWithTimeout), a timer will be started.
  /// </summary>
  public void EnterState(IState newState)
  {
    ArgumentNullException.ThrowIfNull(newState);

    lock (_lock)
    {
      ////if (_disposed)
      ////  throw new ObjectDisposedException(nameof(StateMachine));
      ObjectDisposedException.ThrowIf(_disposed, this);

      if (ReferenceEquals(newState, _currentState))
        return;

      var previous = _currentState;
      try
      {
        previous?.OnExit(this);
      }
      catch (Exception ex)
      {
        // Swallow or log as appropriate. We don't want a failed OnExit to leave timer running.
        Console.Error.WriteLine($"Exception in OnExit of state {previous?.Name}: {ex}");
      }

      StopTimerLocked();
      _currentState = newState;

      try
      {
        _currentState.OnEnter(this);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Exception in OnEnter of state {_currentState?.Name}: {ex}");
      }

      StartTimerIfNeededLocked();
    }
  }

  /// <summary>
  /// Convenience alias.
  /// </summary>
  public void TransitionTo(IState newState) => EnterState(newState);

  private void StartTimerIfNeededLocked()
  {
    if (_currentState is IStateWithTimeout withTimeout)
    {
      var timeout = withTimeout.Timeout;
      if (timeout > TimeSpan.Zero)
      {
        // single-shot timer
        _timer = new Timer(OnTimerFiredCallback, withTimeout, timeout, Timeout.InfiniteTimeSpan);
      }
    }
  }

  private void StopTimerLocked()
  {
    if (_timer != null)
    {
      try
      {
        _timer.Dispose();
      }
      catch
      {
      }

      _timer = null;
    }
  }

  private void OnTimerFiredCallback(object? stateObj)
  {
    // stateObj is the IStateWithTimeout that was active when timer was created.
    if (!(stateObj is IStateWithTimeout timedState))
      return;

    // Ensure only one thread can check/clear the timer/current state at a time.
    // We will check that the timedState is still the current state; if so, clear the timer
    // and call OnTimeout outside the lock so that OnTimeout can call TransitionTo without deadlock.
    bool shouldCallTimeout = false;
    lock (_lock)
    {
      if (_disposed)
        return;

      if (ReferenceEquals(_currentState, timedState))
      {
        // Dispose timer (clear it) under lock so no race with EnterState.
        StopTimerLocked();
        shouldCallTimeout = true;
      }
    }

    if (shouldCallTimeout)
    {
      try
      {
        timedState.OnTimeout(this);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine($"Exception in OnTimeout of state {timedState?.Name}: {ex}");
      }
    }
  }

  public void Dispose()
  {
    lock (_lock)
    {
      if (_disposed)
        return;

      _disposed = true;
      StopTimerLocked();

      try
      {
        _currentState?.OnExit(this);
      }
      catch
      {
      }

      _currentState = null;
    }
  }
}

/*
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
*/
