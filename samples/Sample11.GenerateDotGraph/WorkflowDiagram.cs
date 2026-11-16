// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System;
using Lite.StateMachine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sample11.GenerateDotGraph;

/// <summary>Exports the same topology as DiMsTests.Basic_GeneratesExportUml_SuccessTestAsync.</summary>
public static class WorkflowDiagram
{
  /// <summary>Builds the configured machine and exports DOT without running any states.</summary>
  /// <param name="includeLegend">Whether to add the Graphviz legend.</param>
  /// <returns>DOT text with Entry as the initial state.</returns>
  public static string ExportDot(bool includeLegend)
  {
    // Configure DI
    using var services = new ServiceCollection()
      .AddLogging(builder => builder.AddSimpleConsole())
      .AddSingleton<IEventAggregator, EventAggregator>()
      .AddTransient<EntryState>()
      .AddTransient<ParentState>()
      .AddTransient<ParentSub_FetchState>()
      .AddTransient<ParentSub_WaitMessageState>()
      .AddTransient<DoneState>()
      .AddTransient<ErrorState>()
      .AddTransient<FailureState>()
      .BuildServiceProvider();

    // Factory uses DI to construct states
    var aggregator = services.GetRequiredService<IEventAggregator>();
    Func<Type, object?> factory = type => ActivatorUtilities.CreateInstance(services, type);

    // Create State Machine
    var machine = new StateMachine<CompositeMsgStateId>(factory, aggregator);

    machine.RegisterState<EntryState>(CompositeMsgStateId.Entry, CompositeMsgStateId.Parent);
    machine.RegisterComposite<ParentState>(
      stateId: CompositeMsgStateId.Parent,
      initialChildStateId: CompositeMsgStateId.Parent_Fetch,
      onSuccess: CompositeMsgStateId.Done,
      onError: CompositeMsgStateId.Error,
      onFailure: CompositeMsgStateId.Failure);

    //// Substates of ParentState
    machine.RegisterSubState<ParentSub_FetchState>(CompositeMsgStateId.Parent_Fetch, CompositeMsgStateId.Parent, CompositeMsgStateId.Parent_WaitMessage);
    machine.RegisterSubState<ParentSub_WaitMessageState>(CompositeMsgStateId.Parent_WaitMessage, CompositeMsgStateId.Parent);

    // End of substates
    machine.RegisterState<DoneState>(CompositeMsgStateId.Done);
    machine.RegisterState<ErrorState>(CompositeMsgStateId.Error, CompositeMsgStateId.Parent);
    machine.RegisterState<FailureState>(CompositeMsgStateId.Failure, CompositeMsgStateId.Parent);

    // Export the DOT graph with Entry as the initial state.
    return machine.ExportUml([CompositeMsgStateId.Entry], includeLegend: includeLegend);
  }
}
