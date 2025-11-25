// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace LiteState.Mk1;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>State transition result.</summary>
public enum Result
{
  Success,
  Error,
  Failure
}

public enum StateId
{
  None,
  Init,
  Loading,
  Processing,
  Completed,
  Error,

  // Example composite "parent"
  OrderFlow
}

#region State Machine

/// <summary>Lite Finite State Machine (FSM) implementation.</summary>
public sealed class StateMachine
{
  private readonly bool _isRoot;
  private readonly SemaphoreSlim _lock = new(1, 1);
  private readonly ILogger<StateMachine> _logger;
  private readonly IServiceProvider _services;
  private readonly Dictionary<StateId, Lazy<StateNode>> _states = new();
  private readonly Dictionary<StateId, Dictionary<Result, StateId>> _transitions = new();
  private TaskCompletionSource<Result>? _completionTcs;
  private StateId _current = StateId.None;

  public StateMachine(IServiceProvider services,
                            ILogger<StateMachine> logger,
                            bool isRoot = true)
  {
    _services = services ?? throw new ArgumentNullException(nameof(services));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _isRoot = isRoot;
  }

  public async Task ForwardMessageAsync(IDictionary<string, object>? message = null)
  {
    await _lock.WaitAsync().ConfigureAwait(false);
    try
    {
      if (_current == StateId.None)
        return;

      var node = _states[_current].Value;

      var ctx = BuildContext(message);
      _logger.LogDebug("Forwarding message to current state: {State}.", _current);
      await node.OnMessageAsync(ctx).ConfigureAwait(false);
    }
    finally
    {
      _lock.Release();
    }
  }

  public async Task ForwardTimeoutAsync()
  {
    await _lock.WaitAsync().ConfigureAwait(false);
    try
    {
      if (_current == StateId.None)
        return;

      var node = _states[_current].Value;

      var ctx = BuildContext(null);
      _logger.LogWarning("Forwarding timeout to current state: {State}.", _current);
      await node.OnTimeoutAsync(ctx).ConfigureAwait(false);
    }
    finally
    {
      _lock.Release();
    }
  }

  /// <summary>Register a state lazily via DI (generic type).</summary>
  public void RegisterState<TState>(StateId state)
    where TState : StateNode
  {
    _states[state] = new Lazy<StateNode>(
      () => ActivatorUtilities.CreateInstance<TState>(_services),
      LazyThreadSafetyMode.ExecutionAndPublication);
  }

  /// <summary>Register a state lazily via a custom factory that uses DI.</summary>
  public void RegisterState(StateId state, Func<IServiceProvider, StateNode> factory)
  {
    if (factory is null)
      throw new ArgumentNullException(nameof(factory));

    _states[state] = new Lazy<StateNode>(
      () => factory(_services),
      LazyThreadSafetyMode.ExecutionAndPublication);
  }

  /// <summary>Configure transitions for a state: Result → Next State.</summary>
  /// <remarks>
  /// <![CDATA[
  ///   subFsm.SetTransitions(State.Loading, new Dictionary<Result, State>
  ///   {
  ///     [Result.Success] = State.Processing,
  ///     [Result.Error] = State.Completed,
  ///     [Result.Failure] = State.Completed
  ///   })
  ///   // Not implemented yet
  ///   // .AllowTransition(State.Processing); // Allow an additional transition separately
  /// ]]>
  /// </remarks>
  public void SetTransitions(StateId state, IDictionary<Result, StateId> map)
  {
    var dict = new Dictionary<Result, StateId>();
    foreach (var kvp in map)
      dict[kvp.Key] = kvp.Value;

    _transitions[state] = dict;
  }

  public async Task<Result> StartAndWaitAsync(StateId initial, IDictionary<string, object>? initialParams = null)
  {
    _completionTcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    await StartAsync(initial, initialParams).ConfigureAwait(false);
    return await _completionTcs.Task.ConfigureAwait(false);
  }

  public async Task StartAsync(StateId initial, IDictionary<string, object>? initialParams = null)
  {
    await _lock.WaitAsync().ConfigureAwait(false);

    try
    {
      if (_current != StateId.None)
        throw new InvalidStateTransitionException($"FSM already started at state '{_current}'.");

      _logger.LogInformation("FSM starting at state '{Initial}'.", initial);
      await TransitionToAsync(initial, BuildContext(initialParams)).ConfigureAwait(false);
    }
    finally
    {
      _lock.Release();
    }
  }

  private Context BuildContext(IDictionary<string, object>? initialParams)
  {
    var ctx = new Context(NextStateAsync);
    if (initialParams is not null)
    {
      foreach (var kvp in initialParams)
        ctx.Params[kvp.Key] = kvp.Value;
    }

    ctx.LastState = _current;
    return ctx;
  }

  private async Task NextStateAsync(Result result)
  {
    await _lock.WaitAsync().ConfigureAwait(false);
    try
    {
      if (_current == StateId.None)
        throw new InvalidStateTransitionException("No active state to transition from.");

      if (!_transitions.TryGetValue(_current, out var map) || !map.TryGetValue(result, out var next))
        throw new MissingStateTransitionException($"No transition defined from '{_current}' on result '{result}'.");

      _logger.LogInformation("Transition requested: {Current} --({Result})--> {Next}", _current, result, next);

      var ctx = BuildContext(null);
      await TransitionToAsync(next, ctx).ConfigureAwait(false);

      // If sub-FSM reached terminal state (no outgoing transitions), signal completion.
      if (_completionTcs is not null)
      {
        var hasOutgoing = _transitions.TryGetValue(_current, out var outgoing) && outgoing.Count > 0;
        if (!hasOutgoing)
        {
          _logger.LogInformation("FSM reached terminal state '{State}'. Completing with result '{Result}'.", _current, result);
          _completionTcs.TrySetResult(result);
        }
      }
    }
    finally
    {
      _lock.Release();
    }
  }

  private async Task TransitionToAsync(StateId newState, Context ctx)
  {
    if (!_states.TryGetValue(newState, out var lazyNode))
      throw new InvalidStateTransitionException($"State '{newState}' is not registered.");

    var prev = _current;

    // Exit previous if it was created
    if (prev != StateId.None && _states.TryGetValue(prev, out var prevLazy) && prevLazy.IsValueCreated)
    {
      var prevNode = prevLazy.Value;
      _logger.LogTrace("Exiting state '{Prev}'.", prev);
      await prevNode.OnExitAsync(ctx).ConfigureAwait(false);
    }

    // Switch current
    _current = newState;
    var node = lazyNode.Value;

    _logger.LogTrace("Transitioning to state '{New}'.", newState);

    // Enter lifecycle
    await node.OnEnteringAsync(ctx).ConfigureAwait(false);
    await node.OnEnterAsync(ctx).ConfigureAwait(false);
  }
}

#endregion State Machine

#region State Node

/// <summary>State node.</summary>

public abstract class StateNode
{
  protected StateNode(string name, ILogger logger)
  {
    Name = name ?? throw new System.ArgumentNullException(nameof(name));
    Logger = logger ?? NullLogger.Instance;
  }

  /// <summary>Human-readable name for diagnostics/logging.</summary>
  public string Name { get; }

  /// <summary>Logger injected via DI.</summary>
  protected ILogger Logger { get; }

  public async Task OnEnterAsync(Context ctx)
  {
    Logger.LogDebug("State '{StateName}' OnEnter starting.", Name);
    await OnEnterAsyncCore(ctx).ConfigureAwait(false);
    Logger.LogDebug("State '{StateName}' OnEnter finished.", Name);
  }

  // ---------- Transition methods (sealed) ----------
  public async Task OnEnteringAsync(Context ctx)
  {
    Logger.LogTrace("Entering state '{StateName}' (OnEntering). LastState={LastState}", Name, ctx.LastState);
    await OnEnteringAsyncCore(ctx).ConfigureAwait(false);
    Logger.LogTrace("Entered state '{StateName}' (OnEntering finished).", Name);
  }

  public async Task OnExitAsync(Context ctx)
  {
    Logger.LogTrace("Exiting state '{StateName}'.", Name);
    await OnExitAsyncCore(ctx).ConfigureAwait(false);
    Logger.LogTrace("Exited state '{StateName}'.", Name);
  }

  public async Task OnMessageAsync(Context ctx)
  {
    Logger.LogInformation("State '{StateName}' OnMessage received.", Name);
    await OnMessageAsyncCore(ctx).ConfigureAwait(false);
    Logger.LogInformation("State '{StateName}' OnMessage handled.", Name);
  }

  public async Task OnTimeoutAsync(Context ctx)
  {
    Logger.LogWarning("State '{StateName}' Timeout triggered.", Name);
    await OnTimeoutAsyncCore(ctx).ConfigureAwait(false);
    Logger.LogWarning("State '{StateName}' Timeout handling completed.", Name);
  }

  protected virtual Task OnEnterAsyncCore(Context ctx) => Task.CompletedTask;

  // ---------- Override these in derived states ----------

  protected virtual Task OnEnteringAsyncCore(Context ctx) => Task.CompletedTask;

  protected virtual Task OnExitAsyncCore(Context ctx) => Task.CompletedTask;

  protected virtual Task OnMessageAsyncCore(Context ctx) => Task.CompletedTask;

  protected virtual Task OnTimeoutAsyncCore(Context ctx) => Task.CompletedTask;
}

#endregion State Node

#region Composite State

public sealed class CompositeStateNode : StateNode
{
  private readonly StateId _initialChild;
  private readonly StateMachine _subFsm;

  public CompositeStateNode(
    string name,
    ILogger<CompositeStateNode> logger,
    StateMachine subFsm,
    StateId initialChild)
    : base(name, logger)
  {
    _subFsm = subFsm ?? throw new System.ArgumentNullException(nameof(subFsm));
    _initialChild = initialChild;
  }

  protected override async Task OnEnterAsyncCore(Context ctx)
  {
    Logger.LogInformation("Composite '{Name}' starting sub-FSM at child '{Child}'.", Name, _initialChild);

    var childResult = await _subFsm.StartAndWaitAsync(_initialChild, ctx.Params)
                                   .ConfigureAwait(false);

    Logger.LogInformation("Composite '{Name}' sub-FSM completed with Result={Result}. Passing to parent.", Name, childResult);
    await ctx.NextState(childResult).ConfigureAwait(false);
  }

  protected override Task OnMessageAsyncCore(Context ctx)
  {
    Logger.LogDebug("Composite '{Name}' forwarding message to child FSM.", Name);
    return _subFsm.ForwardMessageAsync(ctx.Params);
  }

  protected override Task OnTimeoutAsyncCore(Context ctx)
  {
    Logger.LogWarning("Composite '{Name}' forwarding timeout to child FSM.", Name);
    return _subFsm.ForwardTimeoutAsync();
  }
}

#endregion Composite State

#region Context

public sealed class Context
{
  private readonly Func<Result, Task> _nextState;

  public Context(Func<Result, Task> nextState)
  {
    _nextState = nextState ?? throw new ArgumentNullException(nameof(nextState));
  }

  ////public PropertyBag Errors { get; } = new();
  public Dictionary<string, object> Errors { get; } = new();

  /// <summary>The previous state's enum value.</summary>
  public StateId LastState { get; internal set; } = StateId.None;

  public Dictionary<string, object> Params { get; } = new();

  /// <summary>Triggers moving to the next state, based on a Result.</summary>
  public Task NextState(Result result) => _nextState(result);
}

#endregion Context

#region Property Bag

public interface IPropertyBag : IDictionary<string, object>
{
}

public class PropertyBag : Dictionary<string, object>
{
}

#endregion Property Bag

#region Exceptions

/// <summary>State transition not allowed by pre-defined rule.</summary>
/// <remarks>Happens when a custom-override provided is not in the allowed list.</remarks>
public class InvalidStateTransitionException : Exception
{
  public InvalidStateTransitionException()
  {
  }

  public InvalidStateTransitionException(string message)
    : base(message)
  {
  }
}

public class MissingStateTransitionException : Exception
{
  public MissingStateTransitionException()
  {
  }

  public MissingStateTransitionException(string message)
    : base(message)
  {
  }
}

#endregion Exceptions
