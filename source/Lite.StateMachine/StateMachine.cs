// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <inheritdoc/>
public sealed partial class StateMachine<TStateId> : IStateMachine<TStateId>
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

  ////private IState<TStateId>? _currentState;
  ////private TStateId _initialState;
  ////private bool _isStarted;
  ////private IDisposable? _subscription;
  ////private CancellationTokenSource? _timeoutCts;

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

  /// <inheritdoc/>
  public Context<TStateId> Context { get; private set; } = default!;

  /// <inheritdoc/>
  public int DefaultCommandTimeoutMs { get; set; } = 3000;

  /// <inheritdoc/>
  public int DefaultStateTimeoutMs { get; set; } = Timeout.Infinite;

  /// <inheritdoc/>
  public List<TStateId> States => [.. _states.Keys];

  /// <inheritdoc/>
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

  /// <inheritdoc/>
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

  /// <inheritdoc/>
  public StateMachine<TStateId> RegisterState<TState>(TStateId stateId, TStateId? onSuccess = null)
    where TState : class, IState<TStateId>
  {
    return RegisterState<TState>(stateId, onSuccess, onError: null, onFailure: null, parentStateId: null, isCompositeParent: false, initialChildStateId: null);
  }

  /// <inheritdoc/>
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

  /// <inheritdoc/>
  public StateMachine<TStateId> RegisterSubState<TChildClass>(
    TStateId stateId,
    TStateId parentStateId,
    TStateId? onSuccess = null,
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

  /// <inheritdoc/>
  public async Task<StateMachine<TStateId>> RunAsync(
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
      ////var tcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
      ////var ctx = new Context<TStateId>(reg.StateId, tcs, _eventAggregator)
      ////{
      ////  Parameters = parameterStack ?? [],
      ////  ErrorStack = errorStack ?? [],
      ////};

      // Run any state (composite or leaf) recursively.
      var result = await RunAnyStateRecursiveAsync(reg, parameterStack, errorStack, cancellationToken).ConfigureAwait(false);
      if (result is null)
        break;

      var nextId = ResolveNext(reg, result.Value);
      if (nextId is null)
        break;

      current = nextId.Value;
    }

    return this;
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
    // TODO (2025-12-18 DS): Use custom exception, UnregisteredStateTransitionException, UnregisteredNextStateException or MissingOrInvalidRegistrationException
    // Because states can override/customize the "NextState" on the fly, we need a unique exception.
    if (!_states.TryGetValue(stateId, out var reg))
      throw new InvalidOperationException($"Next State Id '{stateId}' was not registered.");

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

      // Cancelled or timed out inside child state
      if (childResult is null)
        return null;

      lastChildResult = childResult;
      var nextChildId = ResolveNext(childReg, childResult.Value);

      // NULL mapping => last child => bubble-up to parent and exit
      if (nextChildId is null)
        break;

      // Ensure next state is apart of the same composite parent (DisjointedNextSubStateException)
      // TODO (2025-12-28 DS): Use custom exception, DisjointedNextStateException
      var mappedReg = GetRegistration(nextChildId.Value);
      if (!Equals(mappedReg.ParentId, reg.StateId))
        throw new InvalidOperationException($"Child '{childId}' maps to '{nextChildId}', which is not a sibling under '{reg.StateId}'.");

      // Proceed to the next substate
      childId = nextChildId.Value;
    }

    // Parent's OnExit decides Ok/Error/Failure; Inform parent of last child's result via Context
    // TODO (2025-12-28 DS): Pass one Context object. Just clear "lastChildResult" after the OnExit.
    var parentExitTcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    var parentExitCtx = new Context<TStateId>(reg.StateId, parentExitTcs, _eventAggregator, lastChildResult)
    {
      Parameters = parameterStack ?? [],
      ErrorStack = errorStack ?? [],
    };

    await instance.OnExit(parentExitCtx).ConfigureAwait(false);

    // To avoid composite state's OnExit, use the DefaultStateTimeoutMs to auto-cancel wait.
    var parentDecision = await WaitForNextOrCancelAsync(parentExitTcs.Task, ct).ConfigureAwait(false);

    // vNext:
    ////if (parentDecision is null)
    ////{
    ////  Log null decision, possible DefaultStateTimeoutMs encountered.
    ////}

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

        var timeoutMs = cmd.TimeoutMs ?? DefaultCommandTimeoutMs;
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

      // TODO (2025-12-28 DS): Even leaving OnEnter without NextState(Result.OK), we should always call `OnExit` to allow states to cleanup.
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
    var cancelTask = Task.Delay(DefaultStateTimeoutMs, cts.Token);
    var completed = await Task.WhenAny(t, cancelTask).ConfigureAwait(false);
    cts.Cancel();

    if (completed == t)
      return t.Result;

    return null;
  }
}
