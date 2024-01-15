# Lite State Machine for .NET

Small finite state machine for .NET

Copyright 2022 Xeno Innovations, Inc. (aka. Suess Labs)<br />
Created by: Damian Suess<br />
Date: 2022-06-07<br />

A flexible lightweight state machine for .NET which supports a shared context, passing parameters, and thread safe. The system must be cross-platform compatible and enterprise ready.

## C# Target Features

* Shared Context objects
* Passing of parameters between state transitions
* Basic Linear state machine
* Sub-states
* State Handlers
  * OnEntering - Initial entry of the state
  * OnEnter - Resting (idle) place for state.
  * OnExit - (Optional) Thrown during transitioning. Used for housekeeping or exiting activity.
  * OnMessage - (TBD)
    * Must ensure that code has exited `OnMessage` before going to the next state.
  * OnTimeout - (Optional) Thrown when the state is auto-transitioning due to timeout exceeded

### Non-Critical Path Features

* Actor model state machine (>=1 sub-actors)
* Transition triggers

## References
