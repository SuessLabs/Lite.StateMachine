// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.
/*
namespace LiteState.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

[TestClass]
public class LiteStateMachineTests
{
  [TestMethod]
  public async Task Transition_ShouldCallOnEnter()
  {
    var fsm = new LiteStateMachine();
    bool entered = false;

    var state = new StateDefinition(StateId.Loading)
    {
      OnEnter = async ctx => entered = true
    };

    fsm.AddState(state);
    await fsm.TransitionToAsync(StateId.Loading, new Dictionary<string, object>());

    Assert.IsTrue(entered);
  }

  [TestMethod]
  public async Task Message_ShouldInvokeOnMessage()
  {
    var fsm = new LiteStateMachine();
    string received = null;

    var state = new StateDefinition(StateId.Processing)
    {
      OnEnter = async ctx => { },
      OnMessage = async (msg, ctx) => received = msg
    };

    fsm.AddState(state);
    await fsm.TransitionToAsync(StateId.Processing, new Dictionary<string, object>());
    await fsm.SendMessageAsync("TestMessage", new Dictionary<string, object>());

    Assert.AreEqual("TestMessage", received);
  }

  [TestMethod]
  public async Task Exit_ShouldInvokeOnExit()
  {
    var fsm = new LiteStateMachine();
    bool exited = false;

    var state1 = new StateDefinition(StateId.Loading)
    {
      OnExit = async ctx => exited = true
    };

    var state2 = new StateDefinition(StateId.Completed);

    fsm.AddState(state1);
    fsm.AddState(state2);

    await fsm.TransitionToAsync(StateId.Loading, new Dictionary<string, object>());
    await fsm.TransitionToAsync(StateId.Completed, new Dictionary<string, object>());

    Assert.IsTrue(exited);
  }
}
*/
