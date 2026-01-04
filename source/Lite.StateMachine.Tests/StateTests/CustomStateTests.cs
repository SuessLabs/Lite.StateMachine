// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData;
using Lite.StateMachine.Tests.TestData.Services;
using Lite.StateMachine.Tests.TestData.States.CustomBasicStates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class CustomStateTests : TestBase
{
  public TestContext TestContext { get; set; }

  [TestMethod]
  public async Task BasicState_Override_Executes_SuccessAsync()
  {
    // Assemble with Dependency Injection
    var services = new ServiceCollection()
      //// Register Services
      .AddLogging(InlineTraceLogger(LogLevel.None))
      .AddSingleton<IMessageService, MessageService>()
      .BuildServiceProvider();

    var msgService = services.GetRequiredService<IMessageService>();
    Func<Type, object?> factory = t => ActivatorUtilities.CreateInstance(services, t);

    var ctxProperties = new PropertyBag() { { ParameterType.Counter, 0 } };

    var machine = new StateMachine<CustomBasicStateId>(factory, null, isContextPersistent: true);
    machine.RegisterState<State1>(CustomBasicStateId.State1, CustomBasicStateId.State2_Dummy);
    machine.RegisterState<State2Dummy>(CustomBasicStateId.State2_Dummy, CustomBasicStateId.State3);
    machine.RegisterState<State2SuccessA>(CustomBasicStateId.State2_Success, CustomBasicStateId.State3);
    machine.RegisterState<State3>(CustomBasicStateId.State3);

    // Act - Start your engine!
    await machine.RunAsync(CustomBasicStateId.State1, ctxProperties, cancellationToken: TestContext.CancellationToken);

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    Assert.AreEqual(1, msgService.Counter1);
    Assert.AreEqual(0, msgService.Counter2, "State2Dummy should never enter");
    Assert.AreEqual(1, msgService.Counter3);
  }

  [TestMethod]
  public async Task BasicState_Override_SkipsState3_SuccessAsync()
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
      { ParameterType.TestExitEarly, true },
    };

    var machine = new StateMachine<CustomBasicStateId>(factory, null, isContextPersistent: true);
    machine.RegisterState<State1>(CustomBasicStateId.State1, CustomBasicStateId.State2_Dummy);
    machine.RegisterState<State2Dummy>(CustomBasicStateId.State2_Dummy, CustomBasicStateId.State3);
    machine.RegisterState<State2SuccessA>(CustomBasicStateId.State2_Success, CustomBasicStateId.State3);
    machine.RegisterState<State3>(CustomBasicStateId.State3);

    // Act - Start your engine!
    await machine.RunAsync(CustomBasicStateId.State1, ctxProperties, cancellationToken: TestContext.CancellationToken);

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    Assert.AreEqual(1, msgService.Counter1);
    Assert.AreEqual(0, msgService.Counter2, "State2Dummy should never enter");
    Assert.AreEqual(0, msgService.Counter3, "State3 should never enter ");
  }

  [TestMethod]
  [Ignore]
  public void BasicState_Overrides_OnSuccessOnError_OnFailure_SuccessAsync()
  {
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

    var machine = new StateMachine<CustomBasicStateId>(factory, null, isContextPersistent: true);
    machine.RegisterState<State1>(CustomBasicStateId.State1, CustomBasicStateId.State2_Dummy);
    machine.RegisterState<State2Dummy>(CustomBasicStateId.State2_Dummy, CustomBasicStateId.State3);
    machine.RegisterState<State2SuccessA>(CustomBasicStateId.State2_Success, CustomBasicStateId.State3);
    machine.RegisterState<State3>(CustomBasicStateId.State3);

    // Act - Start your engine!
    await Assert.ThrowsExactlyAsync<UnregisteredStateTransitionException>(()
      => machine.RunAsync(CustomBasicStateId.State1, ctxProperties, null, TestContext.CancellationToken));

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    Assert.AreEqual(0, msgService.Counter1);
    Assert.AreEqual(0, msgService.Counter2, "State2Dummy should never enter");
  }
}
