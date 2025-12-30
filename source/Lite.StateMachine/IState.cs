// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Lite.StateMachine;

/// <summary>Base interface for all states.</summary>
/// <typeparam name="TStateId">Type of state.</typeparam>
public interface IState<TStateId>
  where TStateId : struct, Enum
{
  /// <summary>Transition hook state fully entered.</summary>
  /// <param name="context"><see cref="Context{TState}"/>.</param>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task OnEnter(Context<TStateId> context);

  /// <summary>Transition hook before entering, initialize items needed by the state.</summary>
  /// <param name="context"><see cref="Context{TState}"/>.</param>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task OnEntering(Context<TStateId> context);

  /// <summary>Transition hook leaving state.</summary>
  /// <param name="context"><see cref="Context{TState}"/>.</param>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task OnExit(Context<TStateId> context);

  /////// <summary>Forcibly set the <see cref="TStateId"/> late; used by, ExportUml().</summary>
  /////// <param name="id">State Id.</param>
  ////void SetStateId(TStateId id);
}
