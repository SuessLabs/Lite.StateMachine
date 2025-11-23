// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace LiteState;

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public sealed class CompositeStateNode : StateNode
{
  private readonly State _initialChild;
  private readonly FiniteStateMachine _subFsm;

  public CompositeStateNode(
      string name,
      ILogger<CompositeStateNode> logger,
      FiniteStateMachine subFsm,
      State initialChild)
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
