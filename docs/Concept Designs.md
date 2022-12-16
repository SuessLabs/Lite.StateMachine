# Concept Designs

## Design Questions

* [ ] Is it better to pre-define transition (strict rules) or allow a state to decide where it wants to go (free-floating, code driven).
  * STRICT: Requiring 2 enums (StateId, TriggerId)
  * FREE: Requires only 1 enum, StateId.
* [ ] Builder design pattern for defining a state? (aka: Fluent)
  * [ ] What is the overhead of a C++ Fluent design pattern?
  * Sample: `State(StateId.Init, ...).AllowNext(Opened).AllowNext(Closed);`
* [ ] Passing parameters
  * A: Context as a ParameterSet passed everytime?
  * B: Context as a singleton?
    * PRO: It's small and don't care if a parameter
* [ ] CoAP Messaging needs to interact with State Machine.

### Follow-ups

* [ ] OnMessage
  * Wait for message to come back before allowing a transition.
* [ ] Example with Failover state
* [ ] Mutex and Thread.Join on the Machine thread - _mainAppLoop_
* [ ] Allow for State Based Message Processing - _StateClass SBMP_
* [ ] Exportable to GraphViz DotGraph, PlantUML

## Model A - State Defined Transitions

```cpp
enum StateId =
{
  Init,
  Opening,
  Opened,
  Closing,
  Closed,
};

// TODO: Add locks to door
State doorStates[] =
{
  // State("NameGraphViz", OnEnter, OnHandle, OnTimeout, OnExit, <int>msTimeout),
  State(StateId.Init, "Init", InitOnEnter, NULL, NULL, InitOnExit),
  State(StateId.Opening, "Opening", OpeningOnEnter, NULL, OpeningOnTimeout, NULL, 5000),   // OnTimout: Failed to open, go to Closed state
  State(StateId.Opened, "Opened", OpenedOnEnter),
  State(StateId.Closing, "Closing", ClosingOnEnter, NULL, ClosingOnTimeout, NULL, 5000),  // OnTimeout: Failed to close, go to Opened state
  State(StateId.Closed, "Closed", ClosedOnEnter),
};

// Allowable transitions.
// If `bool ret = State.Next(BadTrans)` is invalid, it will return false
Transition doorTransitions[] =
{
  // Transition(fromStateId, toStateId),
  Transition(StateId.Init, StateId.Opened),
  Transition(StateId.Init, StateId.Closed),
  Transition(StateId.Closed, StateId.Opening),
  Transition(StateId.Opening, StateId.Opened),
  Transition(StateId.Opening, StateId.Closed),  // Action: Failure to open
  Transition(StateId.Opened, StateId.Closing).
  Transition(StateId.Closing, StateId.Closed),
  Transition(StateId.Closing, StateId.Opened),  // Failure to close
};

void app_main()
{
  machine.Transitions(&doorTransitions);
  // Begin running the state machine
  machine.Start();

  machine.Stop();
}

```

### Pros/Cons

* PRO: You can generate GraphViz
* PRO: You get tightly-coupled rules
* CON: It's adds complication of an additional "Trigger" enumeration.
* CON: You're bounded to tightly-coupled rules (YES, duplicate)

## Model B - States with Builder Design Pattern

The Builder Design (aka: Fluent) Pattern allows cleaner construction of the States and their allowed transitions.

The transitions here are use the `StateId` transition to the next state.

### Pros/Cons

* How should we catch/handle an invalid state transition?

### Sample

```cpp
enum StateId =
{
  Uninitialized,
  Init,
  Opening,
  Opened,
  Closing,
  Closed,
  // Faulting,
  // Faulted,
};

StateMachine _machine;

void Builder()
{
  // TODO: Add locks to door
  // State("NameGraphViz", OnEnter, OnHandle, OnTimeout, OnExit, <int>msTimeout),
  _machine.State(StateId.Uninitialized, "Uninitialized", UninitializedOnEnter, NULL, NULL, InitOnExit)
          .AllowNext(StateId.Opened)
          .AllowNext(StateId.Closed);

  _machine.State(StateId.Init, "Init", InitOnEnter, NULL, NULL, InitOnExit)
          .AllowNext(StateId.Opened)
          .AllowNext(StateId.Closed);

  _machine.State(StateId.Opening, "Opening", OpeningOnEnter, NULL, OpeningOnTimeout, NULL, 5000)  // OnTimout: Failed to open, go to Closed state
          .AllowNext(StateId.Opened)
          .AllowNext(StateId.Closed),

  _machine.State(StateId.Opened, "Opened", OpenedOnEnter),
          .OnMessage(DoorOpened_OnMessage)            //  If (msg.Id == "DoorClose") state.Next(StateId.Closing)
          .AllowNext(StateId.Closing),

  _machine.State(StateId.Closing, "Closing", ClosingOnEnter, NULL, ClosingOnTimeout, NULL, 5000)  // OnTimeout: Failed to close, go to Opened state
          .AllowNext(StateId.Opened)
          .AllowNext(StateId.Closed),

  _machine.State(StateId.Closed, "Closed", ClosedOnEnter),
          .AllowNext(StateId.Opening),
}

void app_main()
{
  _machine.Transitions(&doorTransitions);

  // Validate configuration
  auto success = machine.Validate();

  // Begin running the state machine
  auto machine.Start();
  // machine.Stop();

  Lifetime();
}

void Lifetime()
{
  // Mutex if we're doing a thread.join.
  // thread.Join on the machine thread

  for(;;)
  {
    _machine.WaitFor()
  }

}

StateId InitOnEnter()
{
  // If `bool ret = State.Next(BadTrans)` is invalid, it will return false
  // If the return value of "_state.Next(...) == -1" then the transition is not allowed.
  if (1 == 1)
    return _state.Next(StateId.Opened);
  else
    return _state.Next(StateId.Closed);

  // ---OR---
  // _state.Trigger(TriggerId.OpenDoor);
}
```

## Model C - Strongly Defined States and Transitions

In this model, the States transition are fired based upon the `TriggerId`. This allows the TriggerId to be reused and provides a "pretty" text on the GraphViz diagram.

With this model, the develop needs define both a StateId and a TriggerId`.

```cpp
enum StateId =
{
  Init,
  Opening,
  Opened,
  Closing,
  Closed,
};

enum TriggerId
{
  // Lock,
  // Unlock,
  Open,
  Close,
};

// TODO: Add locks to door
State doorStates[] =
{
  // State("NameGraphViz", OnEnter, OnHandle, OnTimeout, OnExit, <int>msTimeout),
  State(StateId.Init,   "Init", InitOnEnter, NULL, NULL, InitOnExit),
  State(StateId.Opening, "Opening", OpeningOnEnter, NULL, OpeningOnTimeout, NULL, 5000),   // OnTimout: Failed to open, go to Closed state
  State(StateId.Opened, "Opened", OpenedOnEnter),
  State(StateId.Closing, "Closing", ClosingOnEnter, NULL, ClosingOnTimeout, NULL, 5000),  // OnTimeout: Failed to close, go to Opened state
  State(StateId.Closed, "Closed", ClosedOnEnter),
};

// Allowable transitions.
// If `bool ret = State.Next(BadTrans)` is invalid, it will return false
Transition doorTransitions[] =
{
  // Transition(TriggerName,    fromStateId, toStateId, TriggeredMethod),
  Transition(TriggerId.Open,    StateId.Init,     StateId.Opened,   NULL),
  Transition(TriggerId.Close,   StateId.Init,     StateId.Closed,   TriggeredMethod),
  Transition(TriggerId.Open,    StateId.Closed,   StateId.Opening,  TriggeredMethod),
  Transition(TriggerId.Open,    StateId.Opening,  StateId.Opened,   TriggeredMethod),
  Transition(TriggerId.Close,   StateId.Opening,  StateId.Closed,   TriggeredMethod),  // Action: Failure to open
  Transition(TriggerId.Close,   StateId.Opened,   StateId.Closing,  TriggeredMethod).
  Transition(TriggerId.Close,   StateId.Closing,  StateId.Closed,   TriggeredMethod),
  Transition(TriggerId.Open,    StateId.Closing,  StateId.Opened,   TriggeredMethod),  // Failure to close
};

void app_main()
{
  machine.Transitions(&doorTransitions);
  // Begin running the state machine
  machine.Start();

  machine.Stop();
}

```

### Pros/Cons

* PRO: You can generate GraphViz
* PRO: You get tightly-coupled rules
* CON: It's adds complication of an additional "Trigger" enumeration.
* CON: You're bounded to tightly-coupled rules (YES, duplicate)

## Model C - Free-Floating

```cpp
```

### Pros/Cons

* PRO: You only need 1 enum for StateId
* PRO: You can define a default StateId to transition and simply use. i.e. `NextState()`
* PRO: You can transition anywhere based on internal logic changes. i.e. `NextState(someStateId)`.
* CON: Harder to generate GraphViz diagrams to preview errors
* CON: You can accidentially transition somewhere else.
