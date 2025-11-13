// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteState.Mk2;

namespace Sample.Mk2Composite;

internal class Program
{
  static async Task Main(string[] args)
  {
    var fsm = new AsyncStateMachine();

    var rootState = new CompositeState(StateId.Root)
    {
      OnEnter = async ctx => Console.WriteLine("Root (OnEnter)."),
      InitialSubState = StateId.Loading
    };

    var loadingState = new StateDefinition(StateId.Loading)
    {
      OnEnter = async ctx => Console.WriteLine("Loading (OnEnter)"),
      OnTimeout = async ctx => Console.WriteLine("Loading (OnTimeout)."),
      OnExit = async ctx => Console.WriteLine("Loading (OnExit).")
    };

    var processingState = new CompositeState(StateId.Processing)
    {
      OnEnter = async ctx => Console.WriteLine("-- Processing (OnEnter)."),
      InitialSubState = StateId.SubProcessing
    };

    var subProcessingState = new StateDefinition(StateId.SubProcessing)
    {
      OnEnter = async ctx => Console.WriteLine("--[ Sub-processing (OnEnter)"),
      OnMessage = async (msg, ctx) => Console.WriteLine($"--[ SubProcessing (OnMessage): {msg}")
    };

    processingState.AddSubState(subProcessingState);
    rootState.AddSubState(loadingState);
    rootState.AddSubState(processingState);

    fsm.AddState(rootState);
    fsm.AddState(processingState);

    var context = new Context();

    await fsm.TransitionToAsync(StateId.Root, context);
    await Task.Delay(2000);
    await fsm.TransitionToAsync(StateId.Processing, context);
    await fsm.SendMessageAsync("Hello from FSM!", context);
  }
}
