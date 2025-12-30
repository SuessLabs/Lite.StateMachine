# Lite State Machine for .NET

Flexible lightweight finite state machine (FSM) for .NET, supporting shared context for passing parameters, composite (sub) states, command states, lazy-loading and thread safe. The library is AOT friendly, cross-platform and optimized for speed for use in enterprise, robotic/industrial systems, and even tiny (mobile) applications.

The Lite State Machine is designed for vertical scaling. Meaning, it can be used for the most basic (tiny) system and beyond medical-grade robotics systems.

Download [Lite.StateMachine @ NuGet.org](https://www.nuget.org/packages/Lite.StateMachine) today!

Copyright 2022-2025 Xeno Innovations, Inc. (dba, Suess Labs)<br />
Created by: Damian Suess<br />
Date: 2022-06-07<br />

## Usage

Create a _state machine_ by defining the states, transitions, and shared context.

You can define the state machine using either the fluent design pattern or standard line-by-line. Each state is represented by a enum `StateId` in the following example.

```cs
using Lite.StateMachine;

// Note the use of generics '<TStateClass>' to strongly-type the state machine
var machine = new StateMachine<StateId>()
  .RegisterState<StartState>(StateId.Start);
  .RegisterState<ProcessingState>(
    StateId.Processing,
    onSuccess: StateId.Finalize,
    subStates: (sub) =>
  {
    sub
      .RegisterState<LoadState>(StateId.Load,);
      .RegisterState<ValidateState>(StateId.Validate);
      .SetInitial(StateId.Load);
  })
  .RegisterState<FinalizeState>(StateId.Finalize,)
  .SetInitial(StateId.Start);

machine.Start();

// Optional: Start with initial context
// var ctx = new PropertyBag { { ParameterKeyTest, TestValueBegin } };
// machine.Start(ctx);

// Extract final context
var ctxFinal = machine.Context.Parameters;
```

States are represented by classes that implement the `IState` interface.

```cs
  private class ProcessingState() : CompositeState<StateId>()
  {
    public override void OnEnter(Context<StateId> context) =>
      context.NextState(Result.Ok);
  }

  private class LoadState() : BaseState<StateId>()
  {
    public override void OnEntering(Context<StateId> context)
    {
      // About to enter our state...
    }

    public override void OnEnter(Context<StateId> context)
    {
      // State is now active...
      context.Parameters.Add("SomeParameterId", "SUCCESS");
      context.NextState(Result.Ok);
    }

    public override void OnExit(Context<StateId> context)
    {
      // State is leaving...
    }
  }

  private class FinalizeState()
    : BaseState<StateId>();
```

### Generate DOT Graph (GraphViz)

```cs
var uml = machine.ExportUml(includeSubmachines: true);
```

## Features

* AOT Friendly - _No Reflection, no Linq, etc._
* Passing parameters between state transitions via `Context`
* Types of States
  * **Basic Linear State** (`BaseState`)
  * **Composite** States (`CompositeState`)
    * Hieratical / Nested Sub-states
    * Similar to Actor/Director model
  * **Command States** with optional Timeout (`CommandState`)
    * Uses internal Event Aggregator for sending/receiving messages
    * Allows users to hook to external messaging services (TCP/IP, RabbitMQ, DBus, etc.)
* State Transition Triggers
  * Transitions are triggered by setting the context's next state result:
  * On Success: `context.NextState(Result.Ok);`
  * On Error: `context.NextState(Result.Error);`
  * On Failure: : `context.NextState(Result.Failure);`
* State Handlers
  * `OnEntering` - Initial entry of the state
  * `OnEnter` - Resting (idle) place for state.
  * `OnExit` - (Optional) Thrown during transitioning. Used for housekeeping or exiting activity.
  * `OnMessage` (Optional)
    * Must ensure that code has exited `OnMessage` before going to the next state.
  * `OnTimeout` - (Optional) Thrown when the state is auto-transitioning due to timeout exceeded
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
