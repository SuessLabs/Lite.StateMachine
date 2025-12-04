// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Lite.State;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// A base class for composite states. The submachine is injected/assigned externally.
/// </summary>
public abstract class CompositeState<TState> : BaseState<TState>, ICompositeState<TState> where TState : struct, Enum
{
  protected CompositeState(TState id) : base(id)
  {
    // : base(name, logger)
  }

  public override bool IsComposite => true;

  public StateMachine<TState> Submachine { get; internal set; } = default!;
}

/*
public sealed class CompositeState : StateNode
{
  private readonly StateId _initialChild;
  private readonly StateMachine _subFsm;

  public CompositeState(
    string name,
    ILogger<CompositeState> logger,
    StateMachine subFsm,
    StateId initialChild)
    : base(name, logger)
  {
    _subFsm = subFsm ?? throw new System.ArgumentNullException(nameof(subFsm));
    _initialChild = initialChild;
  }

  protected override async Task OnEnterAsyncCore(Context ctx)
  {
    Logger.LogInformation("Composite '{Name}' starting sub-FSM at child '{Child}'.", Name, _initialChild);

    var childResult = await _subFsm.StartAndWaitAsync(_initialChild, ctx.Params)
                                   .ConfigureAwait(false);

    Logger.LogInformation("Composite '{Name}' sub-FSM completed with Result={Result}. Passing to parent.", Name, childResult);
    await ctx.NextState(childResult).ConfigureAwait(false);
  }

  protected override Task OnMessageAsyncCore(Context ctx)
  {
    Logger.LogDebug("Composite '{Name}' forwarding message to child FSM.", Name);
    return _subFsm.ForwardMessageAsync(ctx.Params);
  }

  protected override Task OnTimeoutAsyncCore(Context ctx)
  {
    Logger.LogWarning("Composite '{Name}' forwarding timeout to child FSM.", Name);
    return _subFsm.ForwardTimeoutAsync();
  }
}
*/
