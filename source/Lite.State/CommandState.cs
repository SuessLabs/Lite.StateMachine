// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.State;

/// <summary>A base class for command states, adding no behavior itself (machine handles timer/subscriptions).</summary>
public abstract class CommandState<TState> : BaseState<TState>, ICommandState<TState> where TState : struct, Enum
{
  protected CommandState(TState id) : base(id)
  {
  }

  /// <summary>Message filtration (string objects only).</summary>
  public new virtual Func<object, bool> MessageFilter => _ => true;

  /// <summary>Gets a value to override the default <see cref="StateMachine{TState}"/> timeout (in milliseconds).</summary>
  public virtual int? TimeoutOverrideMs => null;

  /// <inheritdoc/>
  Func<object, bool> ICommandState<TState>.MessageFilter => MessageFilter;

  /// <inheritdoc/>
  int? ICommandState<TState>.TimeoutMs => TimeoutOverrideMs;

  /// <inheritdoc/>
  public virtual void OnMessage(Context<TState> context, object message)
  { }

  /// <inheritdoc/>
  public virtual void OnTimeout(Context<TState> context)
  { }
}
