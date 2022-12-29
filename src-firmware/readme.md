# Tiny State Machine

The Tiny State Machine is geared towards implementing on small embedded devices such as ESP32s or Arduinos where resources are limited and can benifit from a finite state machine.

This flexible lightweight state machine for .NET which suports a shared context, passing parameters, and thread safe. The system must be cross-platform compatible and entriprise ready.

Copyright 2022 Xeno Innovations, Inc. (aka. Suess Labs)<br />
Created by: Damian Suess<br />
Date: 2022-12-14<br />

## Target Features

* Transition triggers
* State Handlers
  * OnEnter - Initial entry of the state
  * OnHandle - Resting (idle) place for state.
  * OnExit - (Optional) Thrown during transitioning. Used for housekeeping or exiting activity.
  * OnTimeout - (Optional) Thrown when the state is auto-transitioning due to timout exceeded

### Future Features

* Passing of parameters between state transitions
* Composite (sub) States
* OnMessage handler

## References

* [Builder Pattern](https://en.wikipedia.org/wiki/Builder_pattern)
* [Fluent Interface](https://en.wikipedia.org/wiki/Fluent_interface#C++)
