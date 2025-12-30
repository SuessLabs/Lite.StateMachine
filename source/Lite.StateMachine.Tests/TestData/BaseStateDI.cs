// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.TestData;

public class BaseStateDI<TStateClass, TStateId>(IMessageService msg, ILogger<TStateClass> logger) : IState<TStateId>
  where TStateId : struct, Enum
{
  private readonly ILogger<TStateClass> _logger = logger;
  private readonly IMessageService _msgService = msg;

  public ILogger<TStateClass> Log => _logger;

  public IMessageService MessageService => _msgService;

  public virtual Task OnEnter(Context<TStateId> context)
  {
    _msgService.Number++;
    _msgService.AddMessage(GetType().Name + " OnEnter");
    _logger.LogInformation("[{StateName}] [OnEnter] => OK", GetType().Name);

    Console.WriteLine($"[{GetType().Name}] [OnEnter] => OK");
    Debug.WriteLine($"[{GetType().Name}] [OnEnter] => OK");

    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }

  public virtual Task OnEntering(Context<TStateId> context)
  {
    _msgService.Number++;
    _msgService.AddMessage(GetType().Name + " OnEntering");
    _logger.LogInformation("[{StateName}] [OnEntering]", GetType().Name);
    Console.WriteLine($"[{GetType().Name}] [OnEntering]");
    Debug.WriteLine($"[{GetType().Name}] [OnEntering]");

    return Task.CompletedTask;
  }

  public virtual Task OnExit(Context<TStateId> context)
  {
    _msgService.Number++;
    _msgService.AddMessage(GetType().Name + " OnExit");
    _logger.LogInformation("[{StateName}] [OnExit]", GetType().Name);
    Debug.WriteLine($"[{GetType().Name}] [OnExit]");

    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}
