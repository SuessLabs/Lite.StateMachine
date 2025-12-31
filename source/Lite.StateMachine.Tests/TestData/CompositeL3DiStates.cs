// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.Logging;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace Lite.StateMachine.Tests.TestData.CompositeL3DiStates;

/// <summary>Do nothing.</summary>
public class State1(IMessageService msg, ILogger<State1> log)
  : BaseDiState<State1, CompositeL3>(msg, log);

public class State2(IMessageService msg, ILogger<State2> log)
  : BaseDiState<State2, CompositeL3>(msg, log);

public class State2_Sub1(IMessageService msg, ILogger<State2_Sub1> log)
  : BaseDiState<State2_Sub1, CompositeL3>(msg, log);

public class State2_Sub2(IMessageService msg, ILogger<State2_Sub2> log)
  : BaseDiState<State2_Sub2, CompositeL3>(msg, log);

public class State2_Sub2_Sub1(IMessageService msg, ILogger<State2_Sub2_Sub1> log)
  : BaseDiState<State2_Sub2_Sub1, CompositeL3>(msg, log);

public class State2_Sub2_Sub2(IMessageService msg, ILogger<State2_Sub2_Sub2> log)
  : BaseDiState<State2_Sub2_Sub2, CompositeL3>(msg, log);

public class State2_Sub2_Sub3(IMessageService msg, ILogger<State2_Sub2_Sub3> log)
  : BaseDiState<State2_Sub2_Sub3, CompositeL3>(msg, log);

public class State2_Sub3(IMessageService msg, ILogger<State2_Sub3> log)
  : BaseDiState<State2_Sub3, CompositeL3>(msg, log);

/// <summary>Make sure not child-created context is there</summary>
public class State3(IMessageService msg, ILogger<State3> log)
  : BaseDiState<State3, CompositeL3>(msg, log)
{
  public override Task OnEnter(Context<CompositeL3> context)
  {
    MessageService.Counter1++;
    Log.LogInformation("[OnEnter] => OK");

    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}

#pragma warning restore IDE0130 // Namespace does not match folder structure
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
