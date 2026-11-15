# Sample 10 - Using Lite.EventIPC for External Event Aggregator

Lite.StateMachine can use an external event aggregator through its constructor's
`eventAggregator` parameter. The interfaces in the two libraries differ, so this
sample supplies `EventIpcAdapter`, which implements `Lite.StateMachine.IEventAggregator`
and delegates event delivery to the NuGet package `Lite.EventIpc` (1.3.2).
The core state machine library requires no changes.

Run from the repository root with the .NET 10 SDK or newer:

```powershell
dotnet run --project samples/Sample10.UsingEventIPC
```

## State flow

1. `PreparingState` performs a basic setup step and advances to `WaitingState`.
2. `WaitingState` is an `ICommandState<StateId>` subscribed to `StartWork`.
   Its `OnEnter` publishes that event through `context.EventAggregator`.
   The machine has already subscribed, so the message reaches `OnMessage`.
3. Inside `OnMessage`, the state calls the native EventIPC
   `RequestAsync<WorkRequest, WorkResponse>` and **awaits the response** from an
   asynchronous worker. It saves the response in the context, then calls
   `context.NextState(Result.Success)`.
4. `CompletedState` prints the saved response and finishes the machine.

Expected output:

```text
Preparing: starting the sample.
Waiting: publishing StartWork through the adapter.
Waiting.OnMessage: awaiting WorkResponse...
Worker: processing 'hello from Lite.EventIPC'...
Waiting.OnMessage: received 'HELLO FROM LITE.EVENTIPC'.
Completed: HELLO FROM LITE.EVENTIPC
```

## Adapter and async behavior

```csharp
var events = new Lite.EventIpc.EventAggregator();
var adapter = new EventIpcAdapter(events)
  .RegisterMessageType<StartWork>();

var machine = new StateMachine<StateId>(eventAggregator: adapter);
```

Register every one-way message type with the adapter before running the machine.
It preserves exact runtime type dispatch, deduplicates subscription types, and
implements wildcard subscriptions over those registered types. Unknown types
throw `NotSupportedException`. Native request/response types (`WorkRequest` and
`WorkResponse`) need no adapter registration because they use EventIPC directly.
Other components can publish typed events directly on the same `events` instance.

Subscriptions retain their delegates because EventIPC uses weak references.
Disposing a state machine subscription unsubscribes its EventIPC delegates;
the sample also explicitly unregisters its worker when finished.

The state machine's subscription API accepts `Action<object>`, so neither
`Publish` nor EventIPC's `PublishAsync` waits for an asynchronous `OnMessage` to
finish. Instead, await `machine.RunAsync(...)` for the state flow and call
`NextState` only after awaited work completes. The example uses `WaitAsync` to
bound the response wait to two seconds, a five-second command timeout, and a
ten-second run cancellation guard. The response deadline stops waiting; it does
not cancel the worker. Errors and timeouts end the flow without reaching
`CompletedState`, and the executable reports failure.

This sample uses EventIPC's **local** event aggregator and request/response
support. It does not configure a named pipe, TCP, or other IPC transport.
