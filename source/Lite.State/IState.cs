// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Lite.State;

/// <summary>Base interface for all states.</summary>
public interface IState<TState> where TState : struct, Enum
{
  /// <summary>Gets the state's Id.</summary>
  TState Id { get; }

  /// <summary>Gets a value indicating that the state is hierarchical and has substates.</summary>
  bool IsComposite { get; }

  /// <summary>
  ///   Outcome-based transitions local to this (sub)machine.
  ///   If the current state cannot resolve an outcome, bubbling occurs to parent composite.
  /// </summary>
  IReadOnlyDictionary<Result, TState> Transitions { get; }

  /// <summary>Transition hook state fully entered.</summary>
  /// <param name="context"><see cref="Context{TState}"/>.</param>
  void OnEnter(Context<TState> context);

  /// <summary>Transition hook before entering, initialize items needed by the state.</summary>
  /// <param name="context"><see cref="Context{TState}"/>.</param>
  void OnEntering(Context<TState> context);  // before entering

  /// <summary>Transition hook leaving state.</summary>
  /// <param name="context"><see cref="Context{TState}"/>.</param>
  void OnExit(Context<TState> context);
}
