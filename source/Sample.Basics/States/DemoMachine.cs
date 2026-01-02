// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lite.StateMachine;
using Sample.Basics.Models;

namespace Sample.Basics.States;

public enum BasicStateId
{
  State1,
  State2,
  State3,
}

public static class DemoMachine
{
  public static async Task RunAsync()
  {
    var counter = 0;
    var ctxProperties = new PropertyBag() { { ParameterType.Counter, counter } };

    var machine = new StateMachine<BasicStateId>();
    machine.RegisterState<BasicState1>(BasicStateId.State1, BasicStateId.State2);
    machine.RegisterState<BasicState2>(BasicStateId.State2, BasicStateId.State3);
    machine.RegisterState<BasicState3>(BasicStateId.State3);

    // Act - Start your engine!
    await machine.RunAsync(BasicStateId.State1, ctxProperties);
  }
}
