// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace LiteState;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LiteState.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
