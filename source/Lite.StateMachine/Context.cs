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
  private readonly TaskCompletionSource<Result> _tcs;

  internal Context(
    TStateId current,
    TaskCompletionSource<Result> tcs,
    IEventAggregator? eventAggregator = null,
    Result? lastChildResult = null)
  {
    CurrentStateId = current;
    _tcs = tcs;
    EventAggregator = eventAggregator;
    LastChildResult = lastChildResult;
  }

  /// <summary>Gets the current State's Id.</summary>
  public TStateId CurrentStateId { get; }

  /// <summary>Gets or sets an arbitrary collection of errors to pass along to the next state.</summary>
  public PropertyBag ErrorStack { get; set; } = [];

  /// <summary>Gets the Event aggregator for Command states (optional).</summary>
  public IEventAggregator? EventAggregator { get; }

  /// <summary>Gets result emitted by the last child state (for composite parents only).</summary>
  public Result? LastChildResult { get; }

  /////// <summary>Gets the previous state's enum value.</summary>
  ////public TStateId LastStateId { get; internal set; }

  /// <summary>Gets or sets an arbitrary parameter provided by caller to the current action.</summary>
  public PropertyBag Parameters { get; set; } = [];

  /// <summary>Signal the machine to move forward (only once per state entry).</summary>
  /// <param name="result">Result to pass to the next state.</param>
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
