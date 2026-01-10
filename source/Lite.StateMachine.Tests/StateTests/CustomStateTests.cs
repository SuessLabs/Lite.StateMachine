// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData;
using Lite.StateMachine.Tests.TestData.Services;
using Lite.StateMachine.Tests.TestData.States.CustomStates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class CustomStateTests : TestBase
{
  [TestMethod]
  [DataRow(false, DisplayName = "Don't skip State3")]
  [DataRow(true, DisplayName = "Skip State3")]
  public async Task BasicState_Override_Executes_SuccessAsync(bool skipState3)
  {
    // Assemble with Dependency Injection
    var services = new ServiceCollection()
      //// Register Services
      .AddLogging(InlineTraceLogger(LogLevel.None))
      .AddSingleton<IMessageService, MessageService>()
      .BuildServiceProvider();

    var msgService = services.GetRequiredService<IMessageService>();
    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);

    var ctxProperties = new PropertyBag()
    {
      { ParameterType.Counter, 0 },
      { ParameterType.TestExitEarly, skipState3 },
    };

    var machine = new StateMachine<CustomStateId>(factory, null, isContextPersistent: true);
    machine.RegisterState<State1>(CustomStateId.State1, CustomStateId.State2_Dummy);
    machine.RegisterState<State2Dummy>(CustomStateId.State2_Dummy, CustomStateId.State3);
    machine.RegisterState<State2Success>(CustomStateId.State2_Success, CustomStateId.State3);
    machine.RegisterState<State3>(CustomStateId.State3);

    // Act - Start your engine!
    await machine.RunAsync(CustomStateId.State1, ctxProperties, cancellationToken: TestContext.CancellationToken);

    // Assert Results
    AssertMachineNotNull(machine);

    Assert.AreEqual(1, msgService.Counter1);
    Assert.AreEqual(0, msgService.Counter2, "State2Dummy should never enter");
    Assert.AreEqual(skipState3 ? 0 : 1, msgService.Counter3);
  }

  [TestMethod]
  public async Task BasicState_Overrides_ThrowUnregisteredException_Async()
  {
    // Assemble with Dependency Injection
    var services = new ServiceCollection()
      //// Register Services
      .AddLogging(InlineTraceLogger(LogLevel.None))
      .AddSingleton<IMessageService, MessageService>()
      .BuildServiceProvider();

    var msgService = services.GetRequiredService<IMessageService>();
    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);

    var ctxProperties = new PropertyBag()
    {
      { ParameterType.Counter, 0 },
      { ParameterType.TestUnregisteredTransition, true },
    };

    var machine = new StateMachine<CustomStateId>(factory, null, isContextPersistent: true);
    machine.RegisterState<State1>(CustomStateId.State1, CustomStateId.State2_Dummy);
    machine.RegisterState<State2Dummy>(CustomStateId.State2_Dummy, CustomStateId.State3);
    machine.RegisterState<State2Success>(CustomStateId.State2_Success, CustomStateId.State3);
    machine.RegisterState<State3>(CustomStateId.State3);

    // Act - Start your engine!
    await Assert.ThrowsExactlyAsync<UnregisteredStateTransitionException>(()
      => machine.RunAsync(CustomStateId.State1, ctxProperties, null, TestContext.CancellationToken));

    // Assert Results
    AssertMachineNotNull(machine);

    Assert.AreEqual(0, msgService.Counter1);
    Assert.AreEqual(0, msgService.Counter2, "State2Dummy should never enter");
  }

  [TestMethod]
  [DataRow(false, DisplayName = "Run State2_Sub3")]
  [DataRow(true, DisplayName = "Skip State2_Sub2")]
  public async Task Composite_Override_Executes_SuccessAsync(bool skipSubState2)
  {
    // Assemble with Dependency Injection
    var services = new ServiceCollection()
      //// Register Services
      .AddLogging(InlineTraceLogger(LogLevel.Trace))
      .AddSingleton<IMessageService, MessageService>()
      .BuildServiceProvider();

    var msgService = services.GetRequiredService<IMessageService>();
    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);

    var ctxProperties = new PropertyBag()
    {
      { ParameterType.Counter, 0 },
      { ParameterType.TestExitEarly2, skipSubState2 },
    };

    var machine = new StateMachine<CustomStateId>(factory, null, isContextPersistent: true);

    machine.RegisterState<State1>(CustomStateId.State1, CustomStateId.State2_Dummy);
    machine.RegisterState<State2Dummy>(CustomStateId.State2_Dummy, CustomStateId.State3);
    machine.RegisterComposite<State2Success>(CustomStateId.State2_Success, CustomStateId.State2_Sub1, CustomStateId.State3);
    machine.RegisterSubState<State2Success_Sub1>(CustomStateId.State2_Sub1, CustomStateId.State2_Success, CustomStateId.State2_Sub2);
    machine.RegisterSubState<State2Success_Sub2>(CustomStateId.State2_Sub2, CustomStateId.State2_Success, CustomStateId.State2_Sub3);
    machine.RegisterSubState<State2Success_Sub3>(CustomStateId.State2_Sub3, CustomStateId.State2_Success);
    machine.RegisterState<State3>(CustomStateId.State3);

    // Act - Start your engine!
    await machine.RunAsync(CustomStateId.State1, ctxProperties, cancellationToken: TestContext.CancellationToken);

    // Assert Results
    AssertMachineNotNull(machine);

    Assert.AreEqual(skipSubState2 ? 3 : 4, msgService.Counter1, "State Counter1 failed.");
    Assert.AreEqual(0, msgService.Counter2, "State2Dummy should never enter");
    Assert.AreEqual(1, msgService.Counter3, "Skip Substate Counter3 failed");
  }
}
