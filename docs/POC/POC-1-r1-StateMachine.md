# LiteState PoC v1.1

## Core

```cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public enum StateId
{
    Root,
    Loading,
    Processing,
    Completed,
    Error
}

public class StateDefinition
{
    public StateId Id { get; }
    public Func<Dictionary<string, object>, Task>? OnEntering { get; set; }
    public Func<Dictionary<string, object>, Task>? OnEnter { get; set; }
    public Func<string, Dictionary<string, object>, Task>? OnMessage { get; set; }
    public Func<Dictionary<string, object>, Task>? OnTimeout { get; set; }
    public Func<Dictionary<string, object>, Task>? OnExit { get; set; }
    public List<StateDefinition> SubStates { get; } = new();

    public StateDefinition(StateId id) => Id = id;
}

public class AsyncStateMachine
{
    private readonly Dictionary<StateId, StateDefinition> _states = new();
    private StateDefinition? _currentState;
    private CancellationTokenSource? _timeoutCts;

    public void AddState(StateDefinition state) => _states[state.Id] = state;

    public async Task TransitionToAsync(StateId newStateId, Dictionary<string, object> context)
    {
        if (!_states.TryGetValue(newStateId, out var newState))
            throw new InvalidOperationException($"State {newStateId} not defined.");

        if (_currentState?.OnExit != null)
            await _currentState.OnExit(context);

        _timeoutCts?.Cancel();

        if (newState.OnEntering != null)
            await newState.OnEntering(context);

        _currentState = newState;

        if (newState.OnEnter != null)
            await newState.OnEnter(context);

        if (newState.OnTimeout != null)
        {
            _timeoutCts = new CancellationTokenSource();
            _ = Task.Delay(TimeSpan.FromSeconds(10), _timeoutCts.Token)
                .ContinueWith(async t =>
                {
                    if (!t.IsCanceled)
                        await newState.OnTimeout(context);
                });
        }
    }

    public async Task SendMessageAsync(string message, Dictionary<string, object> context)
    {
        if (_currentState?.OnMessage != null)
            await _currentState.OnMessage(message, context);
    }
}
```

## Sample

```cs

var fsm = new AsyncStateMachine();

var loadingState = new StateDefinition(StateId.Loading)
{
    OnEntering = async ctx => Console.WriteLine("Entering Loading..."),
    OnEnter = async ctx => Console.WriteLine("Now in Loading."),
    OnTimeout = async ctx => Console.WriteLine("Loading timed out."),
    OnExit = async ctx => Console.WriteLine("Exiting Loading.")
};

var processingState = new StateDefinition(StateId.Processing)
{
    OnEnter = async ctx => Console.WriteLine("Processing started."),
    OnMessage = async (msg, ctx) => Console.WriteLine($"Received message: {msg}")
};

fsm.AddState(loadingState);
fsm.AddState(processingState);

var context = new Dictionary<string, object>();
await fsm.TransitionToAsync(StateId.Loading, context);
await Task.Delay(2000);
await fsm.TransitionToAsync(StateId.Processing, context);
await fsm.SendMessageAsync("Hello FSM!", context);
```
