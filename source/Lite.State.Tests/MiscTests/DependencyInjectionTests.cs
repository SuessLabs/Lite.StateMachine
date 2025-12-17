using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lite.State.Tests;

[TestClass]
public class DependencyInjectionTests
{
  /// <summary>State definitions.</summary>
  public enum StateId
  {
    State1,
    State2,
    State2e,
    State2f,
    State3,
    State4,
    State4_Sub1,
    State4_Sub2,
    State5,
  }

  [TestMethod]
  public void Generates_BasicState_RegisterStateEx_SuccessTest()
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

    var machine = new StateMachine<StateId>()
      .RegisterStateEx(new StateEx1(StateId.State1), StateId.State2)
      .RegisterStateEx(new StateEx2(StateId.State2), StateId.State3)
      .RegisterStateEx(new StateEx3(StateId.State3))
      .SetInitialEx(StateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml(includeSubmachines: true);

    machine.Start();

    // Assert
    Assert.IsNotNull(uml);
  }

  #region Services

  private interface IMessageService
  {
    int GetNumber();

    void SetNumber(int number);

  }

  private class MessageService : IMessageService
  {
    private int _cache;

    public int GetNumber() => _cache;

    public void SetNumber(int number) => _cache = number;
  }

  #endregion Services

  #region State Machine Ex

  private class StateEx1(StateId id) : BaseState<StateId>(id);

  private class StateEx2(StateId id) : BaseState<StateId>(id);

  /// <summary>Error state tests.</summary>
  /// <param name="id">StateId</param>
  private class StateEx2e(StateId id) : BaseState<StateId>(id);

  /// <summary>Failure state tests.</summary>
  /// <param name="id">StateId</param>
  private class StateEx2f(StateId id) : BaseState<StateId>(id);

  private class StateEx3(StateId id) : BaseState<StateId>(id);

  // Composite state tests

  private class StateEx4(StateId id) : CompositeState<StateId>(id);

  private class StateEx4_Sub1(StateId id) : BaseState<StateId>(id);

  private class StateEx4_Sub2(StateId id) : BaseState<StateId>(id);

  private class StateEx5(StateId id) : BaseState<StateId>(id);

  #endregion State Machine Ex
}
