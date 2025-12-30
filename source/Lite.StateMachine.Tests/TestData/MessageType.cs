// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine.Tests.TestData;

/// <summary>
/// Test messages for sending requests from states and receiving responses from outside sources.
/// </summary>
public static class MessageType
{
  public const string BadRequest = "BadRequest";
  public const string BadResponse = "BadResponse";

  public const string ErrorRequest = "ErrorRequest";
  public const string ErrorResponse = "ErrorResponse";

  public const string SuccessRequest = "SuccessRequest";
  public const string SuccessResponse = "SuccessResponse";
}
