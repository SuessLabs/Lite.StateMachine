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

  ////private readonly Dictionary<TState, IState<TState>> _states = [];
  private readonly Dictionary<TState, Registration> _states = [];

  private IState<TState>? _currentState;
  private TState _initialState;
  private bool _isStarted;
  private IDisposable? _subscription;
  private CancellationTokenSource? _timeoutCts;

  public StateMachine(IEventAggregator? eventAggregator = null, ILogger<StateMachine<TState>>? logs = null)
  {
    _eventAggregator = eventAggregator;
    _logger = logs;
  }

  private StateMachine(StateMachine<TState> parentMachine, ICompositeState<TState> ownerCompositeState, IEventAggregator? eventAggregator = null, ILogger<StateMachine<TState>>? logs = null)
  {
    _parentMachine = parentMachine;
    _ownerCompositeState = ownerCompositeState;
    _eventAggregator = eventAggregator;
    _logger = logs;
  }

  /// <summary>Gets the context payload passed between the states, and contains methods for transitioning to the next state.</summary>
  public Context<TState> Context { get; private set; } = default!;

  /// <summary>Gets or sets the default timeout (3000ms default) to be used by <see cref="CommandState{TState}"/>'s OnTimeout.</summary>
  public int DefaultTimeoutMs { get; set; } = 3000;

  /// <summary>
  ///   Gets or sets whether or not the machine will evict (discard) the state instance after OnExit to conserve memory.
  ///   False by default, this is useful if state instances are heavy and can be safely recreated on next entry.
  /// </summary>
  public bool EvictStateInstancesOnExit { get; set; } = false;

  /// <summary>Gets the collection of all registered states.</summary>
  /// <remarks>
  ///   Exposed for validations, debugging, etc.
  ///   Previously: <![CDATA[Dictionary<TState, IState<TState>>]]>.
  /// </remarks>
  public List<TState> States => [.. _states.Keys];

  /// <summary>
  ///   Configure a composite (hierarchical) state:
  ///   provide a callback that registers sub-states and sets the submachine's initial state.
  ///   Called lazily when the composite instance is created.
  /// </summary>
  /// <remarks>
  ///   TODO: Incorporate this with <see cref="RegisterState"/> and <see cref="RegisterStateEx"/>.
  ///
  ///   Usage:
  ///     <![CDATA[
  ///     // NOTE
  ///     machine.RegisterState(WorkflowState.Processing, () => new ProcessingState());
  ///     machine.RegisterComposite(WorkflowState.Processing, sub =>
  ///     {
  ///       sub.RegisterState(WorkflowState.Load,     () => new LoadState());
  ///       sub.RegisterState(WorkflowState.Validate, () => new ValidateState());
  ///       sub.SetInitial(WorkflowState.Load);
  ///     });
  ///     ]]>
  ///
  ///   Proposal vNext:
  ///     <![CDATA[
  ///     machine.RegisterComposite<ProcessingState>(WorkflowState.Processing, sub =>
  ///     {
  ///       sub.RegisterState<LoadState>(WorkflowState.Load);
  ///       sub.RegisterState<ValidateState>(WorkflowState.Validate);
  ///       sub.SetInitial(WorkflowState.Load);
  ///     });
  ///     ]]>
  /// </remarks>
  public void RegisterComposite(TState compositeId, Action<StateMachine<TState>> configure)
  {
    if (!_states.TryGetValue(compositeId, out var reg))
      throw new InvalidOperationException($"Composite state '{compositeId}' must be registered before configuring.");

    reg.ConfigureSubmachine = configure ?? throw new ArgumentNullException(nameof(configure));
  }

  /// <summary>
  ///   Register a state by enum id and a factory that creates the state instance.
  ///   The instance will be created lazily on first entry.
  /// </summary>
  /// <remarks>
  ///   Usage:
  ///     <![CDATA[
  ///       machine.RegisterState(WorkflowState.Start, () => new StartState());
  ///     ]]>
  ///
  ///   Proposal vNext:
  ///     <![CDATA[
  ///       machine.RegisterState<StartState>(WorkflowState.Start);
  ///     ]]>
  /// </remarks>
  public void RegisterState(TState id, Func<IState<TState>> factory)
  {
    ArgumentNullException.ThrowIfNull(factory);
    _states[id] = new Registration { Factory = factory };
  }

  /*
  /// <summary>Register state with state machine.</summary>
  /// <param name="state">Instance of <see cref="IState{TState}"/>.</param>
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
  */

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
    if (_isStarted)
      throw new InvalidOperationException("State machine already started.");

    if (!_states.ContainsKey(_initialState))
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

    // Get the state from the Lazy collection
    var initialState = GetInstance(_initialState);
    EnterState(initialState);
  }

  /// <summary>Internal transition logic used by Context.NextState.</summary>
  internal void InternalNextState(Result outcome)
  {
    if (_currentState is null)
      throw new InvalidOperationException("No current state.");

    var current = _currentState;

    // 1) Try local mapping (sub-state machine level).
    if (current.Transitions.TryGetValue(outcome, out var nextStateId))
    {
      ExitCurrent();
      var next = GetInstance(nextStateId);
      EnterState(next);
      return;
    }

    // 2) Bubble up from submachine to parent composite when no local mapping.
    //    If this is within a submachine and cannot resolve mapping,
    //    bubble up: invoke parent's OnExit, then parent machine transitions.
    if (_parentMachine is not null && _ownerCompositeState is not null)
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
    // TODO (2025-12-17 DS): vNext - Option for last-state to choose whether to exit, sit there, or throw exception.
    ExitCurrent();
  }

  /// <summary>Command state cancel timer and message subscription.</summary>
  /// <remarks>TODO (2025-12-17): Rename to, CommandStateCancelTimerAndSubscription() or CommandStateCancel().</remarks>
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

    // If Composite State: Start submachine (requires submachine to be configured and initial set)
    if (state is ICompositeState<TState> comp)
    {
      // TODO: Ensure submachine has initial and is registered.
      // Submachine will drive transitions until the last state, then bubbles up
      comp.Submachine.DefaultTimeoutMs = DefaultTimeoutMs;
      comp.Submachine.EvictStateInstancesOnExit = EvictStateInstancesOnExit;
      comp.Submachine.Start(Context.Parameters);
      return;
    }

    // If Command State: Subscribe to aggregator and start timeout
    if (state is ICommandState<TState> cmd)
      SetupCommandState(cmd);
  }

  /// <summary>Cancel timer and scription and inform exiting of current state.</summary>
  private void ExitCurrent()
  {
    // Even if current state is null, cancel the timer and subscription
    CancelTimerAndSubscription();

    if (_currentState is null)
      return;

    _currentState.OnExit(Context);

    // Optional: Evict instance to conserve memory
    // Let GC collect; will recreate on next entry
    if (EvictStateInstancesOnExit && _states.TryGetValue(_currentState.Id, out var reg))
      reg.LazyInstance = null;
  }

  /// <summary>Get and generate state instance of the Lazy state.</summary>
  /// <returns>Instance of the state.</returns>
  private IState<TState> GetInstance(TState id)
  {
    if (!_states.TryGetValue(id, out var regState))
      throw new InvalidOperationException($"State '{id}' is not registered.");

    // if (reg.LazyInstance is null) { ... }
    regState.LazyInstance ??= new Lazy<IState<TState>>(() =>
    {
      var instance = regState.Factory();

      // If composite: wire a submachine and run configuration callback.
      if (instance is ICompositeState<TState> comp)
      {
        var sub = new StateMachine<TState>(this, comp, _eventAggregator)
        {
          DefaultTimeoutMs = DefaultTimeoutMs,
          EvictStateInstancesOnExit = EvictStateInstancesOnExit,
        };

        comp.Submachine = sub;
        regState.ConfigureSubmachine?.Invoke(sub);
      }

      return instance;
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    return regState.LazyInstance.Value;
  }

  /// <summary>Command State initialization and execution.</summary>
  /// <remarks>TODO (2025-12-17): Rename to, `CommandStateInit()`.</remarks>
  /// <param name="cmd"></param>
  private void SetupCommandState(ICommandState<TState> cmd)
  {
    CancelTimerAndSubscription();

    // Subscribe to messages
    if (_eventAggregator is not null)
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

        // If we arrived here, no message received within timeout
        cmd.OnTimeout(Context);
      }
      catch (TaskCanceledException)
      {
        // Timer auto-cancelled by message arrival or state exited
      }
    });
  }

  private sealed class Registration
  {
    /// <summary>Used for composite states.</summary>
    public Action<StateMachine<TState>>? ConfigureSubmachine;

    public Func<IState<TState>> Factory = default;

    public Lazy<IState<TState>>? LazyInstance;
  }
}
