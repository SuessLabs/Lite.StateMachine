## Prompt

Using C#, create a finite state machine which can optionally have composite states, with each state defined as an Enum. States must be lazy-loaded. Defining states must use the State enumeration and a human-readable name.

Each state's transition must be an async. The state can has an optional transition of OnEntering for transitioning into it, OnEnter for when fully transitioned, optionally an OnMessage for receiving messages sent by the OnEnter, optionally an OnTimeout for when events are not received by the OnMessage in time, and optionally an OnExit for when it is completed.

Each transition must pass a Context class as an argument which contains a property named "Params" of type "Dictionary<string, object>", property named "Errors" of type "Dictionary<string, object>", a property "LastState" which has the enum value of the previous state, and the method "NextState" to trigger moving to the next state. The "NextState" method has an enum argument named "Result" with the values of "Success", "Error", and "Failure". The NextState method can be called by any of the transitions to move to the next state.

### Enhancements

* Transitions must be method, not "Func<Context, Task>"

## Core

```
public enum State
{
    None,
    Init,
    Loading,
    Processing,
    Completed,
    Error,

    // Example composite "parent"
    OrderFlow
}

public enum Result
{
    Success,
    Error,
    Failure
}

// Context

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class Context
{
    public Dictionary<string, object> Params { get; } = new();
    public Dictionary<string, object> Errors { get; } = new();

    /// <summary>The previous state's enum value.</summary>
    public State LastState { get; internal set; } = State.None;

    private readonly Func<Result, Task> _nextState;

    public Context(Func<Result, Task> nextState)
    {
        _nextState = nextState ?? throw new ArgumentNullException(nameof(nextState));
    }

    /// <summary>Triggers moving to the next state, based on a Result.</summary>
    public Task NextState(Result result) => _nextState(result);
}

// State node (transition methods)
using System.Threading.Tasks;

public abstract class StateNode
{
    /// <summary>Human-readable name for diagnostics/logging.</summary>
    public string Name { get; }

    protected StateNode(string name) => Name = name;

    /// <summary>Called when transitioning into this state (pre-activation).</summary>
    public virtual Task OnEnteringAsync(Context ctx) => Task.CompletedTask;

    /// <summary>Called when fully transitioned/activated.</summary>
    public virtual Task OnEnterAsync(Context ctx) => Task.CompletedTask;

    /// <summary>Called for messages/events while in this state.</summary>
    public virtual Task OnMessageAsync(Context ctx) => Task.CompletedTask;

    /// <summary>Called when a timeout occurs while waiting for messages.</summary>
    public virtual Task OnTimeoutAsync(Context ctx) => Task.CompletedTask;

    /// <summary>Called when the state is being exited.</summary>
    public virtual Task OnExitAsync(Context ctx) => Task.CompletedTask;
}

// Composite State
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class CompositeStateNode : StateNode
{
    private readonly FiniteStateMachine _subFsm;
    private readonly State _initialChild;

    public CompositeStateNode(string name, FiniteStateMachine subFsm, State initialChild) : base(name)
    {
        _subFsm = subFsm ?? throw new ArgumentNullException(nameof(subFsm));
        _initialChild = initialChild;
    }

    public override async Task OnEnterAsync(Context ctx)
    {
        // Start the sub-FSM; when it completes, determine parent outcome.
        var childResult = await _subFsm.StartAndWaitAsync(_initialChild, ctx.Params);

        // You can choose how to map childResult to parent result; pass-through here:
        await ctx.NextState(childResult);
    }

    public override Task OnMessageAsync(Context ctx)
    {
        // Optionally forward messages to the child FSM (if it's running):
        return _subFsm.ForwardMessageAsync(ctx.Params);
    }

    public override Task OnTimeoutAsync(Context ctx)
    {
        // Optionally forward timeout handling to the child FSM:
        return _subFsm.ForwardTimeoutAsync();
    }
}


// FSM Core

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class FiniteStateMachine
{
    private readonly Dictionary<State, Lazy<StateNode>> _states = new();
    private readonly Dictionary<State, Dictionary<Result, State>> _transitions = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    private State _current = State.None;
    private TaskCompletionSource<Result>? _completionTcs;
    private bool _isRoot = true; // Used to differentiate sub-FSM instances

    public FiniteStateMachine(bool isRoot = true)
    {
        _isRoot = isRoot;
    }

    /// <summary>Register a state lazily. The factory will be called on first access.</summary>
    public void RegisterState(State state, Func<StateNode> factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));
        _states[state] = new Lazy<StateNode>(factory, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Configure transitions for a state: Result → Next State.</summary>
    public void SetTransitions(State state, IDictionary<Result, State> map)
    {
        if (!_transitions.TryGetValue(state, out var dict))
        {
            dict = new Dictionary<Result, State>();
            _transitions[state] = dict;
        }

        dict.Clear();
        foreach (var kvp in map)
        {
            dict[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>Starts the FSM and returns when a terminal transition completes (by calling NextState).</summary>
    public async Task<Result> StartAndWaitAsync(State initial, IDictionary<string, object>? initialParams = null)
    {
        _completionTcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        await StartAsync(initial, initialParams);
        return await _completionTcs.Task.ConfigureAwait(false);
    }

    /// <summary>Starts the FSM without waiting for completion.</summary>
    public async Task StartAsync(State initial, IDictionary<string, object>? initialParams = null)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_current != State.None)
                throw new InvalidOperationException($"FSM already started at state '{_current}'.");

            await TransitionToAsync(initial, BuildContext(initialParams)).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Forward a message to the current state (useful for composite parent to child).</summary>
    public async Task ForwardMessageAsync(IDictionary<string, object>? message = null)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_current == State.None) return;
            var node = _states[_current].Value;

            var ctx = BuildContext(message);
            await node.OnMessageAsync(ctx).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Forward a timeout event to the current state.</summary>
    public async Task ForwardTimeoutAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_current == State.None) return;
            var node = _states[_current].Value;

            var ctx = BuildContext(null);
            await node.OnTimeoutAsync(ctx).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private Context BuildContext(IDictionary<string, object>? initialParams)
    {
        var ctx = new Context(NextStateAsync);
        if (initialParams is not null)
        {
            foreach (var kvp in initialParams)
                ctx.Params[kvp.Key] = kvp.Value;
        }
        ctx.LastState = _current;
        return ctx;
    }

    private async Task NextStateAsync(Result result)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_current == State.None)
                throw new InvalidOperationException("No active state to transition from.");

            if (!_transitions.TryGetValue(_current, out var map) || !map.TryGetValue(result, out var next))
                throw new KeyNotFoundException($"No transition defined from '{_current}' on result '{result}'.");

            var ctx = BuildContext(null);
            await TransitionToAsync(next, ctx).ConfigureAwait(false);

            // If this FSM is a sub-FSM, signal completion when it reaches a terminal state (no outgoing transitions).
            if (_completionTcs is not null)
            {
                var hasOutgoing = _transitions.TryGetValue(_current, out var outgoing) && outgoing.Count > 0;
                if (!hasOutgoing)
                {
                    _completionTcs.TrySetResult(result);
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task TransitionToAsync(State newState, Context ctx)
    {
        if (!_states.TryGetValue(newState, out var lazyNode))
            throw new InvalidOperationException($"State '{newState}' is not registered.");

        var prev = _current;

        // Exit previous (if created)
        if (prev != State.None && _states.TryGetValue(prev, out var prevLazy) && prevLazy.IsValueCreated)
        {
            var prevNode = prevLazy.Value;
            await prevNode.OnExitAsync(ctx).ConfigureAwait(false);
        }

        // Switch current
        _current = newState;

        var node = lazyNode.Value;

        // Enter lifecycle
        await node.OnEnteringAsync(ctx).ConfigureAwait(false);
        await node.OnEnterAsync(ctx).ConfigureAwait(false);
    }
}
```

## Example

```cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class InitState : StateNode
{
    public InitState() : base("Initialization") { }

    public override async Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Init] OnEnter");
        // Setup params or do work
        ctx.Params["startedAt"] = DateTime.UtcNow;

        // Transition to next
        await ctx.NextState(Result.Success);
    }
}

public sealed class LoadingState : StateNode
{
    public LoadingState() : base("Loading") { }

    public override async Task OnEnteringAsync(Context ctx)
    {
        Console.WriteLine("[Loading] OnEntering - preparing I/O...");
        await Task.Delay(50);
    }

    public override async Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Loading] OnEnter - fetching resources...");
        await Task.Delay(100);
        await ctx.NextState(Result.Success);
    }
}

public sealed class ProcessingState : StateNode
{
    public ProcessingState() : base("Processing") { }

    public override async Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Processing] OnEnter - start processing");
        await Task.Delay(100);

        // Send a message to self (for demo, could be external trigger)
        await OnMessageAsync(ctx);
    }

    public override async Task OnMessageAsync(Context ctx)
    {
        Console.WriteLine("[Processing] OnMessage - received event");
        // Simulate success outcome
        await ctx.NextState(Result.Success);
    }

    public override async Task OnTimeoutAsync(Context ctx)
    {
        Console.WriteLine("[Processing] OnTimeout - no events received in time");
        ctx.Errors["timeout"] = "Processing timed out.";
        await ctx.NextState(Result.Failure);
    }
}

public sealed class CompletedState : StateNode
{
    public CompletedState() : base("Completed") { }

    public override Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Completed] OnEnter - done.");
        return Task.CompletedTask;
    }
}

public sealed class ErrorState : StateNode
{
    public ErrorState() : base("Error") { }

    public override Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Error] OnEnter - error occurred.");
        foreach (var e in ctx.Errors)
        {
            Console.WriteLine($"  {e.Key}: {e.Value}");
        }
        return Task.CompletedTask;
    }
}

## Demo wiring

```cs
public static async Task Main()
{
    await Demo.RunAsync();
}

public static class Demo
{
    public static async Task RunAsync()
    {
        // Sub-FSM for the composite state "OrderFlow"
        var subFsm = new FiniteStateMachine(isRoot: false);
        subFsm.RegisterState(State.Loading, () => new LoadingState());
        subFsm.RegisterState(State.Processing, () => new ProcessingState());
        subFsm.RegisterState(State.Completed, () => new CompletedState());

        subFsm.SetTransitions(State.Loading, new Dictionary<Result, State>
        {
            [Result.Success] = State.Processing,
            [Result.Error]   = State.Completed,   // Example alternate
            [Result.Failure] = State.Completed
        });

        subFsm.SetTransitions(State.Processing, new Dictionary<Result, State>
        {
            [Result.Success] = State.Completed,
            [Result.Error]   = State.Completed,
            [Result.Failure] = State.Completed
        });

        // Composite State that runs the sub-FSM
        var orderFlowComposite = new CompositeStateNode("Order Flow", subFsm, State.Loading);

        // Root FSM
        var fsm = new FiniteStateMachine();
        fsm.RegisterState(State.Init,      () => new InitState());
        fsm.RegisterState(State.OrderFlow, () => orderFlowComposite);
        fsm.RegisterState(State.Completed, () => new CompletedState());
        fsm.RegisterState(State.Error,     () => new ErrorState());

        // Wiring transitions on root FSM
        fsm.SetTransitions(State.Init, new Dictionary<Result, State>
        {
            [Result.Success] = State.OrderFlow,
            [Result.Error]   = State.Error,
            [Result.Failure] = State.Error
        });

        fsm.SetTransitions(State.OrderFlow, new Dictionary<Result, State>
        {
            // Pass-through: child Completed => parent Success => root Completed
            [Result.Success] = State.Completed,
            [Result.Error]   = State.Error,
            [Result.Failure] = State.Error
        });

        // Start the root FSM and wait for terminal outcome
        var finalResult = await fsm.StartAndWaitAsync(State.Init);
        Console.WriteLine($"Root FSM finished with result: {finalResult}");
    }
}
```
