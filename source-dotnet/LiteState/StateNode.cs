// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LiteState;

/// <summary>State transition result.</summary>
public enum Result
{
  Success,
  Error,
  Failure
}

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

  public sealed async Task OnEnterAsync(Context ctx)
  {
    Logger.LogDebug("State '{StateName}' OnEnter starting.", Name);
    await OnEnterAsyncCore(ctx).ConfigureAwait(false);
    Logger.LogDebug("State '{StateName}' OnEnter finished.", Name);
  }

  // ---------- Transition methods (sealed) ----------
  public sealed async Task OnEnteringAsync(Context ctx)
  {
    Logger.LogTrace("Entering state '{StateName}' (OnEntering). LastState={LastState}", Name, ctx.LastState);
    await OnEnteringAsyncCore(ctx).ConfigureAwait(false);
    Logger.LogTrace("Entered state '{StateName}' (OnEntering finished).", Name);
  }

  public sealed async Task OnExitAsync(Context ctx)
  {
    Logger.LogTrace("Exiting state '{StateName}'.", Name);
    await OnExitAsyncCore(ctx).ConfigureAwait(false);
    Logger.LogTrace("Exited state '{StateName}'.", Name);
  }

  public sealed async Task OnMessageAsync(Context ctx)
  {
    Logger.LogInformation("State '{StateName}' OnMessage received.", Name);
    await OnMessageAsyncCore(ctx).ConfigureAwait(false);
    Logger.LogInformation("State '{StateName}' OnMessage handled.", Name);
  }

  public sealed async Task OnTimeoutAsync(Context ctx)
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
