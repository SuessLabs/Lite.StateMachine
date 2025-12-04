// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Sample.Basics;
/*
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lite.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class DemoMachine
{
  public static async Task RunAsync()
  {
    // Build DI container
    var services = new ServiceCollection()
      .AddLogging(b =>
      {
        b.AddConsole();
        b.SetMinimumLevel(LogLevel.Trace);
      })
      // Register state types for DI (transient is typical)
      .AddTransient<InitState>()
      .AddTransient<LoadingState>()
      .AddTransient<ProcessingState>()
      .AddTransient<CompletedState>()
      .AddTransient<ErrorState>()
      .AddTransient<CompositeState>() // if you want DI to be able to create composites generically
      .BuildServiceProvider();

    // Create FSMs with DI + loggers
    var rootLogger = services.GetRequiredService<ILogger<StateMachine>>();
    var fsm = new StateMachine(services, rootLogger, isRoot: true);

    var childLogger = services.GetRequiredService<ILogger<StateMachine>>();
    var subFsm = new StateMachine(services, childLogger, isRoot: false);

    // Register child states lazily via DI
    subFsm.RegisterState<LoadingState>(StateId.Loading);
    subFsm.RegisterState<ProcessingState>(StateId.Processing);
    subFsm.RegisterState<CompletedState>(StateId.Completed);

    // TODO: Set default Error/Failure states for everyone to use
    //  Or, don't set it and have an Exception thrown if missing
    //  - InvalidStateTransitionException
    //  - MissingStateTransitionException

    subFsm.SetTransitions(StateId.Loading, new Dictionary<Result, StateId>
    {
      [Result.Success] = StateId.Processing,
      [Result.Error] = StateId.Completed,
      [Result.Failure] = StateId.Completed
    });

    subFsm.SetTransitions(StateId.Processing, new Dictionary<Result, StateId>
    {
      [Result.Success] = StateId.Completed,
      [Result.Error] = StateId.Completed,
      [Result.Failure] = StateId.Completed
    });

    // Register composite state on root via factory (so we can pass sub-FSM and initial child)
    fsm.RegisterState(StateId.OrderFlow, sp =>
      new CompositeStateNode(
        name: "Order Flow",
        logger: sp.GetRequiredService<ILogger<CompositeState>>(),
        subFsm: subFsm,
        initialChild: StateId.Loading));

    // Register other root states via DI
    fsm.RegisterState<InitState>(StateId.Init);
    fsm.RegisterState<CompletedState>(StateId.Completed);
    fsm.RegisterState<ErrorState>(StateId.Error);

    // Wiring root transitions
    fsm.SetTransitions(StateId.Init, new Dictionary<Result, StateId>
    {
      [Result.Success] = StateId.OrderFlow,
      [Result.Error] = StateId.Error,
      [Result.Failure] = StateId.Error
    });

    fsm.SetTransitions(StateId.OrderFlow, new Dictionary<Result, StateId>
    {
      [Result.Success] = StateId.Completed,
      [Result.Error] = StateId.Error,
      [Result.Failure] = StateId.Error
    });

    // Start the root FSM and wait for terminal outcome
    var finalResult = await fsm.StartAndWaitAsync(StateId.Init);
    Console.WriteLine($"Root FSM finished with result: {finalResult}");
  }
}
*/
