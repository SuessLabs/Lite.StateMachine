// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Sample.Basics;

using System;
using System.Threading.Tasks;
using LiteState;
using Microsoft.Extensions.Logging;

public sealed class CompletedState : StateNode
{
  public CompletedState(ILogger<CompletedState> logger)
    : base("Completed", logger) { }

  protected override Task OnEnterAsyncCore(Context ctx)
  {
    Logger.LogInformation("[Completed] OnEnter - done.");
    return Task.CompletedTask;
  }
}

public sealed class InitState : StateNode
{
  public InitState(ILogger<InitState> logger)
    : base("Initialization", logger)
  {
  }

  protected override async Task OnEnterAsyncCore(Context ctx)
  {
    Logger.LogInformation("[Init] OnEnter");
    ctx.Params["startedAt"] = DateTime.UtcNow;
    await ctx.NextState(Result.Success);
  }
}

public sealed class LoadingState : StateNode
{
  public LoadingState(ILogger<LoadingState> logger)
    : base("Loading", logger)
  {
  }

  protected override async Task OnEnterAsyncCore(Context ctx)
  {
    Logger.LogInformation("[Loading] OnEnter - fetching resources...");
    await Task.Delay(100);
    await ctx.NextState(Result.Success);
  }

  protected override async Task OnEnteringAsyncCore(Context ctx)
  {
    Logger.LogDebug("[Loading] OnEntering - preparing I/O...");
    await Task.Delay(50);
  }
}

public sealed class ProcessingState : StateNode
{
  public ProcessingState(ILogger<ProcessingState> logger)
    : base("Processing", logger)
  {
  }

  protected override async Task OnEnterAsyncCore(Context ctx)
  {
    Logger.LogInformation("[Processing] OnEnter - start processing");
    await Task.Delay(100);
    await OnMessageAsyncCore(ctx); // demo: self-trigger message
  }

  protected override async Task OnMessageAsyncCore(Context ctx)
  {
    Logger.LogInformation("[Processing] OnMessage - received event");
    await ctx.NextState(Result.Success);
  }

  protected override async Task OnTimeoutAsyncCore(Context ctx)
  {
    Logger.LogWarning("[Processing] OnTimeout - no events received in time");
    ctx.Errors["timeout"] = "Processing timed out.";
    await ctx.NextState(Result.Failure);
  }
}

public sealed class ErrorState : StateNode
{
  public ErrorState(ILogger<ErrorState> logger)
    : base("Error", logger)
  {
  }

  protected override Task OnEnterAsyncCore(Context ctx)
  {
    Logger.LogError("[Error] OnEnter - error occurred.");
    foreach (var e in ctx.Errors)
      Logger.LogError("  {Key}: {Value}", e.Key, e.Value);

    return Task.CompletedTask;
  }
}
