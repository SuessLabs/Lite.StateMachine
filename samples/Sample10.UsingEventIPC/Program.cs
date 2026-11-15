// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Lite.StateMachine;
using IpcEventAggregator = Lite.EventIpc.EventAggregator;

namespace Sample10.UsingEventIPC;

internal class Program
{
  private static async Task Main()
  {
    var events = new IpcEventAggregator();
    var adapter = new EventIpcAdapter(events)
      .RegisterMessageType<StartWork>();

    // Retain this delegate until the machine finishes (EventIPC uses weak references).
    Func<WorkRequest, Task<WorkResponse>> worker = async request =>
    {
      Console.WriteLine($"Worker: processing '{request.Text}'...");
      await Task.Delay(100);
      return new WorkResponse(request.Text.ToUpperInvariant());
    };

    events.SubscribeRequest(worker);

    try
    {
      var machine = new StateMachine<StateId>(
        containerFactory: type => (type == typeof(WaitingState)) ? new WaitingState(events) : Activator.CreateInstance(type),
        eventAggregator: adapter)
        .RegisterState<PreparingState>(StateId.Preparing, onSuccess: StateId.Waiting)
        .RegisterState<WaitingState>(StateId.Waiting, onSuccess: StateId.Completed)
        .RegisterState<CompletedState>(StateId.Completed);

      using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
      await machine.RunAsync(StateId.Preparing, cancellation.Token);

      // RunAsync can also return after cancellation; require actual completion.
      if (machine.Context.CurrentStateId != StateId.Completed)
        throw new InvalidOperationException("The sample did not reach the completed state.");
    }
    finally
    {
      events.UnsubscribeRequest(worker);
    }
  }
}
