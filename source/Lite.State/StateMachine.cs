// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.State;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// The generic, enum-driven state machine with hierarchical bubbling and command-state timeout handling.
/// </summary>
public sealed partial class StateMachine<TState> where TState : struct, Enum
{
  private readonly IEventAggregator? _eventAggregator;
  private readonly ILogger<StateMachine<TState>>? _logger;
  private readonly ICompositeState<TState>? _ownerCompositeState;
  private readonly StateMachine<TState>? _parentMachine;
  private readonly Dictionary<TState, IState<TState>> _states = [];

  private IState<TState>? _currentState;
  private TState _initialState;
  private bool _isStarted;
  private IDisposable? _subscription;
  private CancellationTokenSource? _timeoutCts;

  public StateMachine(IEventAggregator? eventAggregator = null)
  {
    _eventAggregator = eventAggregator;
  }

  private StateMachine(StateMachine<TState> parentMachine, ICompositeState<TState> ownerCompositeState, IEventAggregator? eventAggregator = null, ILogger<StateMachine<TState>>? logs = null)
  {
    _parentMachine = parentMachine;
    _ownerCompositeState = ownerCompositeState;
    _eventAggregator = eventAggregator;
    _logger = logs;
  }

  public Context<TState> Context { get; private set; } = default!;

  public int DefaultTimeoutMs { get; set; } = 3000;

  public void RegisterState(IState<TState> state)
  {
    ArgumentNullException.ThrowIfNull(state);

    _states[state.Id] = state;

    // Wire composite sub-state machine instance if needed.
    ////if (state is ICompositeState<TState> comp)
    if (state is CompositeState<TState> comp)
    {
      comp.Submachine = new StateMachine<TState>(this, comp, _eventAggregator)
      {
        DefaultTimeoutMs = DefaultTimeoutMs
      };
    }
  }

  /// <summary>Register State (extended fluent pattern).</summary>
  /// <param name="state">ID of state.</param>
  /// <param name="onSuccess">OnSuccess State Id. When not defined, the machine exits.</param>
  /// <param name="onError">(Optional) OnError State Id.</param>
  /// <param name="onFailure">(Optional) OnFailure State Id.</param>
  /// <returns>StateMachine instance for fluent definitions.</returns>
  /// <exception cref="ArgumentNullException">Must include State ID.</exception>
  public StateMachine<TState> RegisterStateEx(
    IState<TState> state,
    TState? onSuccess = null,
    TState? onError = null,
    TState? onFailure = null)
  {
    ArgumentNullException.ThrowIfNull(state);

    if (onSuccess is not null)
      (state as BaseState<TState>)?.AddTransition(Result.Ok, onSuccess.Value);

    if (onError is not null)
      (state as BaseState<TState>)?.AddTransition(Result.Error, onError.Value);

    if (onFailure is not null)
      (state as BaseState<TState>)?.AddTransition(Result.Failure, onFailure.Value);

    _states[state.Id] = state;

    // Wire composite sub-state machine instance if needed.
    ////if (state is ICompositeState<TState> comp)
    if (state is CompositeState<TState> comp)
    {
      comp.Submachine = new StateMachine<TState>(this, comp, _eventAggregator)
      {
        DefaultTimeoutMs = DefaultTimeoutMs
      };
    }

    return this;
  }

  /// <summary>Set the initial startup state.</summary>
  /// <param name="initial">Initial state from enumeration.</param>
  public void SetInitial(TState initial) => _initialState = initial;

  /// <summary>Set the initial startup state (extended fluent pattern).</summary>
  /// <param name="initial">Initial state from enumeration.</param>
  public StateMachine<TState> SetInitialEx(TState initial)
  {
    _initialState = initial;
    return this;
  }

  /// <summary>Starts the machine at the initial state.</summary>
  /// <param name="initParameters">Initial <see cref="PropertyBag"/> parameter stack.</param>
  /// <param name="errorStack">Error Stack <see cref="PropertyBag"/>.</param>
  public void Start(PropertyBag? initParameters = null, PropertyBag? errorStack = null)
  {
    if (_isStarted) throw new InvalidOperationException("State machine already started.");
    if (!_states.TryGetValue(_initialState, out var initialState))
      throw new InvalidOperationException($"Initial state '{_initialState}' is not registered.");

    _isStarted = true;

    // Same as below
    ////var ctx = new Context<TState>(this) { Parameters = initParameters ?? [] };
    ////typeof(StateMachine<TState>)
    ////    .GetProperty(nameof(Context))!
    ////    .SetValue(this, ctx);

    // Initialize the property bags
    initParameters ??= [];
    errorStack ??= [];

    Context = new Context<TState>(this) { Parameters = initParameters, ErrorStack = errorStack, };

    EnterState(initialState);
  }

  /// <summary>Internal transition logic used by Context.NextState.</summary>
  internal void InternalNextState(Result outcome)
  {
    if (_currentState == null)
      throw new InvalidOperationException("No current state.");

    var current = _currentState;

    // 1) Try local mapping (sub-state machine level).
    if (current.Transitions.TryGetValue(outcome, out var target))
    {
      ExitCurrent();

      var next = _states[target];

      EnterState(next);
      return;
    }

    // 2) If this is within a submachine and cannot resolve mapping,
    // bubble up: invoke parent's OnExit, then parent machine transitions.
    if (_parentMachine != null && _ownerCompositeState != null)
    {
      // Leave last sub-state
      ExitCurrent();

      // Parent composite state's exit hook
      _ownerCompositeState.OnExit(Context);

      // Continue from parent machine using the same outcome
      _parentMachine.InternalNextState(outcome);
      return;
    }

    // 3) Top-level cannot resolve: this is terminal; just exit.
    ExitCurrent();
  }

  private void CancelTimerAndSubscription()
  {
    try { _subscription?.Dispose(); } catch { /* ignore */ }

    _subscription = null;

    try { _timeoutCts?.Cancel(); } catch { /* ignore */ }

    _timeoutCts?.Dispose();
    _timeoutCts = null;
  }

  private void EnterState(IState<TState> state)
  {
    _currentState = state;
    state.OnEntering(Context);
    state.OnEnter(Context);

    // If composite: start its submachine at its own initial (must be set).
    if (state is ICompositeState<TState> comp)
    {
      // inherit default timeout
      comp.Submachine.DefaultTimeoutMs = DefaultTimeoutMs;

      // Compose: ensure submachine has initial and is registered.
      // Submachine will drive transitions until it bubbles up
      comp.Submachine.Start(Context.Parameters);
      return;
    }

    // If command state: subscribe to aggregator and start timeout
    if (state is ICommandState<TState> cmd)
      SetupCommandState(cmd);
  }

  /// <summary>Cancel timer and scription and inform exiting of current state.</summary>
  private void ExitCurrent()
  {
    CancelTimerAndSubscription();
    _currentState?.OnExit(Context);
  }

  private void SetupCommandState(ICommandState<TState> cmd)
  {
    CancelTimerAndSubscription();

    // Subscribe to messages
    if (_eventAggregator != null)
    {
      _subscription = _eventAggregator.Subscribe(msg =>
      {
        // Filter before delivery
        if (!cmd.MessageFilter(msg))
          return false;

        // Cancel timeout upon first relevant message delivery
        _timeoutCts?.Cancel();

        cmd.OnMessage(Context, msg);
        return true;
      });
    }

    // Start timeout
    var timeoutMs = cmd.TimeoutMs ?? DefaultTimeoutMs;
    _timeoutCts = new CancellationTokenSource();

    _ = Task.Run(async () =>
    {
      try
      {
        await Task.Delay(timeoutMs, _timeoutCts.Token);

        // If we arrived here, no message within timeout
        cmd.OnTimeout(Context);
      }
      catch (TaskCanceledException)
      {
        // Timer cancelled by message or exit
      }
    });
  }
}
