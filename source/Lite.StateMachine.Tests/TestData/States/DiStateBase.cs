// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.TestData.States;

public class DiStateBase<TStateClass, TStateId>(IMessageService msg, ILogger<TStateClass> logger) : IState<TStateId>
  where TStateId : struct, Enum
{
  private readonly ILogger<TStateClass> _logger = logger;
  private readonly IMessageService _msgService = msg;

  /// <summary>Gets or sets a value indicating whether output transitions for debugging tests.</summary>
  public bool HasExtraLogging { get; set; } = false;

  public ILogger<TStateClass> Log => _logger;

  public IMessageService MessageService => _msgService;

  public virtual Task OnEnter(Context<TStateId> context)
  {
    _msgService.Counter1++;
    ////_msgService.AddMessage(GetType().Name + " OnEnter");
    _logger.LogInformation("[OnEnter] => OK");

    if (HasExtraLogging)
      Debug.WriteLine($"[{GetType().Name}] [OnEnter] => OK");

    context.NextState(Result.Success);
    return Task.CompletedTask;
  }

  public virtual Task OnEntering(Context<TStateId> context)
  {
    _msgService.Counter1++;
    ////_msgService.AddMessage(GetType().Name + " OnEntering");
    _logger.LogInformation("[OnEntering]");

    if (HasExtraLogging)
      Debug.WriteLine($"[{GetType().Name}] [OnEntering]");

    return Task.CompletedTask;
  }

  public virtual Task OnExit(Context<TStateId> context)
  {
    _msgService.Counter1++;
    ////_msgService.AddMessage(GetType().Name + " OnExit");
    _logger.LogInformation("[OnExit]");

    if (HasExtraLogging)
      Debug.WriteLine($"[{GetType().Name}] [OnExit]");

    context.NextState(Result.Success);
    return Task.CompletedTask;
  }
}
