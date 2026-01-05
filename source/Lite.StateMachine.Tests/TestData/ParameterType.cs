// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine.Tests.TestData;

public enum ParameterType
{
  /// <summary>Generic counter.</summary>
  Counter,

  /// <summary>Random test.</summary>
  KeyTest,

  /// <summary>Test states executed in order using context.LastStateId or states in a different order.</summary>
  TestExecutionOrder,

  /// <summary>Test triggers an early exit. Setting OnSuccess to NULL.</summary>
  TestExitEarly,

  /// <summary>Test triggers a 2nd early exit. Setting OnSuccess to NULL.</summary>
  TestExitEarly2,

  /// <summary>Tests for DoNotAllowHungStatesTest.</summary>
  TestHungStateAvoidance,

  /// <summary>Test trigger to go to an invalid state transition.</summary>
  TestUnregisteredTransition,
}
