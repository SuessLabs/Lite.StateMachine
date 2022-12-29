# Sample State Class

## Overview

The "State Class" design provides a class for each state which can optionally override key features such as `OnEnter`, `OnMessage`, `OnTimeout`, `OnExit`, etc.

The `StateContext` performs multiple roles such as passing `Parameter` information between states, holding a event `Message` payload, informing which state to transition to next, and invoking Logging (`LogInfo`, `LogDebug`, `LogError`).

The fluent State builder includes the following features:

* `.AllowNext(int stateId)`         - Allowable state transition.
  * Used for GraphViz generator.
  * If not defined, any the user could for `Next(stateId)` to virtually anywhere.
* `.Timeout(int milliseconds)`      - Timeout duration (default=0). If not defined, the default state will be used.
* `.MessageType(int messageTypeId)` - Optional event Message filter.

## Class Interfaces

```cpp
class StateTemplate
{
  public:
    int Id;
    std::string Name;

    // Handlers
    virtual void OnEnter(StateContext*);
    virtual void OnMessage(StateContext*);  // or, "(StateContext*, Message*)
    virtual void OnTimeout(StateContext*);
    virtual void OnExit(StateContext*);
    virtual void OnError(StateContext*);
};

class StateContext
{
  Message

  void LogInfo(std::string);
  void LogDebug(std::string);
  void LogError(std::string);
}
```

## Sample

```cpp
enum StateId
{
  Uninitialized,
  Init,
  Opening,
  Opened,
  Closing,
  Closed,
  Error,
};

enum MessageType
{
  Unknown = 0,
  DoorOpened,   // Door opened sensor (0=Closed, 1=Opened)
  DoorLock,     // Door lock sensor (0=Unlocked, 1=Locked)
};

void main()
{
  StateMachine sm;

  sm.FailureState(StateId::Error, "Error", new ErrorState())
    .AllowNext(StateId::)

  sm.State(StateId::Uninitialized, "Unitialized", new Uninitialized())
    .Timeout(5000)
    .AllowNext(StateId::Initialize);

  sm.State(StateId::Init, "Initialize", new Initialize())
    .Timeout(5000)
    .AllowNext(StateId::Opened)
    .AllowNext(StateId::Closed);

  sm.State(StateId::Opened, "Opened", new Opened())
    .AllowNext(StateId::Opening);

  sm.State(StateId::Closed, "Closed", new Closed())
    .AllowNext(StateId::Closing);

  sm.State(StateId::Opening, "Opening", new Opening())
    .Timeout(5000)                          // Timeout after 5 seconds
    .AllowNext(StateId::Opened)             // Allow transitioning to Opened state
    .AllowNext(StateId::Closed)             // Allow transitioning to Closed state
    .MessageType(MessageType::DoorOpened);  // Filter incoming message types for "DoorOpened"

  sm.State(StateId::Closing, "Closing", new Closing())
    .Timeout(5000)                          // Timeout after 5 seconds
    .AllowNext(StateId::Opened)             // Allow transitioning to Opened state
    .AllowNext(StateId::Closed)             // Allow transitioning to Closed state
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
    context->Next(StateId::Init);
  }
}

class Initialize : State
{
  void OnEnter(StateContext* context)
  {
    if (0 == 0)
      context->Next(StateId::Opened);
    else
      context->Next(StateId::Closed)
  }
}

class Opening : State
{
  bool _opened = false;

  // Nothing to do here except logging.
  void OnEnter(StateContext* context)
  {
    context->LogDebug("Entered Door Opening state. Waiting on sensor feedback");
  }

  // Received event message
  void OnMessage(StateContext* context)
  {
    if (context->MessageType != MessageType::DoorOpened)
      return;

    // Received message from DoorOpened sensor
    if (context->MessageData == "1")
      context->Next(StateId::Opened);
    else
      context->Next(StateId::Closed);
  }

  // Door failed to open in the alloted time.
  void OnTimeout(StateContext* context)
  {
    context->LogError("Door failed to open in the allotted time.");
    context->Next(StateId::Closed);
  }
}
```
