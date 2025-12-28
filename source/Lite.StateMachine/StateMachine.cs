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
///   6. Add "IsSanitizedStates()" to check that defined state-chaining is valid.
///      i.e. (i) Sub-States must have the same parent. (ii) 'NextState' is registered.
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
      var ctx = new Context<TStateId>(reg.StateId, tcs, _eventAggregator)
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
    // TODO (2025-12-18 DS): Use custom exception, UnregisteredNextStateException or MissingOrInvalidRegistrationException
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
    CancellationToken ct)
  {
    // Run Normal or Command State
    if (!reg.IsCompositeParent)
      return await RunLeafAsync(reg, parameterStack, errorStack, ct).ConfigureAwait(false);

    // Composite States
    var instance = GetOrCreateInstance(reg);
    var parentEnterTcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    var parentEnterCtx = new Context<TStateId>(reg.StateId, parentEnterTcs, _eventAggregator)
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

    var childId = reg.InitialChildId.Value;
    Result? lastChildResult = null;

    while (!ct.IsCancellationRequested)
    {
      var childReg = GetRegistration(childId);

      // TODO (2025-12-28 DS): Use custom exception, OrphanSubStateException
      if (!Equals(childReg.ParentId, reg.StateId))
        throw new InvalidOperationException($"Child '{childId}' must belong to composite '{reg.StateId}'.");

      // Could just call "RunAnyStateRecursiveAsync" but lets not waste cycles
      Result? childResult;
      if (childReg.IsCompositeParent)
        childResult = await RunAnyStateRecursiveAsync(childReg, parameterStack, errorStack, ct).ConfigureAwait(false);
      else
        childResult = await RunLeafAsync(childReg, parameterStack, errorStack, ct).ConfigureAwait(false);

      // Cancelled
      if (childResult is null)
        return null;

      lastChildResult = childResult;
      var nextChildId = ResolveNext(childReg, childResult.Value);

      // NULL mapping => last child => bubble-up to parent and exit
      if (nextChildId is null)
        break;

      // Ensure next state is apart of the same composite parent
      // TODO (2025-12-28 DS): Use custom exception, DisjointedNextStateException
      var mappedReg = GetRegistration(nextChildId.Value);
      if (!Equals(mappedReg.ParentId, reg.StateId))
        throw new InvalidOperationException($"Child '{childId}' maps to '{nextChildId}', which is not a sibling under '{reg.StateId}'.");
    }

    // Parent exit decides Ok/Error/Failure; Inform parent of last child's result via Context
    // TODO (2025-12-28 DS): Pass one Context object. Just clear "lastChildResult" after the OnExit.
    var parentExitTcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    var parentExitCtx = new Context<TStateId>(reg.StateId, parentEnterTcs, _eventAggregator, lastChildResult)
    {
      Parameters = parameterStack ?? [],
      ErrorStack = errorStack ?? [],
    };

    await instance.OnExit(parentExitCtx).ConfigureAwait(false);

    var parentDecision = await WaitForNextOrCancelAsync(parentExitTcs.Task, ct).ConfigureAwait(false);

    // if (parentDecision is null) { log-something }
    return parentDecision;
  }

  // Rename: RunSingleStateAsync(...)
  private async Task<Result?> RunLeafAsync(
    StateRegistration<TStateId> reg,
    PropertyBag? parameterStack,
    PropertyBag? errorStack,
    CancellationToken cancellationToken)
  {
    IState<TStateId> instance = GetOrCreateInstance(reg);
    var tcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    var ctx = new Context<TStateId>(reg.StateId, tcs, _eventAggregator)
    {
      Parameters = parameterStack ?? [],
      ErrorStack = errorStack ?? [],
    };

    IDisposable? subscription = null;
    CancellationTokenSource? timeoutCts = null;

    if (instance is ICommandState<TStateId> cmd)
    {
      if (_eventAggregator is not null)
      {
        subscription = _eventAggregator.Subscribe(async (msgObj) =>
        {
          if (cancellationToken.IsCancellationRequested || tcs.Task.IsCompleted)
            return;

#pragma warning disable SA1501 // Statement should not be on a single line
          try { await cmd.OnMessage(ctx, msgObj).ConfigureAwait(false); } catch { }
#pragma warning restore SA1501 // Statement should not be on a single line
        });

        var timeoutMs = cmd.TimeoutMs ?? DefaultTimeoutMs;
        if (timeoutMs > 0)
        {
          timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
          _ = Task.Run(async () =>
          {
            try
            {
              await Task.Delay(timeoutMs, timeoutCts.Token).ConfigureAwait(false);
              if (!tcs.Task.IsCompleted && !timeoutCts.IsCancellationRequested)
                await cmd.OnTimeout(ctx).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
              // Expected exception; swallow it.
            }
          },
          timeoutCts.Token);
        }
      }
    }

    try
    {
      await instance.OnEntering(ctx).ConfigureAwait(false);
      await instance.OnEnter(ctx).ConfigureAwait(false);

      var result = await WaitForNextOrCancelAsync(tcs.Task, cancellationToken).ConfigureAwait(false);

      // TODO (2025-12-28 DS): We should always call `OnExit` to allow states to cleanup.
      if (result is null)
        return null;

      await instance.OnExit(ctx).ConfigureAwait(false);
      return result.Value;
    }
    finally
    {
      timeoutCts?.Cancel();
      timeoutCts?.Dispose();
      subscription?.Dispose();
    }
  }

  private async Task<Result?> WaitForNextOrCancelAsync(Task<Result> t, CancellationToken ct)
  {
    var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    var cancelTask = Task.Delay(Timeout.Infinite, cts.Token);
    var completed = await Task.WhenAny(t, cancelTask).ConfigureAwait(false);
    cts.Cancel();

    if (completed == t)
      return t.Result;

    return null;
  }
}
