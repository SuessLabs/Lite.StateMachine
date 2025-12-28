// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.StateMachine;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Intentional public fields.")]
public sealed class StateRegistration<TStateId>
  where TStateId : struct, Enum
{
  /// <summary>Used for composite states.</summary>
  public Action<StateMachine<TStateId>>? ConfigureSubmachine;

  /// <summary>Gets or sets the State Id, used by ExportUml for <see cref="RegisterState{TStateClass}(TStateId, TStateId?, TStateId?, TStateId?, Action{StateMachine{TStateId}}?)"./> .</summary>
  public TStateId StateId;

  public Lazy<IState<TStateId>>? LazyInstance;

  /// <summary>Optional auto-wire OnError StateId transition.</summary>
  public TStateId? OnError = null;

  /// <summary>Optional auto-wire OnFailure StateId transition.</summary>
  public TStateId? OnFailure = null;

  /// <summary>Optional auto-wire OnSuccess StateId transition.</summary>
  public TStateId? OnSuccess = null;

  /// <summary>Gets the state factory to execute.</summary>
  //// OLD: public Func<IState<TStateId>>? Factory = default;
  //// r4c: public Func<IServiceResolver?, IState<TStateId>>? Factory = default;
  public Func<IState<TStateId>> Factory { get; init; } = default!;
}
