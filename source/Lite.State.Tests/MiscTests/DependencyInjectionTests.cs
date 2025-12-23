// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using Lite.State.Tests.TestData;
using Lite.State.Tests.TestData.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lite.State.Tests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Allowed for this test class")]
public class DependencyInjectionTests
{
  private const string ExpectedDotGraph = """
    digraph StateMachine
    {
      rankdir=LR;
      compound=true;
      node [fontname="Segoe UI", fontsize=10];
      edge [fontname="Segoe UI", fontsize=10];
      start [shape=point];
      start -> "State1";
    "State1" [shape=box];
    "State2" [shape=box];
    "State3" [shape=doublecircle];
    "State1" -> "State2" [label="Ok"];
    "State2" -> "State3" [label="Ok"];
    }
    """;

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
      .RegisterState(GenericStateId.State1, () => new StateGenerics1(), GenericStateId.State2)
      .RegisterState(GenericStateId.State2, () => new StateGenerics2(), GenericStateId.State3)
      .RegisterState(GenericStateId.State3, () => new StateGenerics3())
      .SetInitialEx(GenericStateId.State1);

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
      .RegisterState<StateGenerics1>(GenericStateId.State1, GenericStateId.State2)
      .RegisterState<StateGenerics2>(GenericStateId.State2, GenericStateId.State3)
      .RegisterState<StateGenerics3>(GenericStateId.State3)
      .SetInitialEx(GenericStateId.State1);

    // Act - Generate UML
    var uml = machine.ExportUml();

    machine.Start();

    // Assert
    Assert.IsNotNull(uml);
    Console.WriteLine(uml);
  }
}
