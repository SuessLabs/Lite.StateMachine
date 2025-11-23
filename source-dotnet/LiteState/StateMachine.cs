// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace LiteState;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public sealed class FiniteStateMachine
{
  private readonly IServiceProvider _services;
  private readonly ILogger<FiniteStateMachine> _logger;

  private readonly Dictionary<State, Lazy<StateNode>> _states = new();
  private readonly Dictionary<State, Dictionary<Result, State>> _transitions = new();
  private readonly SemaphoreSlim _lock = new(1, 1);

  private State _current = State.None;
  private TaskCompletionSource<Result>? _completionTcs;
  private readonly bool _isRoot;

  public FiniteStateMachine(IServiceProvider services,
                            ILogger<FiniteStateMachine> logger,
                            bool isRoot = true)
  {
    _services = services ?? throw new ArgumentNullException(nameof(services));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _isRoot = isRoot;
  }

  /// <summary>Register a state lazily via DI (generic type).</summary>
  public void RegisterState<TState>(State state)
      where TState : StateNode
  {
    _states[state] = new Lazy<StateNode>(
        () => ActivatorUtilities.CreateInstance<TState>(_services),
        LazyThreadSafetyMode.ExecutionAndPublication);
  }

  /// <summary>Register a state lazily via a custom factory that uses DI.</summary>
  public void RegisterState(State state, Func<IServiceProvider, StateNode> factory)
  {
    if (factory is null) throw new ArgumentNullException(nameof(factory));
    _states[state] = new Lazy<StateNode>(
        () => factory(_services),
        LazyThreadSafetyMode.ExecutionAndPublication);
  }

  /// <summary>Configure transitions for a state: Result → Next State.</summary>
  public void SetTransitions(State state, IDictionary<Result, State> map)
  {
    var dict = new Dictionary<Result, State>();
    foreach (var kvp in map)
      dict[kvp.Key] = kvp.Value;
    _transitions[state] = dict;
  }

  public async Task<Result> StartAndWaitAsync(State initial, IDictionary<string, object>? initialParams = null)
  {
    _completionTcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    await StartAsync(initial, initialParams).ConfigureAwait(false);
    return await _completionTcs.Task.ConfigureAwait(false);
  }

  public async Task StartAsync(State initial, IDictionary<string, object>? initialParams = null)
  {
    await _lock.WaitAsync().ConfigureAwait(false);
    try
    {
      if (_current != State.None)
        throw new InvalidOperationException($"FSM already started at state '{_current}'.");

      _logger.LogInformation("FSM starting at state '{Initial}'.", initial);
      await TransitionToAsync(initial, BuildContext(initialParams)).ConfigureAwait(false);
    }
    finally
    {
      _lock.Release();
    }
  }

  public async Task ForwardMessageAsync(IDictionary<string, object>? message = null)
  {
    await _lock.WaitAsync().ConfigureAwait(false);
    try
    {
      if (_current == State.None) return;
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
      if (_current == State.None) return;
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
      if (_current == State.None)
        throw new InvalidOperationException("No active state to transition from.");

      if (!_transitions.TryGetValue(_current, out var map) || !map.TryGetValue(result, out var next))
        throw new KeyNotFoundException($"No transition defined from '{_current}' on result '{result}'.");

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

  private async Task TransitionToAsync(State newState, Context ctx)
  {
    if (!_states.TryGetValue(newState, out var lazyNode))
      throw new InvalidOperationException($"State '{newState}' is not registered.");

    var prev = _current;

    // Exit previous if it was created
    if (prev != State.None && _states.TryGetValue(prev, out var prevLazy) && prevLazy.IsValueCreated)
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
