// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.TestData.States;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

public class BasicDiState1(IMessageService msg, ILogger<BasicDiState1> log)
  : DiStateBase<BasicDiState1, BasicStateId>(msg, log);

public class BasicDiState2(IMessageService msg, ILogger<BasicDiState2> log)
  : DiStateBase<BasicDiState2, BasicStateId>(msg, log);

public class BasicDiState3(IMessageService msg, ILogger<BasicDiState3> log)
  : DiStateBase<BasicDiState3, BasicStateId>(msg, log);

#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
