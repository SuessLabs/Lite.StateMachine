# Concept Designs

[[_TOC_]]

## Design Patterns

* **Director/Builder**
  * Separate the construction of a complex object from its representation.
  * Allows you to construct an object step-by-step. Helpful when obj has many optional parameters.
  * Typically uses a separate Builder class and a final `Build()` method to return the fully constructed object.
* **Fluent**
  * Design relies extensively on method chaining to increase code legibility.

### Example

```cs
// Fluent Builder Pattern

Car car = new CarBuilder()
    .WithMake("Honda")
    .WithModel("Civic")
    .WithColor("Red")
    .Build();

// The builder class pattern for constructing a "new Car"
public class CarBuilder
{
  private Car _car = new();

  // Note returns the builder itself for chaining (fluent)
  public CarBuilder WithMake(string make) { _car.Make = make; return this; }
  public CarBuilder WithModel(string model) { _car.Model = model; return this; }
  public CarBuilder WithColor(string color) { _car.Color = color; return this; }

  // Builder's `Build` method.
  public Car Build() { return _car; }
}
```

## Chosen


## Design Concepts

### Extension Libraries

Lite State Machine at its core should be simple. Extension libraries should be provided to add additional features that are not normally apart of the core system.  i.e. Messaging interfaces.

### State Types

* Composite (parent) State - _Has children and will move to next state upon completion_
* Standard (custom) State
* Data Load State - _Pulls data from DB (from another SBMP)_
* Command - _Sends command to another SBMP and wait for response_

#### Sending Messages

Sending a message (aka: commands) requires a specific `MessageType` to send so the receiver knows its only for them via a "request", as well as receiving the "response" back.

For example:

1. System control state `ToasterUnlock` sends, `CommandType.UnlockRequest`.
2. The hw-service listens for this "request" and performs the unlock.
3. The hw-service responds with the "response", `CommandType.UnlockResponse`.
4. SysCtrl's `ToasterUnlock_OnMessage` receives this message
   1. If no errors, we proceed to the next state, `ToasterUnlocked`.
   2. If an error happened or `ToasterUnlock_OnTimeout` is encountered, we goto `ToasterFailedState` and attempt recovery.
5. Done.

### Transition Mechanism

**Question:** Is it better to pre-define transition (strict rules) or allow a state to decide where it wants to go (free-floating, code driven).

For simplicity, Model-2 "Strict Predefined (single)" may be best.

#### 1) STRICT with Triggers

Pre-defined state-to-trigger for logical paths. Requiring 2 enums (StateId, TriggerId). AKA: Guarding

* CON: Have to maintain 2 definitions. 1) State-Name, 2) State-Trigger
* CON: Trigger naming can get messy or confusing (_user problem_)

#### 2) STRICT Predefined (single success exit) <--- THIS ONE

Pre-defined transitions using a single enum (OnSuccess, OnError, OnFailure). This will need a default OnError/OnFailure defined in case the OnSuccess fails.

Transitioning happens by returning the enum `Result` as either, `Success`, `Error` (soft-error), `Failure` (hard-fault).

```cs
// Happy path
ToasterUnlocked_OnEnter(Context)
{
  return Result.Success;
}

ToasterUnlock_OnEnter(Context ctx)
{
  ctx.AddError("Something bad happened");
  return Result.Error;
}
```

Pros/Cons:
* PRO: Able to pre-render UML diagrams
* PRO: Success and error case transitions clearly defined
* CON: Only 1 success exit
* CON: Limited exit paths (1 success, 1 failure, 1 timeout)

#### 3) STRICT - Predefinied (multiple successful exit strategies)

```cs
State multiSuccess = new()
  .OnEnter(MultiSuccess_OnEnter)
  .AllowNext(StateId.Opened)
  .AllowNext(StateId.Closed);

public int MultiSuccess_OnEnter(Context params)
{
  return StateId.Opened;
}
```

* CON: How to strictly define which success state to exit to?
  * Cannot say, `return State.Success` - there are multiple successes
* CON: Cannot use `return State.Success;`

#### 4) FREE - User sets whatever

* PRO: Requires only 1 enum, StateId.
* PRO: User can define whatever based on logical paths
* CON: Hard to pre-drawer UML diagrams
* CON: Users can infinite loop themselves, or "fall off a cliff"

### Builder Design Pattern

* [ ] Builder design pattern for defining a state? (aka: Fluent)
  * [ ] What is the overhead of a C++ Fluent design pattern?
  * Sample: `State(StateId.Init, ...).AllowNext(Opened).AllowNext(Closed);`

### Passing Parameters (Context)

Passing parameters via a `ParameterSet` context ensures data object are associated cleanly.

* [ ] A: Context as a singleton?
  * Context would live in the main StateMachine class, being passed to each `State(Context)`.
  * PRO: It's small and don't care if a parameter is forgotten
  * CON: How to clean out Parameters? The same param isn't needed for everything.
* [X] B: Context as a ParameterSet passed everytime?  `Dictionary<enum ParameterType, object payload>()`
  * PRO: Params are passed and not maintained. Allowing for minimal memory overhead.
  * CON: Requires dev to grab and repush params onto context every time.

### CoAP Messaging

* [ ] CoAP Messaging needs to interact with State Machine.
* The `observed` events would then be passed to the active state's OnMessage.
* CoAP - Constrained Application Protocol (_similar to SBMP, just not TCP/IP_)

### Message Services (EXT-lib)

Extension library for handling TCP/IP based state based message processing.

### State Properties

* [B] ON_STATE: Should we transition to (A) "In State" or (B) "Entering the State", and why would we need, "B"?
  * A: "OnEnter > OnMessage > OnTimeout > OnExit"
  * B: "OnEntering > OnEntered > OnMessage > OnTimeout > OnExit"

**Winner:** "B"

* `OnEntering` (optional)
* `OnEnter`
* `OnMessage` (Optional)
* `OnTimeout`
* `OnExit`

### Composite States

* [X] Composite (sub) States?

Yes, this is a must


### Follow-up Features

* [ ] OnMessage Handler
  * Wait for message to come back before allowing a transition.
  * Once
  * May require a main loop to be hit
* [ ] OnError/OnFatal Handler
* [ ] OnTimeout Handler
  * May require a main loop to call `_fsm.WaitFor()`
* [ ] Example with Failover state
* [ ] Mutex and Thread.Join on the Machine thread - _mainAppLoop_
* [ ] Allow for State Based Message Processing - _StateClass SBMP_
* [ ] Exportable to GraphViz DotGraph, PlantUML

## Model A - States with Builder Design Pattern

The state's construction leverages the Builder and Fluent design patterns to provide legiable construction and chaining of methods.

Allowable state transitions are defined upfront providing the `StateId` and transitioning is performed by providing where to go next before exiting the state.

The transitions here are use the `StateId` transition to the next state.

### Pros/Cons and QnA

* How should we catch/handle an invalid state transition?
  * Should the StateMachine class provide a master "OnError" callback to allow the system to reset?
  * Or, should States provided their own "OnError" callbacks?

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

  // Create GraphViz graph
  std:string dotGraph = machine.BuildGraphviz();

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

## Model B - State Defined Transitions

* Date: 2022-12-15
* Modified: 2022-12-16

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

## Model D - State Classes

Above focuses on defining the state machine and directing the state's handling to a user-defined method.  This may work well with smaller projects, however, in larger projects it can benificial to use classes which may have their own uniquie private methods.

See file, `Sample State Class.md` for more information.

```cpp
class StateTemplate
{
  public:
    int Id;
    std::string Name;

    // Handlers
    virtual void OnEnter(StateContext*);
    virtual void OnMessage(StateContext*);
    virtual void OnTimeout(StateContext*);
    virtual void OnExit(StateContext*);
    virtual void OnError(StateContext*);
};

void main()
{
  StateMachine sm;

  sm.State(StateId::Uninitialized, "Unitialized", new Uninitialized())
    .Timeout(5000)
    .AllowNext(StateId.Initialize);

  sm.State(StateId::Init, "Initialize", new Initialize())
    .Timeout(5000)
    .AllowNext(StateId.Opened)
    .AllowNext(StateId.Closed);

  sm.State(StateId::Opened, "Opened", new Opened())
    .AllowNext(StateId.Opening);

  sm.State(StateId::Closed, "Closed", new Closed())
    .AllowNext(StateId.Closing);

  sm.State(StateId::Opening, "Opening", new Opening())
    .Timeout(5000)                          // Timeout after 5 seconds
    .AllowNext(StateId.Opened)              // Allow transitioning to Opened state
    .AllowNext(StateId.Closed)              // Allow transitioning to Closed state
    .MessageType(MessageType::DoorOpened);  // Filter incoming message types for "DoorOpened"

  for(;;)
  {
    sm.WaitFor();
  }
}

class Uninitialized : State
{
  void OnEnter(StateContext* context)
  {
    context->Next(StateId.Init);
  }
}

class Initialize : State
{
  void OnEnter(StateContext* context)
  {
    if (0 == 0)
      context->Next(StateId.Opened);
    else
      context->Next(StateId.Closed)
  }
}
```
