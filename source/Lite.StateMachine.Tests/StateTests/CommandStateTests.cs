// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
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
  [DataRow(false, DisplayName = "Don't skip State3")]
  [DataRow(true, DisplayName = "Skip State3")]
  public async Task BasicState_Override_Executes_SuccessAsync(bool skipState3)
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
      { ParameterType.TestExitEarly, skipState3 },
    };

    var machine = new StateMachine<CompositeL3>(factory, events)
    {
      // Make sure we don't get stuck.
      // And send some message after leaving Command state
      // to make sure we unsubscribed successfully.
      DefaultStateTimeoutMs = 3000,
    };

    machine
      .RegisterState<State1>(CompositeL3.State1, CompositeL3.State2)
      .RegisterComposite<State2>(CompositeL3.State2, initialChildStateId: CompositeL3.State2_Sub1, onSuccess: CompositeL3.State3)
      .RegisterSubState<State2_Sub1>(CompositeL3.State2_Sub1, parentStateId: CompositeL3.State2, onSuccess: CompositeL3.State2_Sub2)
      .RegisterSubComposite<State2_Sub2>(CompositeL3.State2_Sub2, parentStateId: CompositeL3.State2, initialChildStateId: CompositeL3.State2_Sub2_Sub1, onSuccess: CompositeL3.State2_Sub3)
      .RegisterSubState<State2_Sub2_Sub1>(CompositeL3.State2_Sub2_Sub1, parentStateId: CompositeL3.State2_Sub2, onSuccess: CompositeL3.State2_Sub2_Sub2)
      .RegisterSubState<State2_Sub2_Sub2>(CompositeL3.State2_Sub2_Sub2, parentStateId: CompositeL3.State2_Sub2, onSuccess: CompositeL3.State2_Sub2_Sub3)
      .RegisterSubState<State2_Sub2_Sub3>(CompositeL3.State2_Sub2_Sub3, parentStateId: CompositeL3.State2_Sub2, onSuccess: null)
      .RegisterSubState<State2_Sub3>(CompositeL3.State2_Sub3, parentStateId: CompositeL3.State2, onSuccess: null)
      .RegisterState<State3>(CompositeL3.State3, onSuccess: null);

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

          events.Publish(new OpenResponse { Counter = cmd.Counter + 100 });

          // NOTE: This doesn't reach State2_Sub2_Sub2 because it already left (GOOD)
          events.Publish(new CloseResponse { Counter = cmd.Counter + 100 });
        }
      }
    });

    // Act - Start your engine!
    await machine.RunAsync(CompositeL3.State1, ctxProperties, null, TestContext.CancellationToken);

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    Assert.AreEqual(27, msgService.Counter1);
    Assert.AreEqual(14, msgService.Counter2, "State2 Context.Param Count");
    Assert.AreEqual(skipState3 ? 13 : 13, msgService.Counter3);
  }
}
