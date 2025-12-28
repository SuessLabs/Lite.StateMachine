// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// The generic, enum-driven state machine with hierarchical bubbling and command-state timeout handling.
/// </summary>
/// <typeparam name="TStateId">Type of State Id to use (i.e. enum, int, etc.).</typeparam>
/// <remarks>
///   TODO: Do not require creating a new StateMachine for substates.
///     Composite:  <![CDATA[.RegisterState<ParentState>(Enum.StateId1, isComposite: true, onSuccess, onError, onFailure);]]>.
///     Substate:   <![CDATA[.RegisterState<SubState>(Enum.StateId2, parentState: Enum.ParentState, onSuccess, onError, onFailure);]]>.
/// </remarks>
public sealed partial class StateMachine<TStateId>
  where TStateId : struct, Enum
{
  //// OLD-4d3: private readonly IServiceResolver? _services;
  private readonly Func<Type, object?> _containerFactory;

  private readonly IEventAggregator? _eventAggregator;

  ////private readonly ILogger<StateMachine<TStateId>>? _logger;
  private readonly ICompositeState<TStateId>? _ownerCompositeState;

  private readonly StateMachine<TStateId>? _parentMachine;

  /// <summary>r4c States with DI.</summary>
  /// <remarks>Previously: <![CDATA[Dictionary<TState, StateRegistration> _states = [];]]>.</remarks>
  private readonly Dictionary<TStateId, StateRegistration<TStateId>> _states = [];

  private IState<TStateId>? _currentState;
  private TStateId _initialState;
  private bool _isStarted;
  private IDisposable? _subscription;
  private CancellationTokenSource? _timeoutCts;

  //// OLD-4d3: public StateMachine(IServiceResolver? services = null, IEventAggregator? eventAggregator = null, ILogger<StateMachine<TStateId>>? logs = null)
  public StateMachine(
    Func<Type, object?>? containerFactory = null,
    IEventAggregator? eventAggregator = null)
    ////ILogger<StateMachine<TStateId>>? logs = null)
  {
    //// OLD-4d3: _services = services;

    // NOTE-1 (2025-12-25):
    //  * Create Precheck Sanitization:
    //    * Verify initial statates are set for core and all sub-states.
    //    * Throw clear exceptions to inform user what to fix.
    // NOTE-2 (2025-12-25):
    //  When not using DI (null containerFactory), generate instance with parameterless instance
    //  This means all states CANNOT have parameters in their constructors.
    _containerFactory = containerFactory ?? (t => Activator.CreateInstance(t));
    _eventAggregator = eventAggregator;
    ////_logger = logs;
  }

  //// OLD-4d3: private StateMachine(StateMachine<TStateId> parentMachine, ICompositeState<TStateId> ownerCompositeState, IServiceResolver? services, IEventAggregator? eventAggregator = null, ILogger<StateMachine<TStateId>>? logs = null)
  private StateMachine(
    StateMachine<TStateId> parentMachine,
    ICompositeState<TStateId> ownerCompositeState,
    Func<Type, object?>? containerFactory = null,
    IEventAggregator? eventAggregator = null)
  ////ILogger<StateMachine<TStateId>>? logs = null)
  {
    // For submachines within composite states
    _parentMachine = parentMachine;
    _ownerCompositeState = ownerCompositeState;

    //// OLD-4d3: _services = services;
    _containerFactory = containerFactory ?? (t => Activator.CreateInstance(t));
    _eventAggregator = eventAggregator;
    ////_logger = logs;
  }

  /// <summary>Gets the context payload passed between the states, and contains methods for transitioning to the next state.</summary>
  public Context<TStateId> Context { get; private set; } = default!;

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
  public List<TStateId> States => [.. _states.Keys];

  /*
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
  public StateMachine<TStateId> RegisterState(
    TStateId stateId,
    Func<IState<TStateId>> state,
    TStateId? onSuccess = null,
    TStateId? onError = null,
    TStateId? onFailure = null,
    Action<StateMachine<TStateId>>? subStates = null)
  {
    ArgumentNullException.ThrowIfNull(state);

    _states[stateId] = new StateRegistration<TStateId>
    {
      Factory = state,
      OnSuccess = onSuccess,
      OnError = onError,
      OnFailure = onFailure,
      FactoryStateId = stateId,
    };

    // Check for registration errors
    // TODO (2025-12-18): Change exception to, "MissingStateOrInvalidRegistration"
    if (!_states.TryGetValue(stateId, out var reg))
      throw new InvalidOperationException($"Composite state '{stateId}' must be registered before configuring.");

    if (subStates is not null)
      reg.ConfigureSubmachine = subStates;

    return this;
  }
  */

  /// <summary>Registers a state with the state machine using generics and configures its transitions and optional substates.</summary>
  /// <typeparam name="TStateClass">
  ///   The type of the state to register. Must implement the <see cref="IState{TState}"/> interface and have a parameterless constructor.
  /// </typeparam>
  /// <param name="stateId">The unique identifier for the state to register.</param>
  /// <param name="onSuccess">The state to transition to when the registered state completes successfully, or null if no transition is defined.</param>
  /// <param name="onError">The state to transition to when the registered state encounters an error, or null if no transition is defined.</param>
  /// <param name="onFailure">The state to transition to when the registered state fails, or null if no transition is defined.</param>
  /// <param name="subStates">
  ///   An optional delegate to configure substates for the registered state. If provided, this allows the state to act as
  ///   a composite state with its own submachine.
  /// </param>
  /// <returns>The current <see cref="StateMachine{TState}"/> instance, enabling method chaining.</returns>
  /// <exception cref="InvalidOperationException">Thrown if the state registration fails due to an invalid or missing state configuration.</exception>
  /// <remarks>
  ///   Use this method to add a new state to the state machine and define its transitions. If substates
  ///   are configured, the registered state will act as a composite state, allowing for hierarchical state machines. This
  ///   method supports fluent configuration by returning the state machine instance.
  /// </remarks>
  public StateMachine<TStateId> RegisterState<TStateClass>(
    TStateId stateId,
    TStateId? onSuccess = null,
    TStateId? onError = null,
    TStateId? onFailure = null,
    Action<StateMachine<TStateId>>? subStates = null)
    //// OLD-4bx: where TStateClass : class, IState<TStateId>, new()
    where TStateClass : class, IState<TStateId> // (vNext)
  {
    // OLD-4bx: Create factory method
    ////Func<IState<TStateId>> state = () => new TStateClass();
    ////ArgumentNullException.ThrowIfNull(state);

    Func<Type, object?>? stateFactory = null;
    stateFactory = t => Activator.CreateInstance(t);

    _states[stateId] = new StateRegistration<TStateId>
    {
      // Factory: Func<IState<TState>>?
      //// OLD-4bx: Factory = state,
      //// OLD-4d3: Factory = lzy => lzy!.CreateInstance<TStateClass>(),

      // Lazy-load our class with/without the container
      Factory = () => (IState<TStateId>)(_containerFactory(typeof(TStateClass))
        ?? throw new InvalidOperationException($"Factory returned null for {typeof(TStateClass).Name}")),

      StateId = stateId,
      OnSuccess = onSuccess,
      OnError = onError,
      OnFailure = onFailure,

      // vNext:
      // ParentId = parentId,
      // IsComposite = isComposite,
    };

    // Check for registration errors
    // TODO (2025-12-18): Change exception to, "MissingStateOrInvalidRegistration"
    if (!_states.TryGetValue(stateId, out var reg))
      throw new InvalidOperationException($"Composite state '{stateId}' must be registered before configuring.");

    if (subStates is not null)
      reg.ConfigureSubmachine = subStates;

    return this;
  }

  /// <summary>Set the initial startup state (supporting fluent pattern).</summary>
  /// <param name="initial">Initial state from enumeration.</param>
  /// <returns>This class for fluent design pattern.</returns>
  public StateMachine<TStateId> SetInitial(TStateId initial)
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

    var parentEnterTcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    Context = new Context<TStateId>(_initialState, parentEnterTcs)
    {
      Parameters = initParameters,
      ErrorStack = errorStack,
    };

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

  private void EnterState(IState<TStateId> state)
  {
    _currentState = state;
    state.OnEntering(Context);
    state.OnEnter(Context);

    // If Composite State: Start submachine (requires submachine to be configured and initial set)
    if (state is ICompositeState<TStateId> comp)
    {
      // TODO: Ensure submachine has initial and is registered.
      // Submachine will drive transitions until the last state, then bubbles up
      comp.Submachine.DefaultTimeoutMs = DefaultTimeoutMs;
      comp.Submachine.EvictStateInstancesOnExit = EvictStateInstancesOnExit;
      comp.Submachine.Start(Context.Parameters);
      return;
    }

    // If Command State: Subscribe to aggregator and start timeout
    if (state is ICommandState<TStateId> cmd)
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
  private IState<TStateId> GetInstance(TStateId id)
  {
    if (!_states.TryGetValue(id, out var regState))
      throw new InvalidOperationException($"State '{id}' is not registered.");

    regState.LazyInstance ??= new Lazy<IState<TStateId>>(() =>
    {
      if (regState.Factory is null)
        throw new NullReferenceException("Provided state factory as null");

      var instance = regState.Factory();

      if (regState.OnSuccess is not null)
        (instance as BaseState<TStateId>)?.AddTransition(Result.Ok, regState.OnSuccess.Value);

      if (regState.OnError is not null)
        (instance as BaseState<TStateId>)?.AddTransition(Result.Error, regState.OnError.Value);

      if (regState.OnFailure is not null)
        (instance as BaseState<TStateId>)?.AddTransition(Result.Failure, regState.OnFailure.Value);

      // If composite: wire a submachine and run configuration callback.
      if (instance is ICompositeState<TStateId> compositeState)
      {
        var sub = new StateMachine<TStateId>(
          this,
          compositeState,
          containerFactory: _containerFactory,
          _eventAggregator)
        {
          DefaultTimeoutMs = DefaultTimeoutMs,
          EvictStateInstancesOnExit = EvictStateInstancesOnExit,
        };

        compositeState.Submachine = sub;
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
  private void SetupCommandState(ICommandState<TStateId> cmd)
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
}
