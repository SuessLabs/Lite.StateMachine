// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lite.State.Tests.TestData;
using Lite.State.Tests.TestData.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lite.State.Tests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Allowed for this test class")]
public class DependencyInjectionTests
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

  /*
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

    var machine = new StateMachine<GenericStateId>()
      .RegisterState(GenericStateId.State1, () => new StateDi1(), GenericStateId.State2)
      .RegisterState(GenericStateId.State2, () => new StateDi2(), GenericStateId.State3)
      .RegisterState(GenericStateId.State3, () => new StateDi3())
      .SetInitial(GenericStateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml();

    machine.Start();

    // Assert
    Assert.IsNotNull(uml);
    Console.WriteLine(uml);
  }
  */

  [TestMethod]
  public void DI_GenericsRegistration_SuccessTest()
  {
    // Assemble
    var services = new ServiceCollection()
      .AddLogging(builder =>
      {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Trace);
      })
      .AddSingleton<IMessageService, MessageService>();
    services.AddTransient<StateDi1>();

    ////using var provider = services.BuildServiceProvider();

    var machine = new StateMachine<GenericStateId>()
      .RegisterState<StateDi1>(GenericStateId.State1, GenericStateId.State2)
      .RegisterState<StateDi2>(GenericStateId.State2, GenericStateId.State3)
      .RegisterState<StateDi3>(GenericStateId.State3)
      .SetInitial(GenericStateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml();

    machine.Start();

    // Assert
    Assert.IsNotNull(uml);
    Console.WriteLine(uml);
  }

  /*
  [TestMethod]
  public void RegisterState_GenericsWithMsDi_SuccessTest()
  {
    // With Microsoft Dependency Injection
    var services = new ServiceCollection()
      .AddLogging(b => b.AddSimpleConsole(o =>
      {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss ";
      }))
      .AddSingleton<IEventAggregator, EventAggregator>()
      .BuildServiceProvider();

    // DI Factory Resolver:
    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);
    var aggregator = services.GetRequiredService<IEventAggregator>();

    // -----------------------------
    // OLD-4d3 (DI Resolver Helpers):
    // NOTE: Using resolver helpers MAY be the way to go
    ////var resolver = new MsDiResolver(services);
    ////var aggregator = services.GetRequiredService<IEventAggregator>();
    ////var machine = new StateMachine<StateId>(resolver);
    //// END OLD --------------------

    // Assemble
    // NON-DI: var machine = new StateMachine<StateId>();
    var machine = new StateMachine<StateId>(factory, aggregator) { DefaultTimeoutMs = 3000 };
    machine.RegisterState<State1>(StateId.State1, StateId.State2);
    machine.RegisterState<State2>(StateId.State2, StateId.State3);
    machine.RegisterState<State3>(StateId.State3);

    // Set starting point
    machine.SetInitial(StateId.State1);

    // Act - Start your engine!
    var ctxProperties = new PropertyBag() { { ParameterKeyTest, "not-finished" }, };
    machine.Start(ctxProperties);

    // Assert Results
    var ctxFinalParams = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinalParams);
    Assert.AreEqual(TestValue, ctxFinalParams[ParameterKeyTest]);

    // Ensure all transitions are called
    // NOTE: This should be 9 because each state has 3 hooks that increment the counter
    Assert.AreEqual(9 - 1, ctxFinalParams[ParameterCounter]);

    var enums = Enum.GetValues<StateId>().Cast<StateId>();

    // Ensure all states are hit
    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're in order
    Assert.IsTrue(enums.SequenceEqual(machine.States));
  }
  */

  [TestMethod]
  public void RegisterState_MsDi_SuccessTest()
  {
    // Build DI
    var services = new ServiceCollection()
      .AddLogging(b => b.AddSimpleConsole())
      .AddSingleton<IEventAggregator, EventAggregator>()
      .AddTransient<FetchState>()
      .AddTransient<WaitForMessageState>()
      .AddTransient<WorkflowParent>()
      .AddTransient<DoneState>()
      .AddTransient<ErrorState>()
      .BuildServiceProvider();

    // Factory uses DI to construct states
    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);
    var aggregator = services.GetRequiredService<IEventAggregator>();

    var machine = new StateMachine<StateId>(factory, aggregator) { DefaultTimeoutMs = 3000 };

    ////machine.RegisterComposite<WorkflowParent>(StateId.WorkflowParent, onSuccess: StateId.Done, onError: StateId.Error);
    ////machine.RegisterSubState<FetchState>(StateId.Fetch, StateId.WorkflowParent, onSuccess: StateId.WaitForMessage);
    ////machine.RegisterSubState<WaitForMessageState>(StateId.WaitForMessage, StateId.WorkflowParent, onSuccess: default, onError: default, onFailure: default);

    machine.RegisterState<WorkflowParent>(
      StateId.WorkflowParent,
      onSuccess: StateId.Done,
      onError: StateId.Error,
      subStates: (sub) =>
    {
      sub.RegisterState<FetchState>(StateId.Fetch, onSuccess: StateId.WaitForMessage);
      sub.RegisterState<WaitForMessageState>(StateId.WaitForMessage);
    });

    machine.RegisterState<DoneState>(StateId.Done, onSuccess: default);
    machine.RegisterState<ErrorState>(StateId.Error, onSuccess: default);
    machine.SetInitial(StateId.WorkflowParent);

    ////var run = machine.Start(StateId.Fetch, parameter: "msdi", CancellationToken.None);
    machine.Start();

    // Drive command state
    Task.Delay(500);
    aggregator.Publish("go");

    Console.WriteLine("MS.DI workflow finished."); ;
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

  private sealed class WorkflowParent : BaseState<StateId>
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
