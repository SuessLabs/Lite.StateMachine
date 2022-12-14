# Concept Designs

## Design Questions

* [ ] Is it better to pre-define transition (strict rules) or allow a state to decide where it wants to go (free-floating, code driven).
  * STRICT: Requiring 2 enums (StateId, TriggerId)
  * FREE: Requires only 1 enum, StateId.

## Model A - Defined Transitions

```cpp
enum StateId =
{
  Init,
  Opening,
  Opened,
  Closing,
  Closed,
};

////enum TriggerId
////{
////  // Lock,
////  // Unlock,
////  Open,
////  Close,
////};

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

## Model B - Free-Floating

```cpp
```

### Pros/Cons

* PRO: You only need 1 enum for StateId
* PRO: You can define a default StateId to transition and simply use. i.e. `NextState()`
* PRO: You can transition anywhere based on internal logic changes. i.e. `NextState(someStateId)`.
* CON: Harder to generate GraphViz diagrams to preview errors
* CON: You can accidentially transition somewhere else.
*