// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.StateMachine;

/// <summary>State registration class for lazy-loading.</summary>
/// <typeparam name="TStateId">State Id.</typeparam>
internal sealed class StateRegistration<TStateId>
  where TStateId : struct, Enum
{
  /// <summary>Gets the state factory to execute.</summary>
  /// <remarks>OLD: <![CDATA[public Func<IState<TStateId>>? Factory = default;]]>.</remarks>
  public Func<IState<TStateId>> Factory { get; init; } = default!;

  /// <summary>Gets a value indicating whether this is a composite parent state or not.</summary>
  public bool IsCompositeParent { get; init; }

  /// <summary>Gets the initial child <see cref="TStateId"/> (for Composite states only).</summary>
  public TStateId? InitialChildId { get; init; }

  /// <summary>Gets or sets an optional auto-wire OnError StateId transition.</summary>
  public TStateId? OnError { get; set; } = null;

  /// <summary>Gets or sets an optional auto-wire OnFailure StateId transition.</summary>
  public TStateId? OnFailure { get; set; } = null;

  /// <summary>Gets or sets an optional auto-wire OnSuccess StateId transition.</summary>
  public TStateId? OnSuccess { get; set; } = null;

  /// <summary>Gets the sub-state's parent State Id (optional).</summary>
  public TStateId? ParentId { get; init; }

  /// <summary>Gets or sets the Previous <typeparamref name="TStateId"/> we transitioned from.</summary>
  public TStateId? PreviousStateId { get; set; }

  /// <summary>Gets the State Id, used by ExportUml for <see cref="RegisterState{TStateClass}(TStateId, TStateId?, TStateId?, TStateId?, Action{StateMachine{TStateId}}?)"./> .</summary>
  public TStateId StateId { get; init; }
}
