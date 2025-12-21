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
/// <typeparam name="TState">Type of State Id to use (i.e. enum, int, etc.).</typeparam>
public sealed partial class StateMachine<TState>
  where TState : struct, Enum
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
  ///   Gets or sets a value indicating whether or not the machine will evict (discard) the state instance after OnExit to conserve memory.
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
  ///   Register a state by enum id and a factory that creates the state instance.
  ///   The state's class instance will be created lazily on first entry.
  ///
  ///   When supplying <paramref name="subStates"/>, the provided callback registers
  ///   sub-states. You MUST set the submachine's initial state.
  /// </summary>
  /// <param name="stateId">State Id (number/enum).</param>
  /// <param name="state">State class.</param>
  /// <param name="onSuccess">State Id to transition to on success.</param>
  /// <param name="onError">State Id to transition to on error.</param>
  /// <param name="onFailure">State Id to transition to on failure.</param>
  /// <param name="subStates">Sub-state state machine.</param>
  /// <returns>This class for fluent design pattern.</returns>
  /// <remarks>
  ///   Usage:
  ///   <![CDATA[
  ///     // Standard state
  ///     machine.RegisterState(WorkflowState.Start, () => new StartState());
  ///
  ///     // Composite State
  ///     machine.RegisterState(WorkflowState.Processing, () => new ProcessingState()), sub =>
  ///     {
  ///       sub.RegisterState(WorkflowState.Load,     () => new LoadState());
  ///       sub.RegisterState(WorkflowState.Validate, () => new ValidateState());
  ///       sub.SetInitial(WorkflowState.Load);
  ///     });
  ///     ]]>
  ///
  ///   Proposal vNext:
  ///     <![CDATA[
  ///     machine.RegisterState<StartState>(WorkflowState.Start);
  ///
  ///     machine.RegisterState<ProcessingState>(WorkflowState.Processing, sub =>
  ///     {
  ///       sub.RegisterState<LoadState>(WorkflowState.Load);
  ///       sub.RegisterState<ValidateState>(WorkflowState.Validate);
  ///       sub.SetInitial(WorkflowState.Load);
  ///     });
  ///     ]]>.
  /// </remarks>
  public StateMachine<TState> RegisterState(
    TState stateId,
    Func<IState<TState>> state,
    TState? onSuccess = null,
    TState? onError = null,
    TState? onFailure = null,
    Action<StateMachine<TState>>? subStates = null)
  {
    ArgumentNullException.ThrowIfNull(state);

    _states[stateId] = new Registration
    {
      Factory = state,
      OnSuccess = onSuccess,
      OnError = onError,
      OnFailure = onFailure,
    };

    // Check for registration errors
    // TODO (2025-12-18): Change exception to, "MissingStateOrInvalidRegistration"
    if (!_states.TryGetValue(stateId, out var reg))
      throw new InvalidOperationException($"Composite state '{stateId}' must be registered before configuring.");

    if (subStates is not null)
      reg.ConfigureSubmachine = subStates;

    return this;
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
  /// <returns>This class for fluent design pattern.</returns>
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
  /// <param name="resultId">Result of state execution.</param>
  internal void InternalNextState(Result resultId)
  {
    if (_currentState is null)
      throw new InvalidOperationException("No current state.");

    var current = _currentState;

    // 1) Try local mapping (sub-state machine level).
    if (current.Transitions.TryGetValue(resultId, out var nextStateId))
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
      _parentMachine.InternalNextState(resultId);
      return;
    }

    // 3) Top-level cannot resolve: this is terminal; just exit.
    // TODO (2025-12-17 DS): vNext - Option for last-state to choose whether to exit, sit there, or throw exception.
    ExitCurrent();
  }

  /// <summary>Command state cancel timer and message subscription.</summary>
  /// <remarks>TODO (2025-12-17): Rename to, CommandStateCancelTimerAndSubscription() or CommandStateCancel().</remarks>
  [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1501:Statement should not be on a single line", Justification = "We don't need 16 lines for a try/catch.")]
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
  /// <remarks>TODO: Rename to, `InitializeLazyState()`.</remarks>
  /// <returns>Instance of the state.</returns>
  private IState<TState> GetInstance(TState id)
  {
    if (!_states.TryGetValue(id, out var regState))
      throw new InvalidOperationException($"State '{id}' is not registered.");

    // if (reg.LazyInstance is null) { ... }
    regState.LazyInstance ??= new Lazy<IState<TState>>(() =>
    {
      if (regState.Factory is null)
        throw new NullReferenceException("Provided state factory as null");

      var instance = regState.Factory();

      if (regState.OnSuccess is not null)
        (instance as BaseState<TState>)?.AddTransition(Result.Ok, regState.OnSuccess.Value);

      if (regState.OnError is not null)
        (instance as BaseState<TState>)?.AddTransition(Result.Error, regState.OnError.Value);

      if (regState.OnFailure is not null)
        (instance as BaseState<TState>)?.AddTransition(Result.Failure, regState.OnFailure.Value);

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
    },
    LazyThreadSafetyMode.ExecutionAndPublication);

    return regState.LazyInstance.Value;
  }

  /// <summary>Command State initialization and execution.</summary>
  /// <remarks>TODO (2025-12-17): Rename to, `CommandStateInit()`.</remarks>
  /// <param name="cmd">Command state.</param>
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

  [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Intentional public fields.")]
  private sealed class Registration
  {
    /// <summary>Used for composite states.</summary>
    public Action<StateMachine<TState>>? ConfigureSubmachine;

    /// <summary>State factory to execute.</summary>
    public Func<IState<TState>>? Factory = default;

    public Lazy<IState<TState>>? LazyInstance;

    /// <summary>Optional auto-wire OnError StateId transition.</summary>
    public TState? OnError = null;

    /// <summary>Optional auto-wire OnFailure StateId transition.</summary>
    public TState? OnFailure = null;

    /// <summary>Optional auto-wire OnSuccess StateId transition.</summary>
    public TState? OnSuccess = null;
  }
}
