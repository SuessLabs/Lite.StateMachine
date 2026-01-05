// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine.Tests.TestData;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>State definitions.</summary>
public enum BasicStateId
{
  State1,
  State2,
  State3,
}

public enum CompositeL1StateId
{
  State1,
  State2,
  State2_Sub1,
  State2_Sub2,
  State3,
}

public enum CompositeL3
{
  State1,
  State2,
  State2_Sub1,
  State2_Sub2,
  State2_Sub2_Sub1,
  State2_Sub2_Sub2,
  State2_Sub2_Sub3,
  State2_Sub3,
  State3,
}

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

public enum CustomStateId
{
  /// <summary>Initial state.</summary>
  State1,

  /// <summary>Dummy state that is registered but never transitioned to.</summary>
  State2_Dummy,

  State2_Success,

  /// <summary>This state must NEVER be registered.</summary>
  State2_Unregistered,

  State2_Sub1,
  State2_Sub2,
  State2_Sub3,

  State3,
}

#pragma warning restore SA1649 // File name should match first type name
