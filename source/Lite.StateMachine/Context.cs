// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.StateMachine;

using System;
using System.Threading.Tasks;

/// <summary>Context passed to every state. Provides a "Parameter" and a NextState(Result) trigger.</summary>
/// <typeparam name="TStateId">Type of state enum.</typeparam>
public sealed class Context<TStateId>
  where TStateId : struct, Enum
{
#pragma warning disable SA1401 // Fields should be private

  /// <summary>Mapping of the next state transitions for this state.</summary>
  /// <remarks>Optionally override your next transitions.</remarks>
  public StateMap<TStateId> NextStates;

#pragma warning restore SA1401 // Fields should be private

  ////private readonly TaskCompletionSource<Result> _tcs;
  private TaskCompletionSource<Result> _tcs;

  internal Context(
    TStateId currentStateId,
    StateMap<TStateId> nextStates,
    TaskCompletionSource<Result> tcs,
    IEventAggregator? eventAggregator = null,
    Result? lastChildResult = null)
  {
    CurrentStateId = currentStateId;
    NextStates = nextStates;
    _tcs = tcs;
    EventAggregator = eventAggregator;
    LastChildResult = lastChildResult;
  }

  /// <summary>Gets the current State's Id.</summary>
  public TStateId CurrentStateId { get; private set; }

  /// <summary>Gets or sets an arbitrary collection of errors to pass along to the next state.</summary>
  public PropertyBag Errors { get; set; } = [];

  /// <summary>Gets the Event aggregator for Command states (optional).</summary>
  public IEventAggregator? EventAggregator { get; private set; }

  /// <summary>Gets result emitted by the last child state (for composite parents only).</summary>
  public Result? LastChildResult { get; private set; }

  /// <summary>Gets or sets an arbitrary parameter provided by caller to the current action.</summary>
  public PropertyBag Parameters { get; set; } = [];

  /// <summary>Gets the previous state's enum value.</summary>
  public TStateId? PreviousStateId { get; internal set; }

  /// <summary>Not for user consumption. Configures Context for state transitions.</summary>
  /// <param name="tcs">Task Completion Source.</param>
  /// <param name="currentStateId">Current state that we're in.</param>
  /// <param name="previousStateId">State which sent us here.</param>
  public void Configure(TaskCompletionSource<Result> tcs, TStateId currentStateId, TStateId? previousStateId)
  {
    _tcs = tcs;
    CurrentStateId = currentStateId;
    PreviousStateId = previousStateId;
  }

  /// <summary>Not for user consumption. Configures Context for composite state transitions.</summary>
  /// <param name="tcs">Task Completion Source.</param>
  /// <param name="lastChildResult">State which sent us here.</param>
  public void Configure(TaskCompletionSource<Result> tcs, Result? lastChildResult)
  {
    _tcs = tcs;
    LastChildResult = lastChildResult;
  }

  /// <summary>Signal the machine to move forward (only once per state entry).</summary>
  /// <param name="result">Result to pass to the next state.</param>
  /// <remarks>Consider renaming to `StateResult` or `Result` for clarity.</remarks>
  public void NextState(Result result) => _tcs.TrySetResult(result);

  public bool ParameterAsBool(object key, bool defaultBool = false)
  {
    if (Parameters.TryGetValue(key, out var value) && value is bool boolValue)
      return boolValue;
    else
      return defaultBool;
  }

  /// <summary>Get default parameter value as <see cref="int"/> or default.</summary>
  /// <param name="key">Parameter Key.</param>
  /// <param name="defaultInt">Default int (default=0).</param>
  /// <returns>Integer or default.</returns>
  public int ParameterAsInt(object key, int defaultInt = 0)
  {
    if (Parameters.TryGetValue(key, out var value) && value is int intValue)
      return intValue;
    else
      return defaultInt;
  }
}
