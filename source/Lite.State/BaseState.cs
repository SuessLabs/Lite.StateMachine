// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Lite.State;

/// <summary>A simple base implementation for states with convenient transition builder.</summary>
/// <remarks>
///   Consider renaming to, 'State'
/// </remarks>
public abstract class BaseState<TState> : IState<TState> where TState : struct, Enum
{
  private readonly Dictionary<Result, TState> _transitions = new();

  protected BaseState(TState id) => Id = id;

  /// <inheritdoc/>
  public TState Id { get; }

  /// <inheritdoc/>
  public virtual bool IsComposite => false;

  /// <inheritdoc/>
  public IReadOnlyDictionary<Result, TState> Transitions => _transitions;

  public void AddTransition(Result outcome, TState target)
  {
    _transitions[outcome] = target;
  }

  /// <inheritdoc/>
  public virtual void OnEnter(Context<TState> context)
  { }

  /// <inheritdoc/>
  public virtual void OnEntering(Context<TState> context)
  { }

  /// <inheritdoc/>
  public virtual void OnExit(Context<TState> context)
  { }
}

/*
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
*/
