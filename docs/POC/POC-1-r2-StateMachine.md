# LiteState PoC v1.2

## Core

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public enum StateId
{
    Root,
    Loading,
    Processing,
    SubProcessing,
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

    public StateDefinition(StateId id) => Id = id;
}

public class CompositeState : StateDefinition
{
    public Dictionary<StateId, StateDefinition> SubStates { get; } = new();
    public StateId? InitialSubState { get; set; }

    public CompositeState(StateId id) : base(id) { }

    public void AddSubState(StateDefinition state) => SubStates[state.Id] = state;
}

public class AsyncStateMachine
{
    private readonly Dictionary<StateId, StateDefinition> _states = new();
    private StateDefinition? _currentState;
    private StateDefinition? _currentSubState;
    private CancellationTokenSource? _timeoutCts;
    private readonly Channel<(string message, Dictionary<string, object> context)> _messageChannel =
        Channel.CreateUnbounded<(string, Dictionary<string, object>)>();

    public AsyncStateMachine()
    {
        _ = ProcessMessagesAsync();
    }

    public void AddState(StateDefinition state) => _states[state.Id] = state;

    public async Task TransitionToAsync(StateId newStateId, Dictionary<string, object> context)
    {
        if (!_states.TryGetValue(newStateId, out var newState))
            throw new InvalidOperationException($"State {newStateId} not defined.");

        await ExitCurrentStateAsync(context);

        if (newState.OnEntering != null)
            await newState.OnEntering(context);

        _currentState = newState;

        if (newState is CompositeState composite && composite.InitialSubState.HasValue)
        {
            await TransitionToSubStateAsync(composite.InitialSubState.Value, context);
        }

        if (newState.OnEnter != null)
            await newState.OnEnter(context);

        SetupTimeout(newState, context);
    }

    private async Task TransitionToSubStateAsync(StateId subStateId, Dictionary<string, object> context)
    {
        if (_currentState is CompositeState composite && composite.SubStates.TryGetValue(subStateId, out var subState))
        {
            _currentSubState = subState;
            if (subState.OnEntering != null)
                await subState.OnEntering(context);
            if (subState.OnEnter != null)
                await subState.OnEnter(context);
            SetupTimeout(subState, context);
        }
    }

    private async Task ExitCurrentStateAsync(Dictionary<string, object> context)
    {
        if (_currentSubState?.OnExit != null)
            await _currentSubState.OnExit(context);
        if (_currentState?.OnExit != null)
            await _currentState.OnExit(context);
        _timeoutCts?.Cancel();
    }

    private void SetupTimeout(StateDefinition state, Dictionary<string, object> context)
    {
        if (state.OnTimeout != null)
        {
            _timeoutCts = new CancellationTokenSource();
            _ = Task.Delay(TimeSpan.FromSeconds(10), _timeoutCts.Token)
                .ContinueWith(async t =>
                {
                    if (!t.IsCanceled)
                        await state.OnTimeout(context);
                });
        }
    }

    public async Task SendMessageAsync(string message, Dictionary<string, object> context)
    {
        await _messageChannel.Writer.WriteAsync((message, context));
    }

    private async Task ProcessMessagesAsync()
    {
        await foreach (var (message, context) in _messageChannel.Reader.ReadAllAsync())
        {
            if (_currentSubState?.OnMessage != null)
                await _currentSubState.OnMessage(message, context);
            else if (_currentState?.OnMessage != null)
                await _currentState.OnMessage(message, context);
        }
    }
}
```

## Sample 

```
var fsm = new AsyncStateMachine();

var rootState = new CompositeState(StateId.Root)
{
    OnEnter = async ctx => Console.WriteLine("Root entered."),
    InitialSubState = StateId.Loading
};

var loadingState = new StateDefinition(StateId.Loading)
{
    OnEnter = async ctx => Console.WriteLine("Loading..."),
    OnTimeout = async ctx => Console.WriteLine("Loading timed out."),
    OnExit = async ctx => Console.WriteLine("Exiting Loading.")
};

var processingState = new CompositeState(StateId.Processing)
{
    OnEnter = async ctx => Console.WriteLine("Processing started."),
    InitialSubState = StateId.SubProcessing
};

var subProcessingState = new StateDefinition(StateId.SubProcessing)
{
    OnEnter = async ctx => Console.WriteLine("Sub-processing started."),
    OnMessage = async (msg, ctx) => Console.WriteLine($"SubProcessing received: {msg}")
};

processingState.AddSubState(subProcessingState);
rootState.AddSubState(loadingState);
rootState.AddSubState(processingState);

fsm.AddState(rootState);
fsm.AddState(processingState);

var context = new Dictionary<string, object>();

await fsm.TransitionToAsync(StateId.Root, context);
await Task.Delay(2000);
await fsm.TransitionToAsync(StateId.Processing, context);
await fsm.SendMessageAsync("Hello from FSM!", context);
```
