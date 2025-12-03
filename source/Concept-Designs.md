
# Concept Designs

## MK-1 - Defining States - Attribute Driven

Details:

* Sub-states are supported via calling a new class with attributes

### Features

* Does not use State Triggers
* Next state is explicitly defined, eliminating ambiguous trigger calls.
* State Attribute are defined on each method
* Context is a dictionary which users can add to

### Pseudo States

* void OnEnter(Context c)
* void OnMessage(Context c)
* void OnTimeout(Context c)  `(Need to define timeout in the attribute)`
* void OnExit(Context c)

```cs
// Pseudo States
enum StateType
{
  OnEnter,
  OnMessage,
  OnTimeout,
  OnExit,
}
```

### Attribute Definitions

```cs
[State(int stateName, int pseudoState, )]
```

### Sample 1 - Attribute Defined States

```cs
[State("Kitchen Phone")]
public class Phone
{
  enum PhoneState
  {
    Init,
    LoadData,
    LoadDataFaulted,
    Idle,
    Ringing,
    Answered,
    HangUp,
    SystemUpdate,
    PowerDown,
  }

  private readonly LogService _log;
  private readonly UpdaterService _updater;
  private bool _powerDownAfterUpdate = false;

  public Phone(UpdateService updater, LogService log)
  {
    _updater = updater;
    _log = log;

    _useUpdaterSubStates = true;
  }

  [State(PhoneState.Init, StateType.OnEnter, nextState = PhoneState.LoadData)]
  void Init_OnEnter(Context c)
  {
    // Initialize
  }

  [State(PhoneState.LoadData, StateType.OnEnter, nextState = PhoneState.Idle)]
  void LoadData_OnEnter(Context c)
  {
    // Load this phone number for caller id
    if (_db.CallerId is null)
      StateMachine.NextState = PhoneState.LoadDataFaulted;
  }

  [State(PhoneState.Idle)]
  void Idle_OnEnter(Context c, nextState = PhoneState.Idle)
  {
    // Also, not supplying "nextState" in the attribute
    // will tell the system to stay here in this state
  }

  [State(PhoneState.Idle, StateType.OnMessage)]
  void Idle_OnMessage(Context context)
  {
    if (context.Data.ContainsKey("Ringing"))
    {
      //// StateMachine.NextState = PhoneState.Ringing;
      context.NextState = PhoneState.Ringing;
    }
    else if (context.Data.ContainsKey("PendingUpdate"))
    {
      // Set the next state to PhoneState.Update
    }
    else if (context.Data.ContainsKey("TurnOff") && context.Data["TurnOff"] == true)
    {
      //// StateMachine.NextState = PhoneState.PowerDown;
      context.NextState = PhoneState.PowerDown;
    }
  }

  [State(PhoneState.Idle, StateType.OnExit)]
  void Idle_OnExit(Context c)
  {
    Console.WriteLine("Exiting Idle State");
    Console.WriteLine($"Next State: {StateMachine.NextState}");
    // Don't need to supply the next state, we already know where we're going
  }

  [State(PhoneState.Ringing, StateType.OnEnter)]
  void Ringing_OnEnter(Context context)
  {
    if (context.Data.ContainsKey("IncomingCall"))
    {
      var newCall = (CallerInfo)context.Data["IncomingCall"];
      var callerNumber = newCall.PhoneNumber;

      // NOTE: We never set the next state, therefore, never picked up
    }
  }

  [State(PhoneState.Ringing, StateType.OnTimeout, timeout = 10000)]
  void Ringing_OnTimeout(Context context)
  {
    // Never picked up
    StateMachine.NextState = PhoneState.HangUp;
  }

  [State(PhoneState.HangUp, StateType.OnEnter, nextState = PhoneState.Idle)]
  void HangUp_OnEnter(Context c)
  {
    // Do whatever cleanup
    // Return back to Idle
  }

  [State(PhoneState.PowerDown, StateType.OnEnter)]
  void PowerDown_OnEnter(Context c)
  {
    ////if (_isSystemUpdating)
    ////{
    ////  // Abort power-down
    ////  _powerDownAfterUpdate = true;
    ////  context.NextState = PhoneState.SystemUpdate;
    ////}
  }


  [State(PhoneState.SystemUpdate, StateType.OnEnter)]
  Task SystemUpdating_OnEnterAsync(Context c)
  {
    // Start updater sub-states
    if (_useUpdaterSubStates)
    {
      var updater = new Updater();
      var success = await updater.UpdateAsync();
    }
    else
    {
      // Use DependencyInjection service
      await _updater.UpdateAsync();
    }

    Console.WriteLine($"Successful: {success}");

    if (!_powerDownAfterUpdate)
      c.NextState = PhoneState.Idle;
    else
      c.NextState = PhoneState.PowerDown;
  }

  ////[State(PhoneState.SystemUpdating, StateType.OnMessage)]
  ////void SystemUpdating_OnMessage(Context c)
  ////{
  ////  // Wait for service
  ////  c.Date.
  ////}
}
```

## MK-2 SubState Class

Questions:

* Create methodology to cleanly exit the substate after it completes all of it's duties

```cs
public enum Kitchen
{
  Init,
  CloseAllCabinets,
  OpenCabinet,

}

[SubState(Kitchen.CloseAllCabinets, description = "Sample SubState 1")]
public class CloseAllCabinets : Arm
{
  public LeftArm()
  {
  }

  public override void OnEntry()
  {
    // Entrypoint
    // Optionally define substates
    // Begin substate action
  }

  public override void OnEnter()
  {
  }

  public override void OnExit()
  {
    // Return back to parent state
  }

  [State(CloseCabinet.)]
}

[SubState()]

```