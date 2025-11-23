// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace LiteState;

public enum StateId
{
  None,
  Init,
  Loading,
  Processing,
  Completed,
  Error,

  // Example composite "parent"
  OrderFlow
}
