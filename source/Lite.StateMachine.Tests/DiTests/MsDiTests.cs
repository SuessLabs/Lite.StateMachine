// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData;
using Lite.StateMachine.Tests.TestData.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.DiTests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Allowed for this test class")]
public class MsDiTests
{
  public const string ParameterCounter = "Counter";
  public const string ParameterKeyTest = "TestKey";
  public const string TestValue = "success";

  /// <summary>Composite with failure state definitions.</summary>
  private enum StateId
  {
    WorkflowParent,
    Fetch,
    WaitForMessage,
    Done,
    Error,
  }

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
  public async Task Basic_LogLevel_SuccessTestAsync()
  {
    // Assemble with Dependency Injection
    var services = new ServiceCollection()

      // Register Services
      .AddLogging(builder =>
      {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Error);
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

  /*
  [TestMethod]
  [Ignore("This test fails; infinite loop ahead.")]
  public void RegisterState_MsDi_EventAggregatorOnly_SuccessTest()
  {
    // Build DI
    var services = new ServiceCollection()
      //// Register Services
      .AddLogging(b => b.AddSimpleConsole())
      .AddSingleton<IEventAggregator, EventAggregator>()
      //// Register States
      .AddTransient<FetchState>()
      .AddTransient<WaitForMessageState>()
      .AddTransient<WorkflowParent>()
      .AddTransient<DoneState>()
      .AddTransient<ErrorState>()
      .BuildServiceProvider();

    // Factory uses DI to construct states
    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);
    var aggregator = services.GetRequiredService<IEventAggregator>();

    var machine = new StateMachine<StateId>(factory, aggregator) { DefaultCommandTimeoutMs = 3000 };

    machine.RegisterState<WorkflowParent>(
      StateId.WorkflowParent,
      onSuccess: StateId.Done,
      onError: StateId.Error,
      subStates: (sub) =>
    {
      sub.RegisterState<FetchState>(StateId.Fetch, onSuccess: StateId.WaitForMessage);
      sub.RegisterState<WaitForMessageState>(StateId.WaitForMessage);
      sub.SetInitial(StateId.Fetch);
    });

    machine.RegisterState<DoneState>(StateId.Done);
    machine.RegisterState<ErrorState>(StateId.Error);
    machine.SetInitial(StateId.WorkflowParent);

    ////var run = machine.Start(StateId.Fetch, parameter: "msdi", CancellationToken.None);
    machine.Start();

    // Drive command state
    Task.Delay(500);
    aggregator.Publish("go");

    Console.WriteLine("MS.DI workflow finished.");
  }

  /*

  #region MS-DI States

  private sealed class DoneState : BaseState<StateId>
  {
    public override void OnEnter(Context<StateId> ctx)
    {
      Console.WriteLine("[Done] OnEnter");
      ctx.NextState(Result.Ok);
    }
  }

  private sealed class ErrorState : BaseState<StateId>
  {
    public override void OnEnter(Context<StateId> ctx)
    {
      Console.WriteLine("[Error] OnEnter");
      ctx.NextState(Result.Ok);
    }
  }

  // States with parameterless constructors
  private sealed class FetchState : BaseState<StateId>
  {
    public override void OnEnter(Context<StateId> ctx)
    {
      Console.WriteLine("[Fetch] OnEnter");
      ctx.NextState(Result.Ok);
    }

    public override void OnEntering(Context<StateId> ctx) =>
      Console.WriteLine("[Fetch] OnEntering");

    public override void OnExit(Context<StateId> ctx) =>
      Console.WriteLine("[Fetch] OnExit");
  }

  private sealed class WaitForMessageState : CommandState<StateId>
  {
    ////public int? TimeoutMs => null; // use default 3000ms

    public override void OnEnter(Context<StateId> ctx) =>
      Console.WriteLine("[Wait] OnEnter");

    public override void OnEntering(Context<StateId> ctx) =>
      Console.WriteLine("[Wait] OnEntering");

    public override void OnExit(Context<StateId> ctx) =>
      Console.WriteLine("[Wait] OnExit");

    public override void OnMessage(Context<StateId> ctx, object message)
    {
      Console.WriteLine($"[Wait] OnMessage: {message}");
      if (message is string s && s.Equals("go", StringComparison.OrdinalIgnoreCase))
        ctx.NextState(Result.Ok);
      else
        ctx.NextState(Result.Error);
    }

    public override void OnTimeout(Context<StateId> ctx)
    {
      Console.WriteLine("[Wait] OnTimeout");
      ctx.NextState(Result.Failure);
    }
  }

  private sealed class WorkflowParent : CompositeState<StateId>
  {
    public override void OnEnter(Context<StateId> ctx) =>
      Console.WriteLine("[Parent] OnEnter");

    public override void OnEntering(Context<StateId> ctx) =>
      Console.WriteLine("[Parent] OnEntering");

    public override void OnExit(Context<StateId> ctx)
    {
      Console.WriteLine("[Parent] OnExit -> Ok");
      ctx.NextState(Result.Ok);
    }
  }

  #endregion MS-DI States

  */
}
