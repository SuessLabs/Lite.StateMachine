// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lite.StateMachine;
using Microsoft.Extensions.Logging;

namespace Sample11.GenerateDotGraph;

#pragma warning disable SA1402 // Keep the small diagram-only state definitions together.
#pragma warning disable SA1649 // File groups related state definitions.

/// <summary>State identifiers matching the composite export test.</summary>
public enum CompositeMsgStateId
{
  Entry,
  Parent,
  Parent_Fetch,
  Parent_WaitMessage,
  Done,
  Error,
  Failure,
}

public sealed class EntryState(ILogger<DiagramState> logger) : DiagramState(logger);

public sealed class ParentState(ILogger<DiagramState> logger) : DiagramState(logger);

public sealed class ParentSub_FetchState(ILogger<DiagramState> logger) : DiagramState(logger);

public sealed class ParentSub_WaitMessageState(ILogger<DiagramState> logger)
  : DiagramState(logger), ICommandState<CompositeMsgStateId>
{
  public Task OnMessage(Context<CompositeMsgStateId> context, object message) => Task.CompletedTask;

  public Task OnTimeout(Context<CompositeMsgStateId> context)
  {
    context.NextState(Result.Failure);
    return Task.CompletedTask;
  }
}

public sealed class DoneState(ILogger<DiagramState> logger) : DiagramState(logger);

public sealed class ErrorState(ILogger<DiagramState> logger) : DiagramState(logger);

public sealed class FailureState(ILogger<DiagramState> logger) : DiagramState(logger);

/// <summary>Minimal state behavior; diagram generation never executes these handlers.</summary>
public abstract class DiagramState(ILogger<DiagramState> logger) : IState<CompositeMsgStateId>
{
  public Task OnEnter(Context<CompositeMsgStateId> context)
  {
    logger.LogInformation("Entered {StateId}", context.CurrentStateId);
    context.NextState(Result.Success);
    return Task.CompletedTask;
  }

  public Task OnEntering(Context<CompositeMsgStateId> context) => Task.CompletedTask;

  public Task OnExit(Context<CompositeMsgStateId> context) => Task.CompletedTask;
}

#pragma warning restore SA1649 // File groups related state definitions.
#pragma warning restore SA1402 // Keep the small diagram-only state definitions together.
