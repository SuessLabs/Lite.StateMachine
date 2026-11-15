// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Sample10.UsingEventIPC;
using IpcEventAggregator = Lite.EventIpc.EventAggregator;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class EventIpcAdapterTests
{
  [TestMethod]
  public void RuntimeTypesWildcardsAndDisposalTest()
  {
    var events = new IpcEventAggregator();
    var adapter = new EventIpcAdapter(events)
      .RegisterMessageType<string>()
      .RegisterMessageType<int>();

    IEventAggregator machineEvents = adapter;
    var typedCount = 0;
    var wildcardCount = 0;
    var nativeCount = 0;

    Action<string> nativeHandler = _ => nativeCount++;
    events.Subscribe(nativeHandler);

    var typed = adapter.Subscribe(_ => typedCount++, typeof(string), typeof(string));
    var wildcard = adapter.Subscribe(_ => wildcardCount++);

    // Subscriptions must retain EventIPC's weak delegates while active.
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    // Both the object and generic contract must select the runtime string type.
    machineEvents.Publish((object)"first");
    machineEvents.Publish("second");
    events.Publish(42);
    Assert.AreEqual(2, typedCount);
    Assert.AreEqual(3, wildcardCount);
    Assert.AreEqual(2, nativeCount);

    typed.Dispose();
    typed.Dispose();
    events.Publish("third");
    Assert.AreEqual(2, typedCount);
    Assert.AreEqual(4, wildcardCount);

    wildcard.Dispose();
    events.Publish("fourth");
    Assert.AreEqual(4, wildcardCount);
    events.Unsubscribe(nativeHandler);
  }

  [TestMethod]
  public async Task OnMessageAwaitsResponseBeforeTransitionAsync()
  {
    var events = new IpcEventAggregator();
    var adapter = new EventIpcAdapter(events).RegisterMessageType<StartWork>();
    var response = new TaskCompletionSource<WorkResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
    var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    Func<WorkRequest, Task<WorkResponse>> worker = request =>
    {
      requested.SetResult();
      return response.Task;
    };

    events.SubscribeRequest(worker);

    var machine = new StateMachine<StateId>(
      containerFactory: type => type == typeof(WaitingState)
        ? new WaitingState(events)
        : Activator.CreateInstance(type),
      eventAggregator: adapter)
      .RegisterState<PreparingState>(StateId.Preparing, onSuccess: StateId.Waiting)
      .RegisterState<WaitingState>(StateId.Waiting, onSuccess: StateId.Completed)
      .RegisterState<CompletedState>(StateId.Completed);

    using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    try
    {
      var running = machine.RunAsync(StateId.Preparing, cancellation.Token);
      await requested.Task.WaitAsync(cancellation.Token);
      Assert.IsFalse(running.IsCompleted);
      Assert.AreEqual(StateId.Waiting, machine.Context.CurrentStateId);

      response.SetResult(new WorkResponse("Worker finished"));
      await running.WaitAsync(cancellation.Token);
      Assert.AreEqual(StateId.Completed, machine.Context.CurrentStateId);
      Assert.AreEqual("Worker finished", machine.Context.Parameters["Response"]);
    }
    finally
    {
      events.UnsubscribeRequest(worker);
    }
  }
}
