# Lite State Machine for .NET

Flexible lightweight finite state machine (FSM) for .NET, supporting shared context for passing parameters, composite (sub) states, command states, lazy-loading and thread safe. The library is AOT friendly, cross-platform and optimized for speed for use in enterprise, robotic/industrial systems, and even tiny (mobile) applications.

The Lite State Machine is designed for vertical scaling. Meaning, it can be used for the most basic (tiny) system and beyond medical-grade robotics systems.

Download [Lite.State @ NuGet.org](https://www.nuget.org/packages/Lite.State) today!

Copyright 2022-2025 Xeno Innovations, Inc. (dba, Suess Labs)<br />
Created by: Damian Suess<br />
Date: 2022-06-07<br />

## Usage

### Standard and Composite State Registration

```cs
  // Standard state
  machine.RegisterState(WorkflowState.Start, () => new StartState());

  // Composite State
  machine.RegisterState(WorkflowState.Processing, () => new ProcessingState()), sub =>
  {
    sub.RegisterState(WorkflowState.Load,     () => new LoadState());
    sub.RegisterState(WorkflowState.Validate, () => new ValidateState());
    sub.SetInitial(WorkflowState.Load);
  });

  machine.SetInitial(WorkflowState.Start);
```

### Command State

```cs
var aggregator = new EventAggregator();

var machine = new StateMachine<WorkflowState>(aggregator)
{
  // Set default timeout to 3 seconds (can override per-command state)
  DefaultTimeoutMs = 3000,
};

// Register top-level states
machine.RegisterState(WorkflowState.Start, () => new StartState());
machine.RegisterState(WorkflowState.Processing, () => new ProcessingState(), subStates: (sub) =>
{
  // Register sub-states inside Processing's submachine
  sub.RegisterState(WorkflowState.Load, () => new LoadState());
  sub.RegisterState(WorkflowState.Validate, () => new ValidateState());
  sub.SetInitial(WorkflowState.Load);
});

machine.RegisterState(WorkflowState.AwaitMessage, () => new AwaitMessageState());
machine.RegisterState(WorkflowState.Done, () => new DoneState());
machine.RegisterState(WorkflowState.Error, () => new ErrorState());
machine.RegisterState(WorkflowState.Failed, () => new FailedState());

// Set initial state
machine.SetInitial(WorkflowState.Start);

// Start workflow
var ctx = new PropertyBag { { ParameterKeyTest, TestValueBegin } };
machine.Start(ctx);
```

### Generate DOT Graph (GraphViz)

```cs
var uml = machine.ExportUml(includeSubmachines: true);
```

## Features

* Shared Context objects
* Passing of parameters between state transitions via `Context`
* Basic Linear state machine
* Composite States (Sub-states)
  * Similar to Actor/Director model
* Command States with optional Timeout
  * Uses internal Event Aggregator for sending/receiving messages
  * Allows users to hook to external messaging services (TCP/IP, RabbitMQ, DBus, etc.)
* State Handlers (pseudo states)
  * OnEntering - Initial entry of the state
  * OnEnter - Resting (idle) place for state.
  * OnExit - (Optional) Thrown during transitioning. Used for housekeeping or exiting activity.
  * OnMessage (Optional)
    * Must ensure that code has exited `OnMessage` before going to the next state.
  * OnTimeout - (Optional) Thrown when the state is auto-transitioning due to timeout exceeded
* Transition has knowledge of the `PreviousState` and `NextState`

## vNext Proposals

### Generics for State Definitions

```cs
machine.RegisterState<StartState>(WorkflowState.Start);
machine.RegisterState<ProcessingState>(WorkflowState.Processing, sub =>
{
  sub.RegisterState<LoadState>(WorkflowState.Load)
     .RegisterState<ValidateState>(WorkflowState.Validate)
    .SetInitial(WorkflowState.Load);
});
```

## References
