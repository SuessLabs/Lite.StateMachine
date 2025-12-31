# Lite State Machine for .NET

<image align="right" width="200" height="200" src="https://github.com/SuessLabs/Lite.StateMachine/blob/develop/docs/icon-128x128.png" />

Flexible lightweight finite state machine (FSM) for .NET, supporting shared context for passing parameters, composite (sub) states, command states, lazy-loading and thread safe. The library is AOT friendly, cross-platform and optimized for speed for use in enterprise, robotic/industrial systems, and even tiny (mobile) applications.

The Lite State Machine is designed for vertical scaling. Meaning, it can be used for the most basic (tiny) system and beyond medical-grade robotics systems.

> Copyright 2021-2025 Xeno Innovations, Inc. (_dba, Suess Labs_)<br/>
> Created by: Damian Suess<br/>
> Date: 2021-06-07<br/>

## Package Releases

| Package | Stable | Preview
|-|-|-|
| Lite.StateMachine | [![Lite.StateMachine NuGet Badge](https://img.shields.io/nuget/v/Lite.StateMachine)](https://www.nuget.org/packages/Lite.StateMachine/) | [![Lite.StateMachine NuGet Badge](https://img.shields.io/nuget/vpre/Lite.StateMachine)](https://www.nuget.org/packages/Lite.StateMachine/)

## Usage

Create a _state machine_ by defining the states, transitions, and shared context.

You can define the state machine using either the fluent design pattern or standard line-by-line. Each state is represented by a enum `StateId` in the following example.

### Basic State

```cs
// That's it! Just create the state machine, register states, and run it.
var machine = await new StateMachine<StateId>()
  .RegisterState<BasicState1>(StateId.State1, StateId.State2)
  .RegisterState<BasicState2>(StateId.State2, StateId.State3)
  .RegisterState<BasicState3>(StateId.State3)
  .RunAsync(StateId.State1);

// To avoid hung states, you can pass in a timeout value (in milliseconds)
// Useful for robotic systems; fail fast and recover!
var machine = new StateMachine<BasicStateId>();
machine.DefaultStateTimeoutMs = 3000;
```

Define States:

```cs
// Optional Wrapper
public class BaseState : IState<StateId>
{
  public virtual Task OnEntering(Context<StateId> context) => Task.CompletedTask;
  public virtual Task OnEnter(Context<StateId> context) => Task.CompletedTask;
  public virtual Task OnExit(Context<StateId> context) => Task.CompletedTask;
}

public class BasicState1() : BaseState
{
  public async Task OnEnter(Context<BasicStateId> context)
  {
    await Task.Yield(); // Some async work here...
    context.NextState(Result.Ok);
  }
}

public class BasicState2() : BaseState
{
  public Task OnEnter(Context<StateId> context)
  {
    context.NextState(Result.Ok);
    return Task.CompletedTask; // Notice, we did not async/await this method
  }
}

public class BasicState3() : BaseState
{
  public Task OnEnter(Context<StateId> context)
  {
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}
```

### Composite States

![Sample Composite State Image](https://github.com/SuessLabs/Lite.StateMachine/blob/develop/docs/SampleGraphviz-1080.png)

```cs
using Lite.StateMachine;

var ctxProperties = new PropertyBag() { { "CounterKey", 0 } };

// Note the use of generics '<TStateClass>' to strongly-type the state machine
var machine = new StateMachine<CompositeL1StateId>()
  .RegisterState<Composite_State1>(CompositeL1StateId.State1, CompositeL1StateId.State2)

  .RegisterComposite<Composite_State2>(
    stateId: CompositeL1StateId.State2,
    initialChildStateId: CompositeL1StateId.State2_Sub1,
    onSuccess: CompositeL1StateId.State3)

  .RegisterSubState<Composite_State2_Sub1>(
    stateId: CompositeL1StateId.State2_Sub1,
    parentStateId: CompositeL1StateId.State2,
    onSuccess: CompositeL1StateId.State2_Sub2)

  .RegisterSubState<Composite_State2_Sub2>(
    stateId: CompositeL1StateId.State2_Sub2,
    parentStateId: CompositeL1StateId.State2,
    onSuccess: null) // NULL denotes returning to parent state on success

  .RegisterState<Composite_State3>(CompositeL1StateId.State3);

// Optional, pass in starting Context Property
await machine.RunAsync(CompositeL1StateId.State1, ctxProperties);
```

States are represented by classes that implement the `IState` interface.

```cs
public class Composite_State1() : BaseState
{
  public override Task OnEnter(Context<CompositeL1StateId> context)
  {
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}

public class Composite_State2() : BaseState
{
  public override Task OnEnter(Context<CompositeL1StateId> context)
  {
    // Signify we're ready to go to sub-states
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }

  public override Task OnExit(Context<CompositeL1StateId> context)
  {
    // Signify we're ready to go to next state after composite
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}

public class Composite_State2_Sub1() : BaseState
{
  public override Task OnEnter(Context<CompositeL1StateId> context)
  {
    context.Parameters.Add("ParameterSubStateEntered", SUCCESS);
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}

public class Composite_State2_Sub2() : BaseState
{
  public override Task OnEnter(Context<CompositeL1StateId> context)
  {
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}

public class Composite_State3() : BaseState
{
  public override Task OnEnter(Context<CompositeL1StateId> context)
  {
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}
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

## References
