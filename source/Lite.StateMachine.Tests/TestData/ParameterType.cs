// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine.Tests.TestData;

public enum ParameterType
{
  /// <summary>Generic counter.</summary>
  Counter,

  /// <summary>Tests for DoNotAllowHungStatesTest.</summary>
  HungStateAvoidance,

  /// <summary>Random test.</summary>
  KeyTest,

  /// <summary>Test triggers an early exit. Setting OnSuccess to NULL.</summary>
  TestExitEarly,

  /// <summary>Test trigger to go to an invalid state transition.</summary>
  TestUnregisteredTransition,
}
