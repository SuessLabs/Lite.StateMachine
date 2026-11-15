# Sample 05 - Command States

All states (`State1` through `State5`), messages, and the simulated worker are in
`Program.cs`. Each state implements `ICommandState<StateId>` through a small shared
`CommandState` base class that handles subscriptions, results, and timeouts.

Run from the repository root:

```powershell
dotnet run --project samples/Sample05.CommandStates --framework net10.0
```

| State | Intended action | Result | Next state |
| --- | --- | --- | --- |
| State1 | Connect to a device | Success | State2 |
| State2 | Try starting with the safety door open | Failure | State3 |
| State3 | Recover by closing the door | Success | State4 |
| State4 | Read a sensor with a simulated communication fault | Error | State5 |
| State5 | Clean up and disconnect | Success | End |

`OnEnter` publishes an `ActionCommand` to a simulated device service using the
built-in event aggregator. The service prints its action and publishes an
`ActionCompleted` result. The active state's `OnMessage` checks the state ID and
calls `context.NextState(result)` to select `onSuccess`, `onFailure`, or `onError`.
Returning from `OnEnter` alone does not advance a command state.

The worker deliberately returns `Failure` for State2 and `Error` for State4 so
every run demonstrates all three outcomes. No hardware or external service is
required. Each state logs its request, received result, and exit. Missing results
use the machine's default command timeout; the executable also has a ten-second
cancellation guard and verifies that cleanup completed.
