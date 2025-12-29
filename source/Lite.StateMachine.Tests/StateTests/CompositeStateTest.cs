// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lite.StateMachine.Tests.TestData;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class CompositeStateTest
{
  public const string ParameterSubStateEntered = "SubEntered";
  public const string SUCCESS = "success";

  [TestMethod]
  public async Task BasicTransitions_SuccessTestAsync()
  {
    // Assemble
    var machine = new StateMachine<CompositeL1StateId>();
    machine.DefaultStateTimeoutMs = 5000;

    machine.RegisterState<CompositeL1_State1>(CompositeL1StateId.State1, CompositeL1StateId.State2);

    machine.RegisterComposite<CompositeL1_State2>(CompositeL1StateId.State2, CompositeL1StateId.State2_Sub1, CompositeL1StateId.State3);

    machine.RegisterSubState<CompositeL1_State2_Sub1>(
      stateId: CompositeL1StateId.State2_Sub1,
      parentStateId: CompositeL1StateId.State2,
      onSuccess: CompositeL1StateId.State2_Sub2);

    machine.RegisterSubState<CompositeL1_State2_Sub2>(
      stateId: CompositeL1StateId.State2_Sub2,
      parentStateId: CompositeL1StateId.State2,
      onSuccess: null,
      onError: null,
      onFailure: null);

    machine.RegisterState<CompositeL1_State3>(CompositeL1StateId.State3);

    // Act
    var task = machine.RunAsync(CompositeL1StateId.State1);
    await task;   //// .GetAwaiter().GetResult();

    // Assert
    Assert.IsNotNull(machine);
    Assert.IsNull(machine.Context);

    // vNext:
    ////var ctxFinal = machine.Context.Parameters;
    ////Assert.IsNotNull(ctxFinal);
    ////Assert.AreEqual(SUCCESS, ctxFinal[ParameterSubStateEntered]);
  }

  /*
    [TestMethod]
    public void RegisterStateEx_Fluent_SuccessTest()
    {
      // Assemble
      var machine = new StateMachine<StateId>()
        .RegisterState<StateEx1>(StateId.State1, StateId.State2)
        .RegisterState<StateEx2>(
          StateId.State2,
          onSuccess: StateId.State3,
          subStates: (sub) =>
        {
          sub
            .RegisterState<StateEx2_Sub1>(StateId.State2_Sub1, StateId.State2_Sub2)
            .RegisterState<StateEx2_Sub2>(StateId.State2_Sub2)
            .SetInitial(StateId.State2_Sub1);
        })
        .RegisterState<StateEx3>(StateId.State3)
        .SetInitial(StateId.State1);

      // Act
      machine.Start();

      // Assert
      var ctxFinal = machine.Context.Parameters;
      Assert.IsNotNull(ctxFinal);
      Assert.AreEqual(SUCCESS, ctxFinal[ParameterSubStateEntered]);

      // Ensure all states are hit (-2 because of subs)
      ////var enums = Enum.GetValues<StateId>().Cast<StateId>();
      ////Assert.AreEqual(enums.Count(), machine.States.Count());
      ////Assert.IsTrue(enums.All(k => machine.States.Contains(k)));
      ////
      ////// Ensure they're in order
      ////Assert.IsTrue(enums.SequenceEqual(machine.States));
    }
  */
}
