# Lite State Machine for .NET

Small finite state machine for .NET

Copyright 2022-2025 Xeno Innovations, Inc. (aka. Suess Labs)<br />
Created by: Damian Suess<br />
Date: 2022-06-07<br />

A flexible lightweight state machine for .NET which supports a shared context, passing parameters, and thread safe. The system is cross-platform compatible and enterprise ready.

Lite State Machine is designed for vertical scaling. Meaning, it can be used for the most basic (tiny) system and beyond medical-grade robotics systems.

## C# Target Features

Transitions can occur between sub-states and parent states.

* Shared Context object
  * `Parameters` payload object for passing data between state transitions.
  * `Errors` payload object for passing error information between states.
  * (OUT OF SCOPE) `Timeout` property for getting/setting state timeouts.
  * `NextState()` method for requesting the next state.
  * `CurrentState` property for getting the current state.
  * `PreviousState` property for getting the previous state.
* Lazy-load states
* Basic Linear state machine
* Sub-states
* State Handlers
  * OnEntering - (Optional) Initial entry of the state
  * OnEnter - Resting (idle) place for state.
  * OnMessage - (TBD)
    * Must ensure that code has exited `OnMessage` before going to the next state.
  * OnTimeout - (Optional) Thrown when the state is auto-transitioning due to timeout exceeded
  * OnExit - (Optional) Thrown during transitioning. Used for housekeeping or exiting activity.

### Non-Critical Path Features

* Actor model state machine (>=1 sub-actors)
* Transition triggers
* Add UML-Style Triggers and guards
* Export state transition UML

## Installation

The Lite State Machine is available as a NuGet package. You can install it via the NuGet Package Manager Console with the following command:

```sh
```

## Examples

### Simple States

### Composite States

### State History

## MK2

1. Optional State History Tracking
   * Composite states can remember the last active sub-state.
   * Controlled by a flag EnableHistory in CompositeState.
2. Sample Program - Demonstrates:
   * Simple states
   * Composite states
   * State history
   * Timeout handling
   * Message queue
   * Event-driven transitions
3. Unit Tests (MSTest)
   * For a lightweight version called LiteState (basic FSM without composite states).
   * Tests cover:
     * Transition logic
     * Timeout behavior
     * Message handling
     * Context propagation

## Out of Scope Features

### Context
* `IsInState(string stateName)` method for checking if the current state matches the provided state name.
* `StateMachine` property for accessing the state machine instance.
* `Context` property for accessing the context object.
* `Data` property for storing arbitrary data.
* `CancellationToken` property for cooperative cancellation.
* `Logger` property for logging.
* `MessageQueue` property for message handling.
* `StartTime` property for tracking when the state was entered.
* `ElapsedTime` property for tracking how long the state has been active.
* `SetTimeout(TimeSpan timeout)` method for setting state timeouts.
* `ClearTimeout()` method for clearing state timeouts.
* `IsTimeoutExceeded` property for checking if the timeout has been exceeded.
* `ResetTimeout()` method for resetting the timeout timer.
* `GetParameter<T>(string key)` method for retrieving parameters.
* `SetParameter(string key, object value)` method for setting parameters.
* `GetError<T>(string key)` method for retrieving errors.
* `SetError(string key, Exception error)` method for setting errors.
* `LogInfo(string message)` method for logging informational messages.
* `LogWarning(string message)` method for logging warning messages.
* `LogError(string message, Exception exception)` method for logging error messages.
* `EnqueueMessage(object message)` method for adding messages to the queue.
* `DequeueMessage()` method for retrieving messages from the queue.
* `PeekMessage()` method for peeking at the next message in the queue.
* `ClearMessages()` method for clearing the message queue.
* `GetElapsedTime()` method for getting the elapsed time as a TimeSpan.
* `GetStartTime()` method for getting the start time as a DateTime.
* `GetCancellationToken()` method for getting the cancellation token.
* `GetLogger()` method for getting the logger.
* `GetMessageQueue()` method for getting the message queue.
* `GetStateMachine()` method for getting the state machine instance.
* `GetContext()` method for getting the context object.
* `GetData<T>()` method for getting the arbitrary data.

## References
