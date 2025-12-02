// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace LiteState.Mk2.Models;

public class StateHistory
{
  private readonly Dictionary<StateId, StateId?> _history = new();

  public void Record(StateId parent, StateId child)
  {
    _history[parent] = child;
  }

  public StateId? GetLast(StateId parent)
  {
    return _history.TryGetValue(parent, out var last) ? last : null;
  }
}
