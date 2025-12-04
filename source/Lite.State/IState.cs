// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Lite.State;

/// <summary>
/// Base interface for all states.
/// </summary>
public interface IState<TState> where TState : struct, Enum
{
  TState Id { get; }

  /// <summary>
  /// Indicates hierarchical state.
  /// </summary>
  bool IsComposite { get; }

  /// <summary>
  /// Outcome-based transitions local to this (sub)machine.
  /// If the current state cannot resolve an outcome, bubbling occurs to parent composite.
  /// </summary>
  IReadOnlyDictionary<Result, TState> Transitions { get; }

  void OnEnter(Context<TState> context);

  // Transition hooks:
  void OnEntering(Context<TState> context);  // before entering

  // entered
  void OnExit(Context<TState> context);      // leaving
}
