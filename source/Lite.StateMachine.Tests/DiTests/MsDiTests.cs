// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData;
using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Lite.StateMachine.Tests.DiTests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Allowed for this test class")]
public class MsDiTests
{
  [TestMethod]
  public async Task Basic_FlatStates_SuccessTestAsync()
  {
    // Assemble with Dependency Injection
    var services = new ServiceCollection()
      //// Register Services
      .AddLogging(b => b.AddSimpleConsole())
      .AddSingleton<IMessageService, MessageService>()
      //// Register States
      .AddTransient<BasicDiState1>()
      .AddTransient<BasicDiState2>()
      .AddTransient<BasicDiState3>()
      .BuildServiceProvider();

    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);

    var machine = new StateMachine<BasicStateId>(factory)
      .RegisterState<BasicDiState1>(BasicStateId.State1, BasicStateId.State2)
      .RegisterState<BasicDiState2>(BasicStateId.State2, BasicStateId.State3)
      .RegisterState<BasicDiState3>(BasicStateId.State3);

    var result = await machine.RunAsync(BasicStateId.State1);

    Assert.IsNotNull(result);
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    var msgService = services.GetRequiredService<IMessageService>();
    Assert.AreEqual(9, msgService.Number, "Message service should have 9 from the 3 states.");
    Assert.HasCount(9, msgService.Messages, "Message service should have 9 messages from the 3 states.");

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're registered in order
    Assert.IsTrue(enums.SequenceEqual(machine.States), "States should be registered for execution in the same order as the defined enums, StateId 1 => 2 => 3.");
  }

  [TestMethod]
  [Ignore]
  public async Task Basic_GeneratesExportUml_SuccessTestAsync()
  {
    // Assemble with Dependency Injection
    var services = new ServiceCollection()
      //// Register Services
      .AddSingleton<IMessageService, MessageService>()
      //// Register States
      .AddTransient<BasicDiState1>()
      .AddTransient<BasicDiState2>()
      .AddTransient<BasicDiState3>()
      .BuildServiceProvider();

    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);

    var machine = new StateMachine<BasicStateId>(factory)
      .RegisterState<BasicDiState1>(BasicStateId.State1, BasicStateId.State2)
      .RegisterState<BasicDiState2>(BasicStateId.State2, BasicStateId.State3)
      .RegisterState<BasicDiState3>(BasicStateId.State3);

    var result = await machine.RunAsync(BasicStateId.State1);

    ////// Act - Generate UML
    ////var uml = machine.ExportUml();

    Assert.IsNotNull(result);
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);
  }

  [TestMethod]
  public async Task Basic_LogLevelNone_SuccessTestAsync()
  {
    // Assemble with Dependency Injection
    var services = new ServiceCollection()

      // Register Services
      .AddLogging(builder =>
      { // Set level to Error to reduce test output
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.None);
      })
      .AddSingleton<IEventAggregator, EventAggregator>()
      .AddSingleton<IMessageService, MessageService>()

      // Register States
      .AddTransient<BasicDiState1>()
      .AddTransient<BasicDiState2>()
      .AddTransient<BasicDiState3>();

    using var provider = services.BuildServiceProvider();
    var aggregator = provider.GetRequiredService<IEventAggregator>();

    object? Factory(Type t) => ActivatorUtilities.CreateInstance(provider, t);

    var machine = new StateMachine<BasicStateId>(Factory)
      .RegisterState<BasicDiState1>(BasicStateId.State1, BasicStateId.State2)
      .RegisterState<BasicDiState2>(BasicStateId.State2, BasicStateId.State3)
      .RegisterState<BasicDiState3>(BasicStateId.State3);

    var result = await machine.RunAsync(BasicStateId.State1);

    Assert.IsNotNull(result);
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're registered in order
    Assert.IsTrue(enums.SequenceEqual(machine.States), "States should be registered for execution in the same order as the defined enums, StateId 1 => 2 => 3.");
  }

  /// <summary>Following demonstrates Composite + Command States with Dependency Injection.</summary>
  /// <remarks>
  ///   Test StateMachine Flow:
  ///   1. EntryState => ParentState => ParentSub_FetchState => ParentSub_WaitMessageState (waits for messages from the EventAggregator).
  ///   2. ParentState => FailureState => ParentState => ParentSub_FetchState => ParentSub_WaitMessageState
  ///   3. ParentState => ErrorState => ParentState => ParentSub_FetchState
  ///   Workflow_FailureState => ErrorState => Workflow_DoneState based on the received counter.</remarks>
  /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
  [TestMethod]
  public async Task RegisterState_MsDi_EventAggregatorOnly_SuccessTestAsync()
  {
    // Build DI
    var services = new ServiceCollection()
      //// Register Services
      ////.AddLogging(b => b.AddSimpleConsole())
      .AddLogging(config =>
      {
        // Creates in-line log format
        config.AddSimpleConsole(options =>
        {
          options.TimestampFormat = "HH:mm:ss.fff ";
          options.UseUtcTimestamp = false;
          options.IncludeScopes = true;
          options.SingleLine = true;
          options.ColorBehavior = LoggerColorBehavior.Enabled;
        });
        config.SetMinimumLevel(LogLevel.Trace);
      })
      .AddSingleton<IEventAggregator, EventAggregator>()
      .AddSingleton<IMessageService, MessageService>()
      //// Register States
      .AddTransient<EntryState>()
      .AddTransient<ParentState>()
      .AddTransient<ParentSub_FetchState>()
      .AddTransient<ParentSub_WaitMessageState>()
      .AddTransient<Workflow_DoneState>()
      .AddTransient<Workflow_ErrorState>()
      .AddTransient<Workflow_FailureState>()
      .BuildServiceProvider();

    var aggregator = services.GetRequiredService<IEventAggregator>();
    var messages = services.GetRequiredService<IMessageService>();

    // Factory uses DI to construct states
    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);
    var machine = new StateMachine<CompositeMsgStateId>(factory, aggregator)
    {
      DefaultCommandTimeoutMs = 200,
      DefaultStateTimeoutMs = 8000,
    };

    machine.RegisterState<EntryState>(CompositeMsgStateId.Entry, CompositeMsgStateId.Parent);
    machine.RegisterComposite<ParentState>(
      stateId: CompositeMsgStateId.Parent,
      initialChildStateId: CompositeMsgStateId.Parent_Fetch,
      onSuccess: CompositeMsgStateId.Done,
      onError: CompositeMsgStateId.Error,
      onFailure: CompositeMsgStateId.Failure);
    machine.RegisterSubState<ParentSub_FetchState>(CompositeMsgStateId.Parent_Fetch, CompositeMsgStateId.Parent, CompositeMsgStateId.Parent_WaitMessage);
    machine.RegisterSubState<ParentSub_WaitMessageState>(CompositeMsgStateId.Parent_WaitMessage, CompositeMsgStateId.Parent);
    machine.RegisterState<Workflow_DoneState>(CompositeMsgStateId.Done);
    machine.RegisterState<Workflow_ErrorState>(CompositeMsgStateId.Error);
    machine.RegisterState<Workflow_FailureState>(CompositeMsgStateId.Failure, null);

    // Act - Run the state machine and send messages
    await machine.RunAsync(CompositeMsgStateId.Entry, null, null, CancellationToken.None);

    // Drive command state
    ////await Task.Delay(500);
    ////aggregator.Publish(ExpectedData.StringBadData);

    await Task.Delay(500);
    aggregator.Publish(ExpectedData.StringSuccess);

    Console.WriteLine("MS.DI workflow finished.");
    Assert.HasCount(16, messages.Messages);
    Assert.AreEqual(16, messages.Number);
  }
}
