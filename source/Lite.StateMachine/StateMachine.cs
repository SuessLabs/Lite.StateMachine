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
///   TODO:
///   1. Use context to override default ctx.NextState(StateId).
///   2. Implement custom Exceptions.
///   3. Ensure context is passed along and bubbled-up.
///   4. Configuration: Destroy state instance(s) after leaving (customizable to not have GC pressure).
///   5. Optionally pass in ILogger for state machine logging.
/// </remarks>
public sealed partial class StateMachine<TStateId>
  where TStateId : struct, Enum
{
  /////// <summary>Adapter so the state machine can use any DI container</summary>
  /////// <remarks>OLD-4d3: private readonly IServiceResolver? _services;.</remarks>

  /// <summary>Optional dependency injection container factory.</summary>
  private readonly Func<Type, object?> _containerFactory;

  private readonly IEventAggregator? _eventAggregator;

  ////private readonly ILogger<StateMachine<TStateId>>? _logger;

  /// <summary>Active states.</summary>
  private readonly Dictionary<TStateId, IState<TStateId>> _instances = [];

  /// <summary>States registered with system.</summary>
  private readonly Dictionary<TStateId, StateRegistration<TStateId>> _states = [];

  private IState<TStateId>? _currentState;
  private TStateId _initialState;
  private bool _isStarted;
  private IDisposable? _subscription;
  private CancellationTokenSource? _timeoutCts;

  /// <summary>
  ///   Initializes a new instance of the <see cref="StateMachine{TStateId}"/> class.
  ///   Dependency Injection is optional:
  ///   - If containerFactory is omitted, states are created with Activator.CreateInstance.
  ///   - If provided, it's used to construct state instances via your container.
  /// </summary>
  /// <param name="containerFactory">Optional DI container factory (remember to register states as Transient).</param>
  /// <param name="eventAggregator">Optional event aggregator for command states.</param>
  public StateMachine(
    Func<Type, object?>? containerFactory = null,
    IEventAggregator? eventAggregator = null)
  {
    _containerFactory = containerFactory ?? (t => Activator.CreateInstance(t));
    _eventAggregator = eventAggregator;

    // NOTE-1 (2025-12-25):
    //  * Create Precheck Sanitization:
    //    * Verify initial states are set for core and all sub-states.
    //    * Throw clear exceptions to inform user what to fix.
    // NOTE-2 (2025-12-25):
    //  When not using DI (null containerFactory), generate instance with parameterless instance
    //  This means all states CANNOT have parameters in their constructors.
    //
    //// OLD-4d3, 4bx:
    ////  public StateMachine(IServiceResolver? services = null, IEventAggregator? eventAggregator = null, ILogger<StateMachine<TStateId>>? logs = null)
    ////  _services = services;
    ////  _logger = logs;
  }

  /// <summary>Gets the context payload passed between the states, and contains methods for transitioning to the next state.</summary>
  public Context<TStateId> Context { get; private set; } = default!;

  /// <summary>Gets or sets the default timeout in milliseconds (3000ms default). Used by <see cref="ICommandState{TState}"/>, triggering OnTimeout.</summary>
  public int DefaultTimeoutMs { get; set; } = 3000;

  /// <summary>Gets the collection of all registered states.</summary>
  /// <remarks>
  ///   Exposed for validations, debugging, etc.
  ///   Previously: <![CDATA[Dictionary<TState, IState<TState>>]]>.
  /// </remarks>
  public List<TStateId> States => [.. _states.Keys];

  /// <summary>
  /// Registers a top-level composite parent state (has no parent state) and explicitly sets:
  /// - the initial child (initialChildStateId).
  /// - the next top-level transitions (nextOnOk, nextOnError, nextOnFailure).
  /// </summary>
  public StateMachine<TStateId> RegisterComposite<TCompositeParent>(
    TStateId stateId,
    TStateId initialChildStateId,
    TStateId? onSuccess = null,
    TStateId? onError = null,
    TStateId? onFailure = null)
    where TCompositeParent : class, IState<TStateId>
  {
    // TODO (2025-12-28 DS): Use exception, DuplicateStateException
    if (_states.ContainsKey(stateId))
      throw new InvalidOperationException($"Composite parent '{stateId}' already registered.");

    return RegisterState<TCompositeParent>(
      stateId: stateId,
      onSuccess: onSuccess,
      onError: onError,
      onFailure: onFailure,
      parentStateId: null,
      isCompositeParent: true,
      initialChildStateId: initialChildStateId);
  }

  /// <summary>Nested composite (child composite under a parent composite).</summary>
  public StateMachine<TStateId> RegisterCompositeChild<TCompositeParent>(
    TStateId stateId,
    TStateId parentStateId,
    TStateId initialChildStateId,
    TStateId? onSuccess = null,
    TStateId? onError = null,
    TStateId? onFailure = null)
    where TCompositeParent : class, IState<TStateId>
  {
    // TODO (2025-12-28 DS): Use exception, ParentStateMustBeCompositeException
    if (!_states.TryGetValue(parentStateId, out var pr) || !pr.IsCompositeParent)
      throw new InvalidOperationException($"Parent state '{parentStateId}' must be a composite.");

    // TODO (2025-12-28 DS): Use exception, DuplicateStateException
    if (_states.ContainsKey(stateId))
      throw new InvalidOperationException($"Composite child state '{stateId}' already registered.");

    return RegisterState<TCompositeParent>(
      stateId: stateId,
      onSuccess: onSuccess,
      onError: onError,
      onFailure: onFailure,
      parentStateId: parentStateId,
      isCompositeParent: true,
      initialChildStateId: initialChildStateId);
  }

  /// <summary>Registers a regular or command state (optionally with transitions).</summary>
  /// <remarks>Example: <![CDATA[RegisterState<T>(StateId.State1, StateId.State2);]]>.</remarks>
  /// <param name="stateId">State Id.</param>
  /// <param name="onSuccess">State Id to transition to on success.</param>
  /// <returns>Instance of this class.</returns>
  /// <typeparam name="TState">State class.</typeparam>
  public StateMachine<TStateId> RegisterState<TState>(TStateId stateId, TStateId? onSuccess)
    where TState : class, IState<TStateId>
  {
    return RegisterState<TState>(stateId, onSuccess, onError: null, onFailure: null, parentStateId: null, isCompositeParent: false, initialChildStateId: null);
  }

  /// <summary>
  ///   Registers a new state with the state machine and configures its transitions and hierarchy.
  /// </summary>
  /// <remarks>
  ///   Use this method to add states and define their transitions and hierarchy before starting the
  ///   state machine. Registering duplicate state identifiers is not allowed.
  /// </remarks>
  /// <typeparam name="TState">The type of the state to register. Must implement <see cref="IState{TStateId}"/>.</typeparam>
  /// <param name="stateId">The unique identifier for the state to register.</param>
  /// <param name="onSuccess">The identifier of the state to transition to when the registered state completes successfully, or null if no transition is defined.</param>
  /// <param name="onError">The identifier of the state to transition to when the registered state encounters an error, or null if no transition is defined.</param>
  /// <param name="onFailure">The identifier of the state to transition to when the registered state fails, or null if no transition is defined.</param>
  /// <param name="parentStateId">The identifier of the parent state if the registered state is part of a composite state; otherwise, null.</param>
  /// <param name="isCompositeParent">true if the registered state is a composite parent state; otherwise, false.</param>
  /// <param name="initialChildStateId">The identifier of the initial child state to activate when entering a composite parent state; otherwise, null.</param>
  /// <returns>The current StateMachine<TStateId> instance, enabling method chaining.</returns>
  /// <exception cref="InvalidOperationException">Thrown if a state with the specified stateId is already registered or if the state factory returns null.</exception>
  public StateMachine<TStateId> RegisterState<TState>(
    TStateId stateId,
    TStateId? onSuccess,
    TStateId? onError,
    TStateId? onFailure,
    TStateId? parentStateId = null,
    bool isCompositeParent = false,
    TStateId? initialChildStateId = null)
    where TState : class, IState<TStateId>
  {
    // TODO (2025-12-28 DS): Use custom exception, DuplicateStateException
    if (_states.ContainsKey(stateId))
      throw new InvalidOperationException($"State '{stateId}' already registered.");

    var reg = new StateRegistration<TStateId>
    {
      StateId = stateId,
      Factory = () => (IState<TStateId>)(_containerFactory(typeof(TState))
        ?? throw new InvalidOperationException($"Factory returned null for {typeof(TState).Name}")),
      ParentId = parentStateId,
      IsCompositeParent = isCompositeParent,
      InitialChildId = initialChildStateId,
      OnSuccess = onSuccess,
      OnError = onError,
      OnFailure = onFailure,
    };

    _states[stateId] = reg;

    return this;
  }

  /// <summary>
  /// Registers a composite's sub-state (regular/leaf or command state) under a composite parent.
  /// The nextOnOk is nullable: null means this is the last child, so bubble to the parent's OnExit.
  /// </summary>
  public StateMachine<TStateId> RegisterSubState<TChildClass>(
    TStateId stateId,
    TStateId parentStateId,
    TStateId? onSuccess,
    TStateId? onError = null,
    TStateId? onFailure = null)
    where TChildClass : class, IState<TStateId>
  {
    // TODO (2025-12-28 DS): Use exception, ParentStateMustBeCompositeException
    if (!_states.TryGetValue(parentStateId, out var pr) || !pr.IsCompositeParent)
      throw new InvalidOperationException($"Parent '{parentStateId}' must be registered as a composite parent.");

    // TODO (2025-12-28 DS): Use exception, DuplicateStateException
    if (_states.ContainsKey(stateId))
      throw new InvalidOperationException($"Child state '{stateId}' already registered.");

    return RegisterState<TChildClass>(
      stateId,
      onSuccess,
      onError,
      onFailure,
      parentStateId,
      isCompositeParent: false,
      initialChildStateId: null);
  }

  /// <summary>Starts the machine at the initial state.</summary>
  /// <param name="initialState">Initial startup state.</param>
  /// <param name="parameterStack">Initial <see cref="PropertyBag"/> parameter stack.</param>
  /// <param name="errorStack">Error Stack <see cref="PropertyBag"/>.</param>
  /// <param name="cancellationToken">Cancellation Token.</param>
  /// <returns>Async task.</returns>
  /// <exception cref="InvalidOperationException">Thrown if the specified state identifier has not been registered.</exception>
  public async Task RunAsync(
    TStateId initialState,
    PropertyBag? parameterStack = null,
    PropertyBag? errorStack = null,
    CancellationToken cancellationToken = default)
  {
    // TODO (2025-12-28 DS): Use custom exception, InvalidMissingStartupStateException
    if (!_states.ContainsKey(initialState))
      throw new InvalidOperationException($"Initial state '{initialState}' was not registered.");

    var current = initialState;

    while (!cancellationToken.IsCancellationRequested)
    {
      var reg = GetRegistration(current);

      // TODO (2025-12-28 DS): Configure context and pass it along
      var tcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
      var ctx = new Context<TStateId>(reg.StateId, tcs)
      {
        Parameters = parameterStack ?? [],
        ErrorStack = errorStack ?? [],
      };

      // Run any state (composite or leaf) recursively.
      var result = await RunAnyStateRecursiveAsync(reg, parameterStack, errorStack, cancellationToken).ConfigureAwait(false);
      if (result is null)
        break;

      var nextId = ResolveNext(reg, result.Value);
      if (nextId is null)
        break;

      current = nextId.Value;
    }
  }

  /// <summary>
  ///   Retrieves an existing state instance associated with the specified registration,
  ///   or creates and stores a new instance if none exists.
  /// </summary>
  /// <param name="reg">
  ///   The state registration containing the state identifier and factory method used to
  ///   create the instance if it does not already exist.</param>
  /// <returns>The state instance corresponding to the specified registration.</returns>
  private IState<TStateId> GetOrCreateInstance(StateRegistration<TStateId> reg)
  {
    // NOTE (2025-12-28): In the future, should optionally destroy states after `OnExit` via config param
    if (_instances.TryGetValue(reg.StateId, out var instance))
      return instance;

    var stateInstance = reg.Factory();
    _instances[reg.StateId] = stateInstance;
    return stateInstance;
  }

  /// <summary>Retrieves the registration information for the specified state identifier.</summary>
  /// <param name="stateId">The identifier of the state whose registration information is to be retrieved.</param>
  /// <returns>The registration associated with the specified state identifier.</returns>
  /// <exception cref="InvalidOperationException">Thrown if the specified state identifier has not been registered.</exception>
  private StateRegistration<TStateId> GetRegistration(TStateId stateId)
  {
    // TODO (2025-12-27 DS): Use custom exception, UnregisteredNextStateException
    // Because states can override/customize the "NextState" on the fly, we need a unique exception.
    if (!_states.TryGetValue(stateId, out var reg))
      throw new InvalidOperationException($"Next state '{stateId}' was not registered.");

    return reg;
  }

  /// <summary>Get next state transition based on state's result.</summary>
  /// <param name="reg">State registration.</param>
  /// <param name="result">State's returned result.</param>
  /// <returns><see cref="TStateId"/> to go to next or NULL to bubble-up or end state machine process.</returns>
  private TStateId? ResolveNext(StateRegistration<TStateId> reg, Result result)
  {
    return result switch
    {
      Result.Ok => reg.OnSuccess,
      Result.Error => reg.OnError,
      Result.Failure => reg.OnFailure,
      _ => null,
    };
  }

  private async Task<Result?> RunAnyStateRecursiveAsync(
    StateRegistration<TStateId> reg,
    PropertyBag? parameterStack,
    PropertyBag? errorStack,
    CancellationToken cancellationToken)
  {
    // Run Normal or Command State
    if (!reg.IsCompositeParent)
      return await RunLeafAsync(reg, parameterStack, errorStack, cancellationToken).ConfigureAwait(false);

    // Composite States
    var instance = GetOrCreateInstance(reg);
    var parentEnterTcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    var parentEnterCtx = new Context<TStateId>(reg.StateId, parentEnterTcs)
    {
      Parameters = parameterStack ?? [],
      ErrorStack = errorStack ?? [],
    };

    await instance.OnEntering(parentEnterCtx).ConfigureAwait(false);
    await instance.OnEnter(parentEnterCtx).ConfigureAwait(false);

    // TODO (2025-12-28 DS): Consider StateMachine config param to just move along or throw exception
    // TODO (2025-12-28 DS): Use custom exception, InvalidMissingInitialSubStateException
    if (reg.InitialChildId is null)
      throw new InvalidOperationException($"Composite '{reg.StateId}' must have an initial child (InitialChildId).");
  }

  // Rename: RunSingleStateAsync(...)
  private async Task<Result?> RunLeafAsync(
    StateRegistration<TStateId> reg,
    PropertyBag? parameterStack,
    PropertyBag? errorStack,
    CancellationToken cancellationToken)
  {
    var instance = GetOrCreateInstance(reg);
    var tcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    var ctx = new Context<TStateId>(reg.StateId, tcs)
    {
      Parameters = parameterStack ?? [],
      ErrorStack = errorStack ?? [],
    };

    IDisposable? sub = null;
    CancellationTokenSource? timeoutCts = null;

    if (instance is ICommandState<TStateId> cmd)
    {

    }
  }

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
    try { _subscription?.Dispose(); } catch { /* ignore * / }

    _subscription = null;

    try { _timeoutCts?.Cancel(); } catch { /* ignore * / }

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
    if (EvictStateInstancesOnExit && _states.TryGetValue(_currentState.StateId, out var reg))
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
  */
}
