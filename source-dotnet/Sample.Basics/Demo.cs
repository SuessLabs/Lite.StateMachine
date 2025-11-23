// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Sample.Basics;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteState;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class Demo
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
      .AddTransient<CompositeStateNode>() // if you want DI to be able to create composites generically
      .BuildServiceProvider();

    // Create FSMs with DI + loggers
    var rootLogger = services.GetRequiredService<ILogger<FiniteStateMachine>>();
    var fsm = new FiniteStateMachine(services, rootLogger, isRoot: true);

    var childLogger = services.GetRequiredService<ILogger<FiniteStateMachine>>();
    var subFsm = new FiniteStateMachine(services, childLogger, isRoot: false);

    // Register child states lazily via DI
    subFsm.RegisterState<LoadingState>(State.Loading);
    subFsm.RegisterState<ProcessingState>(State.Processing);
    subFsm.RegisterState<CompletedState>(State.Completed);

    subFsm.SetTransitions(State.Loading, new Dictionary<Result, State>
    {
      [Result.Success] = State.Processing,
      [Result.Error] = State.Completed,
      [Result.Failure] = State.Completed
    });

    subFsm.SetTransitions(State.Processing, new Dictionary<Result, State>
    {
      [Result.Success] = State.Completed,
      [Result.Error] = State.Completed,
      [Result.Failure] = State.Completed
    });

    // Register composite state on root via factory (so we can pass sub-FSM and initial child)
    fsm.RegisterState(State.OrderFlow, sp =>
      new CompositeStateNode(
        name: "Order Flow",
        logger: sp.GetRequiredService<ILogger<CompositeStateNode>>(),
        subFsm: subFsm,
        initialChild: State.Loading));

    // Register other root states via DI
    fsm.RegisterState<InitState>(State.Init);
    fsm.RegisterState<CompletedState>(State.Completed);
    fsm.RegisterState<ErrorState>(State.Error);

    // Wiring root transitions
    fsm.SetTransitions(State.Init, new Dictionary<Result, State>
    {
      [Result.Success] = State.OrderFlow,
      [Result.Error] = State.Error,
      [Result.Failure] = State.Error
    });

    fsm.SetTransitions(State.OrderFlow, new Dictionary<Result, State>
    {
      [Result.Success] = State.Completed,
      [Result.Error] = State.Error,
      [Result.Failure] = State.Error
    });

    // Start the root FSM and wait for terminal outcome
    var finalResult = await fsm.StartAndWaitAsync(State.Init);
    Console.WriteLine($"Root FSM finished with result: {finalResult}");
  }
}
