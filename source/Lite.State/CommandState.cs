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
  public virtual new Func<object, bool> MessageFilter => _ => true;

  public virtual int? TimeoutOverrideMs => null;

  Func<object, bool> ICommandState<TState>.MessageFilter => MessageFilter;

  int? ICommandState<TState>.TimeoutMs => TimeoutOverrideMs;

  public virtual void OnMessage(Context<TState> context, object message)
  { }

  public virtual void OnTimeout(Context<TState> context)
  { }
}
