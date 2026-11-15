// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lite.StateMachine;
using IpcEvents = Lite.EventIpc.IEventAggregator;

namespace Sample10.UsingEventIPC;

internal sealed class WaitingState(IpcEvents events) : ICommandState<StateId>
{
  public IReadOnlyCollection<Type> SubscribedMessageTypes => [typeof(StartWork)];

  // The request's own deadline is shorter than the command state's timeout.
  public int? TimeoutMs => 5000;

  public Task OnEnter(Context<StateId> context)
  {
    Console.WriteLine("Waiting: publishing StartWork through the adapter.");

    // The machine subscribes before OnEnter. No delay is needed for readiness.
    // An external component could equivalently call events.Publish(new StartWork(...)).
    context.EventAggregator!.Publish(new StartWork("hello from Lite.EventIPC"));
    return Task.CompletedTask;
  }

  public Task OnEntering(Context<StateId> context) => Task.CompletedTask;

  public Task OnExit(Context<StateId> context) => Task.CompletedTask;

  public async Task OnMessage(Context<StateId> context, object message)
  {
    if (message is not StartWork start)
      return;

    Console.WriteLine("Waiting.OnMessage: awaiting WorkResponse...");
    try
    {
      // Use the native EventIPC interface for request/response. Publish and
      // PublishAsync do not await the state machine's async OnMessage callback.
      var response = await events.RequestAsync<WorkRequest, WorkResponse>(new WorkRequest(start.Text))
        .WaitAsync(TimeSpan.FromSeconds(2))
        ?? throw new InvalidOperationException("The worker returned no response.");

      context.Parameters["Response"] = response.Text;

      Console.WriteLine($"Waiting.OnMessage: received '{response.Text}'.");
      context.NextState(Result.Success);
    }
    catch (Exception exception)
    {
      // OnMessage exceptions are otherwise swallowed by the state machine.
      Console.WriteLine($"Waiting.OnMessage: {exception.Message}");
      context.NextState(Result.Error);
    }
  }

  public Task OnTimeout(Context<StateId> context)
  {
    Console.WriteLine("Waiting: command timed out.");
    context.NextState(Result.Error);
    return Task.CompletedTask;
  }
}
