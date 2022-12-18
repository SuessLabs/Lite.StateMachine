# Sample State Designs

The following sample is based upon State Machine 'Model A' which uses a chaining pattern for State construction.

## Design Patterns

* Director/Builder - Separate the construction of a complex object from its representation.
* Fluent - design relies extensively on method chaining to increase code legibility.

## Model A - States with Builder Design Pattern

The state's construction leverages the Builder and Fluent design patterns to provide legiable construction and chaining of methods.

Allowable state transitions are defined upfront providing the `StateId` and transitioning is performed by providing where to go next before exiting the state.

The transitions here are use the `StateId` transition to the next state.

### QnA

* INVALID_TRANSITION: How should we catch/handle an invalid state transition?
  * Should the StateMachine class provide a master "OnError" callback to allow the system to reset?
  * Or, should States provided their own "OnError" callbacks?
* ON_STATE: Should we transition to (A) "In State" or (B) "Entering the State", and why would we need, "B"?
  * A: "OnEnter > OnMessage > OnTimeout > OnExit"
  * B: "OnEntering > OnEntered > OnMessage > OnTimeout > OnExit"
* Composite States?
* Error Handling
  1. Invalid transition
  2. Code crashing

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
  Error,
  // Faulting,
  // Faulted,
};

StateMachine _machine;

void Builder()
{
  // TODO: Add locks to door

  // CONCEPT ALT-DESIGN: Master OnError handler to reduce re-writing
  // _machine.OnError(StateId.Error, DoorOnError);

  // State("NameGraphViz", OnEnter, OnMessage, OnTimeout, OnExit, OnError, <int>msTimeout),
  _machine.State(StateId.Uninitialized, "Uninitialized", UninitializedOnEnter, NULL, NULL, InitOnExit, DoorOnError)
          .AllowNext(StateId.Opened)
          .AllowNext(StateId.Closed);

  // NOTE: Defining DoorOnError handler. Should we use a master 'catch all'?
  _machine.State(StateId.Init, "Init", InitOnEnter, NULL, NULL, InitOnExit, DoorOnError)
          .AllowNext(StateId.Opened)
          .AllowNext(StateId.Closed);

  // OnTimeout: Failed to open, go to Closed state.
  //// _machine.State(StateId.Opening, "Opening", OpeningOnEnter, NULL, NULL, OpeningOnTimeout, NULL, 5000) // in-line constructor
  _machine.State(StateId.Opening, "Opening")
          .OnEnter(OpeningOnEnter)
          .OnTimeout(OpeningOnTimeout, 5000)
          .AllowNext(StateId.Opened)
          .AllowNext(StateId.Closed),

  _machine.State(StateId.Opened, "Opened")
          .OnEnter(DoorOpened_OnEnter)          // Optional: Add handler via chaining
          .OnMessage(DoorOpened_OnMessage)      // If (msg.Id == "DoorClose") state.Next(StateId.Closing)
          .AllowNext(StateId.Closing),

  // OnTimeout: Failed to close, go to Opened state
  _machine.State(StateId.Closing, "Closing", ClosingOnEnter, NULL, ClosingOnTimeout, NULL, 5000)
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

  // Create GraphViz graph
  std:string dotGraph = machine.BuildGraphviz();

  // Begin running the state machine
  auto machine.Start();
  // machine.Stop();

  AppLifetime();
}

void AppLifetime()
{
  // Mutex if we're doing a thread.join.
  // thread.Join on the machine thread

  for(;;)
  {
    // Used to trigger timeouts
    _machine.WaitFor()
  }

}

StateId InitOnEnter()
{
  // If `bool ret = State.Next(BadTrans)` is invalid, it will return false
  // If the return value of "_state.Next(...) == -1" then the transition is not allowed.
  if (_door->IsOpened)
    return _state.Next(StateId.Opened);
  else if (_door->IsClosed)
    return _state.Next(StateId.Closed);
  else
    return _state.Next(StateId.InvalidTransition);  // This would result in the OnError state to get thrown

  // ---OR---
  // _state.Trigger(TriggerId.OpenDoor);
}
```
