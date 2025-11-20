## Prompt

Using C#, create a finite state machine which can optionally have composite states, with each state defined as an Enum. States must be lazy-loaded. Defining states must use the State enumeration and a human-readable name.

Each state's transition must be an async. The state can has an optional transition of OnEntering for transitioning into it, OnEnter for when fully transitioned, optionally an OnMessage for receiving messages sent by the OnEnter, optionally an OnTimeout for when events are not received by the OnMessage in time, and optionally an OnExit for when it is completed.

Each transition must pass a Context class as an argument which contains a property named "Params" of type "Dictionary<string, object>", property named "Errors" of type "Dictionary<string, object>", a property "LastState" which has the enum value of the previous state, and the method "NextState" to trigger moving to the next state. The "NextState" method has an enum argument named "Result" with the values of "Success", "Error", and "Failure". The NextState method can be called by any of the transitions to move to the next state.

## Core

```cs
// Step 1: Define Enums
public enum State
{
    None,
    Init,
    Loading,
    Processing,
    Completed,
    Error
}

public enum Result
{
    Success,
    Error,
    Failure
}


public class Context
{
    public Dictionary<string, object> Params { get; } = new();
    public Dictionary<string, object> Errors { get; } = new();
    public State LastState { get; internal set; } = State.None;

    private readonly Func<Result, Task> _nextStateCallback;

    public Context(Func<Result, Task> nextStateCallback)
    {
        _nextStateCallback = nextStateCallback;
    }

    public Task NextState(Result result) => _nextStateCallback(result);
}

// State Definitions
public class StateDefinition
{
    public string Name { get; }
    public Func<Context, Task>? OnEntering { get; init; }
    public Func<Context, Task>? OnEnter { get; init; }
    public Func<Context, Task>? OnMessage { get; init; }
    public Func<Context, Task>? OnTimeout { get; init; }
    public Func<Context, Task>? OnExit { get; init; }

    public StateDefinition(string name)
    {
        Name = name;
    }
}

// Main state machine
public class FiniteStateMachine
{
    private readonly Dictionary<State, Lazy<StateDefinition>> _states = new();
    private State _currentState = State.None;

    public void RegisterState(State state, Func<StateDefinition> factory)
    {
        _states[state] = new Lazy<StateDefinition>(factory);
    }

    public async Task StartAsync(State initialState)
    {
        await TransitionToAsync(initialState, new Context(NextStateAsync));
    }

    private async Task NextStateAsync(Result result)
    {
        // Logic to determine next state based on result
        State nextState = result switch
        {
            Result.Success => State.Completed,
            Result.Error => State.Error,
            Result.Failure => State.Error,
            _ => State.Error
        };

        await TransitionToAsync(nextState, new Context(NextStateAsync));
    }

    private async Task TransitionToAsync(State newState, Context context)
    {
        if (!_states.ContainsKey(newState))
            throw new InvalidOperationException($"State {newState} is not registered.");

        var stateDef = _states[newState].Value;
        context.LastState = _currentState;

        // Exit old state
        if (_currentState != State.None && _states[_currentState].IsValueCreated)
        {
            var oldStateDef = _states[_currentState].Value;
            if (oldStateDef.OnExit != null)
                await oldStateDef.OnExit(context);
        }

        _currentState = newState;

        // Enter new state
        if (stateDef.OnEntering != null)
            await stateDef.OnEntering(context);

        if (stateDef.OnEnter != null)
            await stateDef.OnEnter(context);
    }
}
```

## Example

```cs
// =====================
// Example Usage

var fsm = new FiniteStateMachine();

// Register states lazily
fsm.RegisterState(State.Init, () => new StateDefinition("Initialization")
{
    OnEntering = async ctx => Console.WriteLine("Entering Init..."),
    OnEnter = async ctx =>
    {
        Console.WriteLine("Init complete.");
        await ctx.NextState(Result.Success);
    }
});

fsm.RegisterState(State.Completed, () => new StateDefinition("Completed")
{
    OnEnter = async ctx => Console.WriteLine("Process completed successfully.")
});

fsm.RegisterState(State.Error, () => new StateDefinition("Error")
{
    OnEnter = async ctx => Console.WriteLine("An error occurred.")
});

// Start FSM
await fsm.StartAsync(State.Init);
```