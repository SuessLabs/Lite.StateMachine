// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine.Tests.TestData.Models;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

/// <summary>Signifies it's one of our event packets.</summary>
public interface ICustomCommand;

public class CancelCommand : ICustomCommand
{
  public int Counter { get; set; }
}

public class CancelResponse : ICustomCommand;

/// <summary>Sample command response received by state machine.</summary>
public class CloseResponse : ICustomCommand
{
  public int Counter { get; set; } = 0;
}

/// <summary>Sample command sent by state machine.</summary>
public class UnlockCommand : ICustomCommand
{
  public int Counter { get; set; } = 0;
}

/// <summary>Sample command response received by state machine.</summary>
public class UnlockResponse : ICustomCommand
{
  public int Counter { get; set; } = 0;
}

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
