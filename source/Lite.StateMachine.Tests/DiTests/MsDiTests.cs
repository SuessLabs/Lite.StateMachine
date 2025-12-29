// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

/*
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

/// <summary>State definitions.</summary>
private enum StateId
{
  WorkflowParent,
  Fetch,
  WaitForMessage,
  Done,
  Error,
}

[TestMethod]
public void DI_BasicRegistration_SuccessTest()
{
  // Assemble
  var services = new ServiceCollection()
    .AddLogging(builder =>
    {
      builder.AddConsole();
      builder.SetMinimumLevel(LogLevel.Trace);
    })
    .AddSingleton<IMessageService, MessageService>();
  ////.AddTransient<MyClass>();

  using var provider = services.BuildServiceProvider();

  var machine = new StateMachine<FlatStateId>()
    .RegisterState(FlatStateId.State1, () => new StateDi1(), FlatStateId.State2)
    .RegisterState(FlatStateId.State2, () => new StateDi2(), FlatStateId.State3)
    .RegisterState(FlatStateId.State3, () => new StateDi3())
    .SetInitial(FlatStateId.State1);

  // Act - Generate UML
  var uml = machine.ExportUml();

  machine.Start();

  // Assert
  Assert.IsNotNull(uml);
  Console.WriteLine(uml);
}

[TestMethod]
  public void DI_GenericsRegistration_SuccessTest()
  {
    // Assemble
    var services = new ServiceCollection();

    // Register DI Services
    services.AddLogging(builder =>
    {
      builder.AddConsole();
      builder.SetMinimumLevel(LogLevel.Trace);
    })
    .AddSingleton<IMessageService, MessageService>();

    // Register States for DI friendly
    services.AddTransient<StateDi1>();
    services.AddTransient<StateDi2>();
    services.AddTransient<StateDi3>();
    var provider = services.BuildServiceProvider();

    // TODO (2025-12-25 DS): vNext - PoC builder
    ////using var provider = services.BuildServiceProvider();

    // DI Factory Resolver:
    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(provider, t);

    // Uncomment the following to use Event Aggregator for Command States
    ////var aggregator = provider.GetRequiredService<IEventAggregator>();
    ////var machine = new StateMachine<FlatStateId>(factory, aggregator);

    var machine = new StateMachine<FlatStateId>(factory);
    machine
      .RegisterState<StateDi1>(FlatStateId.State1, FlatStateId.State2)
      .RegisterState<StateDi2>(FlatStateId.State2, FlatStateId.State3)
      .RegisterState<StateDi3>(FlatStateId.State3)
      .SetInitial(FlatStateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml();

    machine.Start();

    // Assert
    Assert.IsNotNull(uml);
    Console.WriteLine(uml);

    // Assert Results
    var ctxFinalParams = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinalParams);

    // Ensure all transitions are called
    // NOTE: This should be 9 because each state has 3 hooks that increment the counter
    Assert.AreEqual(9 - 1, ctxFinalParams[ParameterCounter]);

    var enums = Enum.GetValues<FlatStateId>().Cast<FlatStateId>();

    // Ensure all states are registered
    Assert.AreEqual(enums.Count(), machine.States.Count());

    // Ensure all states are registered (order doesn't matter)
    ////Assert.IsTrue(enums.All(k => machine.States.Contains(k)));
    // Ensure all states are in order
    ////Assert.IsTrue(enums.SequenceEqual(machine.States));
  }

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
}
*/
