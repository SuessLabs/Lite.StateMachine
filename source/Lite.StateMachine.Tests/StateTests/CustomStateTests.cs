// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData;
using Lite.StateMachine.Tests.TestData.States.CustomBasicStates;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class CustomStateTests
{
  public TestContext TestContext { get; set; }

  [TestMethod]
  public async Task BasicState_Overrides_OnSuccess_SuccessAsync()
  {
    // Assemble
    var counter = 0;
    var ctxProperties = new PropertyBag() { { ParameterType.Counter, counter } };

    var machine = new StateMachine<CustomBasicStateId>();
    machine.RegisterState<State1>(CustomBasicStateId.State1, CustomBasicStateId.State2_Dummy);
    machine.RegisterState<State2Dummy>(CustomBasicStateId.State2_Dummy, CustomBasicStateId.State3);
    machine.RegisterState<State2SuccessA>(CustomBasicStateId.State2_SuccessA, CustomBasicStateId.State3);
    machine.RegisterState<State3>(CustomBasicStateId.State3);

    // Act - Start your engine!
    await machine.RunAsync(CustomBasicStateId.State1, ctxProperties, cancellationToken: TestContext.CancellationToken);

    // Assert Results
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    /*
    // Ensure all states are registered
    var enums = Enum.GetValues<CustomBasicStateId>()
                    .Cast<CustomBasicStateId>();

    Assert.AreEqual(enums.Count(), machine.States.Count());
    Assert.IsTrue(enums.All(k => machine.States.Contains(k)));

    // Ensure they're registered in order
    Assert.IsTrue(enums.SequenceEqual(machine.States), "States should be registered for execution in the same order as the defined enums, StateId 1 => 2 => 3.");
    */
  }

  [TestMethod]
  [Ignore]
  public void BasicState_Overrides_OnSuccessOnError_OnFailure_SuccessAsync()
  {
  }
}
