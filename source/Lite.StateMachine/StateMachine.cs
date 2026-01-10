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

  /// <summary>Optional local event aggregator.</summary>
  private readonly IEventAggregator? _eventAggregator;

  ////private readonly ILogger<StateMachine<TStateId>>? _logger;

  /// <summary>Active states.</summary>
  private readonly Dictionary<TStateId, IState<TStateId>> _instances = [];

  /// <summary>States registered with system.</summary>
  private readonly Dictionary<TStateId, StateRegistration<TStateId>> _states = [];

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
  /// <param name="isContextPersistent">Is substate-added context persists when returning to the parent.</param>
  public StateMachine(
    Func<Type, object?>? containerFactory = null,
    IEventAggregator? eventAggregator = null,
    bool isContextPersistent = true)
  {
    // TODO (2025-12-31 DS): Throw "Missing DI Container" exception because there are parameters in a state class's constructor.
    //// Current Exception:
    ////  System.MissingMethodException: 'Cannot dynamically create an instance of type 'Lite.StateMachine.Tests.TestData.CompositeL3DiStates.State1'. Reason: No parameterless constructor defined.'
    _containerFactory = containerFactory ?? (t => Activator.CreateInstance(t));
    _eventAggregator = eventAggregator;
    IsContextPersistent = isContextPersistent;

    // NOTE-1 (2025-12-25):
    //  * Create Precheck Sanitization:
    //    * Verify initial states are set for core and all sub-states.
    //    * Verify any transition exceptions (i.e. DisjointedNextSubStateException)
    //
    //// OLD-4d3, 4bx:
    ////  public StateMachine(IServiceResolver? services = null, IEventAggregator? eventAggregator = null, ILogger<StateMachine<TStateId>>? logs = null)
    ////  _services = services;
    ////  _logger = logs;
  }

  /// <inheritdoc/>
  public Context<TStateId> Context { get; private set; } = new Context<TStateId>(default, default, default!, null);
  ////public Context<TStateId> Context { get; private set; } = default!;

  /// <inheritdoc/>
  public int DefaultCommandTimeoutMs { get; set; } = 3000;

  /// <inheritdoc/>
  public int DefaultStateTimeoutMs { get; set; } = Timeout.Infinite;

  /// <summary>Gets or sets a value indicating whether substate-added context persists when returning to the parent (default: true).</summary>
  public bool IsContextPersistent { get; set; } = true;

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
    // NOTE (2025-12-28 DS): This is already caught by the base method, just provides a different message
    if (_states.ContainsKey(stateId))
      throw new DuplicateStateException($"Composite parent '{stateId}' already registered.");

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
  public StateMachine<TStateId> RegisterState<TStateClass>(TStateId stateId, TStateId? onSuccess = null, TStateId? onError = null, TStateId? onFailure = null)
    where TStateClass : class, IState<TStateId>
  {
    return RegisterState<TStateClass>(stateId, onSuccess, onError: null, onFailure: null, parentStateId: null, isCompositeParent: false, initialChildStateId: null);
  }

  /// <inheritdoc/>
  public StateMachine<TStateId> RegisterState<TStateClass>(
    TStateId stateId,
    TStateId? onSuccess,
    TStateId? onError,
    TStateId? onFailure,
    TStateId? parentStateId = null,
    bool isCompositeParent = false,
    TStateId? initialChildStateId = null)
    where TStateClass : class, IState<TStateId>
  {
    if (_states.ContainsKey(stateId))
      throw new DuplicateStateException($"State '{stateId}' already registered.");

    // TODO (2025-12-28 DS): Shouldn't happen. Use custom exception, StateClassNotRegisteredInContainerException
    var reg = new StateRegistration<TStateId>
    {
      StateId = stateId,
      Factory = () => (IState<TStateId>)(_containerFactory(typeof(TStateClass))
        ?? throw new InvalidOperationException($"Factory returned null for {typeof(TStateClass).Name}")),
      ParentId = parentStateId,
      IsCompositeParent = isCompositeParent,
      InitialChildId = initialChildStateId,
      OnSuccess = onSuccess,
      OnError = onError,
      OnFailure = onFailure,
      //// vNext: SubscribedMessages = cmdMsgs ?? [],
    };

    _states[stateId] = reg;

    return this;
  }

  /// <inheritdoc/>
  public StateMachine<TStateId> RegisterSubComposite<TCompositeParent>(
    TStateId stateId,
    TStateId parentStateId,
    TStateId initialChildStateId,
    TStateId? onSuccess = null,
    TStateId? onError = null,
    TStateId? onFailure = null)
    where TCompositeParent : class, IState<TStateId>
  {
    if (!_states.TryGetValue(parentStateId, out var pr) || !pr.IsCompositeParent)
      throw new ParentStateMustBeCompositeException($"Parent state '{parentStateId}' must be registered as a composite state.");

    // NOTE (2025-12-28 DS): This is already caught by the base method
    if (_states.ContainsKey(stateId))
      throw new DuplicateStateException($"Composite child state '{stateId}' already registered.");

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
  public StateMachine<TStateId> RegisterSubState<TChildClass>(
    TStateId stateId,
    TStateId parentStateId,
    TStateId? onSuccess = null,
    TStateId? onError = null,
    TStateId? onFailure = null)
    where TChildClass : class, IState<TStateId>
  {
    if (!_states.TryGetValue(parentStateId, out var pr) || !pr.IsCompositeParent)
      throw new ParentStateMustBeCompositeException($"Parent state '{parentStateId}' must be registered as a composite state.");

    // NOTE (2025-12-28 DS): This is already caught by the base method
    if (_states.ContainsKey(stateId))
      throw new DuplicateStateException($"Child state '{stateId}' already registered.");

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
    TStateId initialStateId,
    PropertyBag? parameterStack = null,
    PropertyBag? errorStack = null,
    CancellationToken cancellationToken = default)
  {
    if (!_states.ContainsKey(initialStateId))
      throw new MissingInitialStateException($"Initial state '{initialStateId}' was not registered.");

    TStateId? prevStateId = null;
    var currentStateId = initialStateId;

    // TBD
    ////Context = new Context<TStateId>(currentStateId, default, default!, null);

    while (!cancellationToken.IsCancellationRequested)
    {
      var reg = GetRegistration(currentStateId);
      reg.PreviousStateId = prevStateId;

      // TODO (2025-12-28 DS): Configure context and pass it along
      ////var tcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
      ////var ctx = new Context<TStateId>(reg.StateId, tcs, _eventAggregator)
      ////{
      ////  Parameters = parameterStack ?? [],
      ////  Errors = errorStack ?? [],
      ////};

      ////Context.Parameters = parameterStack ?? [];
      ////Context.Errors = errorStack ?? [];

      parameterStack ??= [];
      errorStack ??= [];

      // Run any state (composite or leaf) recursively.
      var result = await RunAnyStateRecursiveAsync(reg, parameterStack, errorStack, cancellationToken).ConfigureAwait(false);
      ////var result = await RunAnyStateRecursiveAsync(reg, cancellationToken).ConfigureAwait(false);
      if (result is null)
        break;

      var nextId = StateMachine<TStateId>.ResolveNext(reg, result.Value);
      if (nextId is null)
        break;

      prevStateId = currentStateId;
      currentStateId = nextId.Value;
    }

    return this;
  }

  /// <summary>Get next state transition based on state's result.</summary>
  /// <param name="reg">State registration.</param>
  /// <param name="result">State's returned result.</param>
  /// <returns><see cref="TStateId"/> to go to next or NULL to bubble-up or end state machine process.</returns>
  private static TStateId? ResolveNext(StateRegistration<TStateId> reg, Result result) => result switch
  {
    Result.Success => reg.OnSuccess,
    Result.Error => reg.OnError,
    Result.Failure => reg.OnFailure,
    _ => null,
  };

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
  /// <exception cref="UnregisteredStateTransitionException">Thrown if the specified state identifier has not been registered.</exception>
  private StateRegistration<TStateId> GetRegistration(TStateId stateId)
  {
    // In the future, states can override/customize the "NextState" on the fly, we need a unique exception.
    if (!_states.TryGetValue(stateId, out var reg))
      throw new UnregisteredStateTransitionException($"Next State Id '{stateId}' was not registered.");

    return reg;
  }

  private async Task<Result?> RunAnyStateRecursiveAsync(
    StateRegistration<TStateId> reg,
    PropertyBag? parameters,
    PropertyBag? errors,
    CancellationToken ct)
  {
    // Ensure we always operate on non-null, shared bags
    ////parameters ??= [];
    ////errors ??= [];

    // Run Normal or Command State
    if (!reg.IsCompositeParent)
      ////return await RunLeafAsync(reg, ct).ConfigureAwait(false);
      return await RunLeafAsync(reg, parameters, errors, ct).ConfigureAwait(false);

    // Composite States
    var instance = GetOrCreateInstance(reg);

    StateMap<TStateId> nextStates = new()
    {
      OnSuccess = reg.OnSuccess,
      OnError = reg.OnError,
      OnFailure = reg.OnFailure,
    };

    var parentEnterTcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    var parentEnterCtx = new Context<TStateId>(reg.StateId, nextStates, parentEnterTcs, _eventAggregator)
    {
      Parameters = parameters,
      Errors = errors,
      PreviousStateId = reg.PreviousStateId,
    };

    await instance.OnEntering(parentEnterCtx).ConfigureAwait(false);

    // [IsContextPersistent]
    //  Take snapshot of original Context keys AFTER OnEntering so we can give the state a chance
    //  to purposely add new keys to and carry forward for subsequent top-level states.
    //
    //  Any new Context keys added via OnEnter are considered "for children consumption only".
    //  After our OnExit, they'll be (optionally) removed.
    var originalParamKeys = new HashSet<object>(parameters.Keys);
    var originalErrorKeys = new HashSet<object>(errors.Keys);

    await instance.OnEnter(parentEnterCtx).ConfigureAwait(false);

    // Check for next transition overrides
    ////reg.OnSuccess = parentEnterCtx.NextStates.OnSuccess;
    ////reg.OnError = parentEnterCtx.NextStates.OnError;
    ////reg.OnFailure = parentEnterCtx.NextStates.OnFailure;

    // TODO (2025-12-28 DS): Consider StateMachine config param to just move along or throw exception
    if (reg.InitialChildId is null)
      throw new MissingInitialSubStateException($"Composite '{reg.StateId}' must have an initial child (InitialChildId).");

    var childId = reg.InitialChildId.Value;
    Result? lastChildResult = null;

    // Set the initial substate's PreviousStateId to NULL, as we already know the parent.
    TStateId? childPrevStateId = null;

    // Composite Loop
    while (!ct.IsCancellationRequested)
    {
      var childReg = GetRegistration(childId);
      childReg.PreviousStateId = childPrevStateId;

      // The child state was not registered the specified composite parent state
      if (!Equals(childReg.ParentId, reg.StateId))
        throw new OrphanSubStateException($"Child state '{childId}' must belong to composite '{reg.StateId}'.");

      // Could just call "RunAnyStateRecursiveAsync" but lets not waste cycles
      Result? childResult;
      if (childReg.IsCompositeParent)
        childResult = await RunAnyStateRecursiveAsync(childReg, parameters, errors, ct).ConfigureAwait(false);
      else
        childResult = await RunLeafAsync(childReg, parameters, errors, ct).ConfigureAwait(false);

      // Cancelled or timed out inside child state
      if (childResult is null)
        return null;

      // TODO (#76): Extract the Context.OnSuccess/Error/Failure override (if any)
      lastChildResult = childResult;
      var nextChildId = StateMachine<TStateId>.ResolveNext(childReg, childResult.Value);

      // NULL mapping => last child => bubble-up to parent and exit
      if (nextChildId is null)
        break;

      // Ensure next state is a sibling under the same composite parent (i.e. unlinked sub-states)
      var mappedReg = GetRegistration(nextChildId.Value);
      if (!Equals(mappedReg.ParentId, reg.StateId))
        throw new DisjointedNextSubStateException($"Child '{childId}' maps to '{nextChildId}', which is not a sibling under '{reg.StateId}'.");

      // Proceed to the next substate
      childPrevStateId = childId;
      childId = nextChildId.Value;
    }

    // Parent's OnExit decides Ok/Error/Failure; Inform parent of last child's result via Context
    // TODO (2025-12-28 DS): Pass one Context object. Just clear "lastChildResult" after the OnExit.
    var parentExitTcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    var parentExitCtx = new Context<TStateId>(reg.StateId, nextStates, parentExitTcs, _eventAggregator, lastChildResult)
    {
      Parameters = parameters ?? [],
      Errors = errors ?? [],
    };

    await instance.OnExit(parentExitCtx).ConfigureAwait(false);

    // To avoid composite state's OnExit, use the DefaultStateTimeoutMs to auto-cancel wait.
    var parentDecision = await WaitForNextOrCancelAsync(parentExitTcs.Task, ct).ConfigureAwait(false);

    // Optionally cleanup context added by the children; giving the parent a peek at their mess.
    if (!IsContextPersistent)
    {
      if (parameters is not null)
      {
        foreach (var k in parameters.Keys)
          if (!originalParamKeys.Contains(k)) parameters.Remove(k);
      }

      if (errors is not null)
      {
        foreach (var k in errors.Keys)
          if (!originalErrorKeys.Contains(k)) errors.Remove(k);
      }
    }

    // vNext:
    ////if (parentDecision is null)
    ////  // Log null decision, possible DefaultStateTimeoutMs encountered.

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

    // Next state transitions
    StateMap<TStateId> nextStates = new()
    {
      OnSuccess = reg.OnSuccess,
      OnError = reg.OnError,
      OnFailure = reg.OnFailure,
    };

    var tcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    var ctx = new Context<TStateId>(reg.StateId, nextStates, tcs, _eventAggregator)
    {
      Parameters = parameterStack ?? [],
      Errors = errorStack ?? [],
      PreviousStateId = reg.PreviousStateId,
    };

    IDisposable? subscription = null;
    CancellationTokenSource? timeoutCts = null;

    if (instance is ICommandState<TStateId> cmd)
    {
      if (_eventAggregator is not null)
      {
        // Subscribed message types or `Array.Empty<Type>()` for none
        //// vNext: IReadOnlyCollection<Type> types2 = [.. cmd.SubscribedMessageTypes ?? [], .. reg.SubscribedMessageTypes ?? []];
        var types = cmd.SubscribedMessageTypes ?? [];

        subscription = _eventAggregator.Subscribe(async (msgObj) =>
        {
          if (cancellationToken.IsCancellationRequested || tcs.Task.IsCompleted)
            return;

#pragma warning disable SA1501 // Statement should not be on a single line
          // Swallow to avoid breaking publication loop
          try { await cmd.OnMessage(ctx, msgObj).ConfigureAwait(false); } catch { }
#pragma warning restore SA1501 // Statement should not be on a single line
        },
        [.. types]);   //// [.. types] == types.ToArray()

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

      // TODO (2025-12-28 DS): Potential DefaultStateTimeoutMs. Even leaving OnEnter without NextState(Result.OK), should consider calling `OnExit` to allow states to cleanup.
      // TODO (2026-01-04 DS): Consider handling "OnTRANSITION" overrides. Passing a single Context would solve this as it's passed by ref.
      if (result is null)
        return null;

      await instance.OnExit(ctx).ConfigureAwait(false);

      reg.OnSuccess = ctx.NextStates.OnSuccess;
      reg.OnError = ctx.NextStates.OnError;
      reg.OnFailure = ctx.NextStates.OnFailure;

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
