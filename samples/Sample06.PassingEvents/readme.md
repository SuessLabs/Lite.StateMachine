# Sample 06 - Event Aggregator for Passing Events

All states (`State1`, `State2`, and `State3`) and message records are in `Program.cs`.
This sample uses the built-in `Lite.StateMachine.EventAggregator` with typed
subscriptions on command states.

Run from the repository root:

```powershell
dotnet run --project samples/Sample06.PassingEvents --framework net10.0
```

1. State1 publishes `OrderCreated` for three notebooks. Its `OnMessage` receives
   the product and quantity and prepares a `PriceRequested` payload for State2.
2. State2 publishes that payload after entering. Its `OnMessage` awaits a
   simulated price lookup, calculates `3 x 4.50 = 13.50`, and prepares an
   `OrderPriced` payload for State3.
3. State3 publishes the priced payload after entering. Its `OnMessage` prints
   the receipt and completes the flow.

Each state's `SubscribedMessageTypes` declares the exact message type it accepts.
The shared `EventState` base class provides the empty lifecycle hooks and timeout
handling so the numbered states focus on sending and receiving data.

The aggregator delivers immediately to current subscribers and does not buffer
messages for future states. To pass data safely between states, each `OnMessage`
stores the next payload in `context.Parameters["NextMessage"]` before advancing.
The next state's `OnEnter` republishes it after the machine has subscribed that
state. This keeps the handoff explicit and avoids losing events during transitions.

`Publish` returns without awaiting asynchronous `OnMessage` work. State2 calls
`NextState(Result.Success)` only after its awaited work and payload preparation
finish. The program awaits `RunAsync`, uses a ten-second cancellation guard, and
verifies that State3 produced its receipt.
