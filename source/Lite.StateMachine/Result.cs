// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine;

/// <summary>State result for transitions.</summary>
public enum Result
{
  /// <summary>Transition to the next success state (OnSuccess).</summary>
  Success,

  /// <summary>Transition to the next error state (OnError).</summary>
  Error,

  /// <summary>Transition to the next failure state (OnFailure).</summary>
  Failure,
}
