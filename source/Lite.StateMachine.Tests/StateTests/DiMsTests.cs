// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData;
using Lite.StateMachine.Tests.TestData.Services;
using Lite.StateMachine.Tests.TestData.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Lite.StateMachine.Tests.StateTests;

/// <summary>Microsoft Dependency Injection Tests.</summary>
[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Allowed for this test class")]
public class DiMsTests : TestBase
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
    AssertMachineNotNull(machine);

    var msgService = services.GetRequiredService<IMessageService>();
    Assert.AreEqual(9, msgService.Counter1, "Message service should have 9 from the 3 states.");

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're registered in order
    Assert.IsTrue(enums.SequenceEqual(machine.States), "States should be registered for execution in the same order as the defined enums, StateId 1 => 2 => 3.");
  }

  [TestMethod]
  public async Task Basic_GeneratesExportUml_SuccessTestAsync()
  {
    // Build DI
    var services = new ServiceCollection()
      //// Register Services
      .AddLogging(b => b.AddSimpleConsole())
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

    // Factory uses DI to construct states
    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);
    var machine = new StateMachine<CompositeMsgStateId>(factory, aggregator);

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
    machine.RegisterState<Workflow_ErrorState>(CompositeMsgStateId.Error, CompositeMsgStateId.Parent);
    machine.RegisterState<Workflow_FailureState>(CompositeMsgStateId.Failure, CompositeMsgStateId.Parent);

    // Act - Generate UML
    var umlBasic = machine.ExportUml([CompositeMsgStateId.Entry], includeLegend: false);
    var umlLegend = machine.ExportUml([CompositeMsgStateId.Entry], includeLegend: true);

    // Assert
    Assert.IsNotNull(umlBasic);
    Assert.IsNotNull(umlLegend);

    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.CompositeWithErrorFailure(false), umlBasic);
    AssertExtensions.AreEqualIgnoreLines(ExpectedUmlData.CompositeWithErrorFailure(true), umlLegend);
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
    AssertMachineNotNull(machine);

    // Ensure all states are registered
    var enums = Enum.GetValues<BasicStateId>().Cast<BasicStateId>();
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're registered in order
    Assert.IsTrue(enums.SequenceEqual(machine.States), "States should be registered for execution in the same order as the defined enums, StateId 1 => 2 => 3.");
  }

  /// <summary>Following demonstrates Composite + Command States with Dependency Injection.</summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
  /// <remarks>
  ///   NOTE:
  ///   * The test worked off MessageService.Counter2 to determine the flow.
  ///     Cntr2 = 0 => OnTimeout => FailureState
  ///     Cntr2 = 1 => OnEnter   => ErrorState
  ///     Cntr2 = 2 => OnEnter   => DoneState
  ///   * You MUST publish while in the ParentSub_WaitMessageState
  ///     otherwise the message is never received (rightfully so).
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
    var msgService = services.GetRequiredService<IMessageService>();
    var logService = services.GetRequiredService<ILogger<DiMsTests>>();

    msgService.Counter1 = 0;
    msgService.Counter2 = 0;

    // Factory uses DI to construct states
    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);
    var machine = new StateMachine<CompositeMsgStateId>(factory, aggregator)
    {
      DefaultCommandTimeoutMs = 200,
      DefaultStateTimeoutMs = 5000,
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
    machine.RegisterState<Workflow_ErrorState>(CompositeMsgStateId.Error, CompositeMsgStateId.Parent);
    machine.RegisterState<Workflow_FailureState>(CompositeMsgStateId.Failure, CompositeMsgStateId.Parent);

    // vNext:  (for now, we send a message during WaitFor's OnEnter and send back.
    // 1. Start the state machine
    //    - (CommandTimeout set to 200ms)
    //    - Subscribe to EventAggregator to respond to messages
    // 2. ParentSub_WaitMessageState will Timeout, not receiving a message at all (Counter2++)
    // 3. OnTimeout() publishes, "NotificationType.Timeout", which is caught by the aggregator subscription below.
    // 4. Subscriber replies with, "BadResponse" which goes nowhere since we're not in OnMessage yet.
    // 5. OnEnter() Counter==1, publishes "ErrorRequest"
    // 6. Subscriber replies with, "ErrorResponse"
    // 7. OnMessage() receives 'ErrorResponse', setting Response.Error and goes to ErrorState (Counter2++)
    // 8. OnEnter() notes Counter2==2", publishing 'SuccessRequest'. Aggregator responds with 'SuccessResponse'.
    // 9. OnMessage() sets Response.Ok and goes to Done
    aggregator.Subscribe(message =>
    {
      if (message is not string msg)
        return;

      // NOTE:
      //  You MUST publish while in the ParentSub_WaitMessageState
      //  otherwise the message is never received (rightfully so).
      switch (msg)
      {
        case NotificationType.Timeout:
          // This will never get picked up
          Debug.WriteLine($"> RCV 'Timeout' > SEND: {MessageType.BadResponse}");
          logService.LogInformation("Publish >> {msg} (NO ONE SHOULD RECEIVE/RESPOND)", MessageType.BadResponse);
          aggregator.Publish(MessageType.BadResponse);
          break;

        case MessageType.ErrorRequest:
          Debug.WriteLine($"> RCV '{msg}' > SEND: {MessageType.ErrorResponse}");
          logService.LogInformation("Publish >> {msg}", MessageType.ErrorResponse);
          aggregator.Publish(MessageType.ErrorResponse);
          break;

        case MessageType.SuccessRequest:
          Debug.WriteLine($"> RCV '{msg}' > SEND: {MessageType.SuccessResponse}");
          logService.LogInformation("Publish >> {msg}", MessageType.SuccessResponse);
          aggregator.Publish(MessageType.SuccessResponse);
          break;

        case MessageType.BadRequest:
        default:
          Assert.Fail("Unexpected message");
          break;
      }
    });

    // Act - Run the state machine and send messages
    await machine.RunAsync(CompositeMsgStateId.Entry, CancellationToken.None);

    Console.WriteLine("MS.DI workflow finished.");

    // Assert
    AssertMachineNotNull(machine);

    Assert.AreEqual(2, msgService.Counter2);
    Assert.AreEqual(42, msgService.Counter1);
  }
}
