// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.TestData;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

public class BaseStateDI<T>(IMessageService msg, ILogger<T> logger) : IState<BasicStateId>
{
  private readonly ILogger<T> _logger = logger;
  private readonly IMessageService _msgService = msg;

  public virtual Task OnEnter(Context<BasicStateId> context)
  {
    _msgService.Number++;
    _msgService.AddMessage(GetType().Name + " OnEnter");
    _logger.LogInformation("[{StateName}] [OnEnter] => OK", GetType().Name);

    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }

  public virtual Task OnEntering(Context<BasicStateId> context)
  {
    _msgService.Number++;
    _msgService.AddMessage(GetType().Name + " OnEntering");
    _logger.LogInformation("[{StateName}] [OnEntering]", GetType().Name);

    return Task.CompletedTask;
  }

  public virtual Task OnExit(Context<BasicStateId> context)
  {
    _msgService.Number++;
    _msgService.AddMessage(GetType().Name + " OnExit");
    _logger.LogInformation("[{StateName}] [OnExit]", GetType().Name);

    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}

public class BasicDiState1(IMessageService msg, ILogger<BasicDiState1> log)
  : BaseStateDI<BasicDiState1>(msg, log);

public class BasicDiState2(IMessageService msg, ILogger<BasicDiState2> log)
  : BaseStateDI<BasicDiState2>(msg, log);

public class BasicDiState3(IMessageService msg, ILogger<BasicDiState3> log)
  : BaseStateDI<BasicDiState3>(msg, log);

#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
