// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData;
using Lite.StateMachine.Tests.TestData.Models;
using Lite.StateMachine.Tests.TestData.Services;
using Lite.StateMachine.Tests.TestData.States.CommandL3DiStates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class CommandStateTests : TestBase
{
  [TestMethod]
  public async Task BasicState_Override_Executes_SuccessAsync()
  {
    // Assemble with Dependency Injection
    var services = new ServiceCollection()
      .AddLogging(InlineTraceLogger(LogLevel.Trace))
      .AddSingleton<IMessageService, MessageService>()
      .AddSingleton<IEventAggregator, EventAggregator>()
      .BuildServiceProvider();

    var msgService = services.GetRequiredService<IMessageService>();
    var events = services.GetRequiredService<IEventAggregator>();
    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);

    var ctxProperties = new PropertyBag()
    {
      { ParameterType.Counter, 0 },
    };

    var machine = new StateMachine<StateId>(factory, events)
    {
      // Make sure we don't get stuck.
      // And send some message after leaving Command state
      // to make sure we unsubscribed successfully.
      DefaultStateTimeoutMs = 3000,
      IsContextPersistent = true,
    };

    machine
      .AddContext(ctxProperties)
      .RegisterState<State1>(StateId.State1, StateId.State2, subscriptionTypes: [typeof(UnlockResponse)])
      .RegisterComposite<State2>(StateId.State2, initialChildStateId: StateId.State2_Sub1, onSuccess: StateId.State3)
      .RegisterSubState<State2_Sub1>(StateId.State2_Sub1, parentStateId: StateId.State2, onSuccess: StateId.State2_Sub2)
      .RegisterSubComposite<State2_Sub2>(StateId.State2_Sub2, parentStateId: StateId.State2, initialChildStateId: StateId.State2_Sub2_Sub1, onSuccess: StateId.State2_Sub3)
      .RegisterSubState<State2_Sub2_Sub1>(StateId.State2_Sub2_Sub1, parentStateId: StateId.State2_Sub2, onSuccess: StateId.State2_Sub2_Sub2, subscriptionTypes: [typeof(UnlockResponse), typeof(CloseResponse)])
      .RegisterSubState<State2_Sub2_Sub2>(StateId.State2_Sub2_Sub2, parentStateId: StateId.State2_Sub2, onSuccess: StateId.State2_Sub2_Sub3)
      .RegisterSubState<State2_Sub2_Sub3>(StateId.State2_Sub2_Sub3, parentStateId: StateId.State2_Sub2, onSuccess: null)
      .RegisterSubState<State2_Sub3>(StateId.State2_Sub3, parentStateId: StateId.State2, onSuccess: null)
      .RegisterState<State3>(StateId.State3, onSuccess: null);

    events.Subscribe(msg =>
    {
      if (msg is ICustomCommand)
      {
        if (msg is UnlockCommand cmd)
        {
          // +100 check so we don't trigger this a 2nd time.
          if (cmd.Counter > 100 && cmd.Counter < 200)
            return;

          // NOTE:
          //  First we purposely publish 'OpenCommand' to prove that our OnMessage
          //  filters out the bad message, followed by publishing the REAL message.
          if (cmd.Counter < 200)
            events.Publish(new UnlockCommand { Counter = cmd.Counter + 100 });

          events.Publish(new UnlockResponse { Counter = cmd.Counter + 100 });

          // NOTE: This doesn't reach State2_Sub2_Sub2 because it already left (GOOD)
          events.Publish(new CloseResponse { Counter = cmd.Counter + 100 });
        }
      }
    });

    // Act - Start your engine!
    await machine.RunAsync(StateId.State1, TestContext.CancellationToken);

    // Assert Results
    AssertMachineNotNull(machine);

    Assert.AreEqual(29, msgService.Counter1);
    Assert.AreEqual(13, msgService.Counter2, "State2 Context.Param Count");
    Assert.AreEqual(12, msgService.Counter3);
    Assert.AreEqual(2, msgService.Counter4);
  }

  [TestMethod]
  public async Task CancelsInfiniteStateMachineTestAsync()
  {
    // Assemble with Dependency Injection
    var services = new ServiceCollection()
      .AddLogging(InlineTraceLogger(LogLevel.Trace))
      .AddSingleton<IMessageService, MessageService>()
      .AddSingleton<IEventAggregator, EventAggregator>()
      .BuildServiceProvider();

    var msgService = services.GetRequiredService<IMessageService>();
    var events = services.GetRequiredService<IEventAggregator>();

    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);

    var machine = new StateMachine<StateId>(factory, events)
      .RegisterState<InfState1>(StateId.State1, StateId.State2)
      .RegisterState<InfState2>(StateId.State2, StateId.State1);

    var cts = new CancellationTokenSource();

    var counter = 0;
    events.Subscribe(msg =>
    {
      // All messages get received, even `CancelResponse`
      if (msg is not CancelCommand cmd)
        return;

      counter++;
      if (counter >= 100)
      {
        // Get outta here!!
        cts.Cancel();
      }

      // Don't let it hang waiting for a response
      events.Publish(new CancelResponse());
    });

    var result = await machine.RunAsync(StateId.State1, cts.Token);

    // Assert
    Assert.IsNotNull(result);
    AssertMachineNotNull(machine);
    Assert.AreEqual(100, counter);
  }

#pragma warning disable SA1124 // Do not use regions
  #region Infinite Loop Test State Classes

  private class InfState1 : IState<StateId>
#pragma warning restore SA1124 // Do not use regions
  {
    public Task OnEnter(Context<StateId> context)
    {
      context.NextState(Result.Success);
      return Task.CompletedTask;
    }

    public Task OnEntering(Context<StateId> context) => Task.CompletedTask;

    public Task OnExit(Context<StateId> context) => Task.CompletedTask;
  }

  private class InfState2 : ICommandState<StateId>
  {
    public IReadOnlyCollection<Type> SubscribedMessageTypes =>
    [
      typeof(CancelResponse),
    ];

    public Task OnEnter(Context<StateId> context)
    {
      context.EventAggregator?.Publish(new CancelCommand());
      return Task.CompletedTask;
    }

    public Task OnEntering(Context<StateId> context) => Task.CompletedTask;

    public Task OnExit(Context<StateId> context) => Task.CompletedTask;

    public Task OnMessage(Context<StateId> context, object message)
    {
      context.NextState(Result.Success);
      return Task.CompletedTask;
    }

    public Task OnTimeout(Context<StateId> context) => Task.CompletedTask;
  }

  #endregion Infinite Loop Test State Classes
}
