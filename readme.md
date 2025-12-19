# Lite State Machine for .NET

Small finite state machine for .NET

Copyright 2022-2025 Xeno Innovations, Inc. (dba, Suess Labs)<br />
Created by: Damian Suess<br />
Date: 2022-06-07<br />

Flexible lightweight finite state machine (FSM) for .NET, supporting shared context for passing parameters, composite (sub) states, command states, lazy-loading and thread safe. The library is AOT friendly, cross-platform and optimized for speed for use in enterprise, robotic/industrial systems, and even tiny (mobile) applications.

The Lite State Machine is designed for vertical scaling. Meaning, it can be used for the most basic (tiny) system and beyond medical-grade robotics systems.

## Target Features

* Shared Context objects
* Passing of parameters between state transitions
* Basic Linear state machine
* Composite States (Sub-states)
  * Similar to Actor/Director model
* State Handlers (pseudo states)
  * OnEntering - Initial entry of the state
  * OnEnter - Resting (idle) place for state.
  * OnExit - (Optional) Thrown during transitioning. Used for housekeeping or exiting activity.
  * OnMessage (Optional)
    * Must ensure that code has exited `OnMessage` before going to the next state.
  * OnTimeout - (Optional) Thrown when the state is auto-transitioning due to timeout exceeded
* Transition has knowledge of the `PreviousState` and `NextState`

## References
