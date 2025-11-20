## Prompt

Using C#, create a finite state machine which can optionally have composite states, with each state defined as an Enum. States must be lazy-loaded. Defining states must use the State enumeration and a human-readable name.

Each state's transition must be an async. The state can has an optional transition of OnEntering for transitioning into it, OnEnter for when fully transitioned, optionally an OnMessage for receiving messages sent by the OnEnter, optionally an OnTimeout for when events are not received by the OnMessage in time, and optionally an OnExit for when it is completed.

Each transition must pass a Context class as an argument which contains a property named "Params" of type "Dictionary<string, object>", property named "Errors" of type "Dictionary<string, object>", a property "LastState" which has the enum value of the previous state, and the method "NextState" to trigger moving to the next state. The "NextState" method has an enum argument named "Result" with the values of "Success", "Error", and "Failure". The NextState method can be called by any of the transitions to move to the next state.

### Enhancements

* Transitions must be method, not "Func<Context, Task>"
* Composite states to support deep hierarchies. Provide a strongly-typed Params wrapper (e.g., Context.Get<T>(key) / Set(key, value)).
* Add logging hooks (ILogger integration) via DependencyInjection in StateNode class

## Core
Awesome—let’s add **`ILogger` integration via Dependency Injection** directly into the `StateNode` class and keep everything lazy-loaded and enum-driven.

Below is a complete, drop-in **C# FSM framework** updated to:

*   Inject `ILogger` into **each state** (`StateNode`) via **DI**.
*   Provide **sealed public transition methods** that do logging and call **protected virtual core methods** (`OnEnterAsyncCore`, etc.), so logging happens **even if derived states override behavior**.
*   Keep **lazy-loaded** states via DI (`ActivatorUtilities.CreateInstance<TState>(IServiceProvider)`).
*   Support **composite states** (parent hosting a sub-FSM).
*   Maintain your **Context** and **Result** contracts.

> Uses `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging`.

***

## Enums

```csharp
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
```

***

## Context (unchanged API)

```csharp
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
```

***

## StateNode with ILogger (DI-aware, with logging hooks)

> **Key pattern**: Public transition methods are `sealed` and perform logging. You override the protected `*Core` methods for behavior. This guarantees consistent logging across all states.

```csharp
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public abstract class StateNode
{
    /// <summary>Human-readable name for diagnostics/logging.</summary>
    public string Name { get; }

    /// <summary>Logger injected via DI.</summary>
    protected ILogger Logger { get; }

    protected StateNode(string name, ILogger logger)
    {
        Name   = name ?? throw new System.ArgumentNullException(nameof(name));
        Logger = logger ?? NullLogger.Instance;
    }

    // ---------- Transition methods (sealed) ----------
    public sealed async Task OnEnteringAsync(Context ctx)
    {
        Logger.LogTrace("Entering state '{StateName}' (OnEntering). LastState={LastState}", Name, ctx.LastState);
        await OnEnteringAsyncCore(ctx).ConfigureAwait(false);
        Logger.LogTrace("Entered state '{StateName}' (OnEntering finished).", Name);
    }

    public sealed async Task OnEnterAsync(Context ctx)
    {
        Logger.LogDebug("State '{StateName}' OnEnter starting.", Name);
        await OnEnterAsyncCore(ctx).ConfigureAwait(false);
        Logger.LogDebug("State '{StateName}' OnEnter finished.", Name);
    }

    public sealed async Task OnMessageAsync(Context ctx)
    {
        Logger.LogInformation("State '{StateName}' OnMessage received.", Name);
        await OnMessageAsyncCore(ctx).ConfigureAwait(false);
        Logger.LogInformation("State '{StateName}' OnMessage handled.", Name);
    }

    public sealed async Task OnTimeoutAsync(Context ctx)
    {
        Logger.LogWarning("State '{StateName}' Timeout triggered.", Name);
        await OnTimeoutAsyncCore(ctx).ConfigureAwait(false);
        Logger.LogWarning("State '{StateName}' Timeout handling completed.", Name);
    }

    public sealed async Task OnExitAsync(Context ctx)
    {
        Logger.LogTrace("Exiting state '{StateName}'.", Name);
        await OnExitAsyncCore(ctx).ConfigureAwait(false);
        Logger.LogTrace("Exited state '{StateName}'.", Name);
    }

    // ---------- Override these in derived states ----------
    protected virtual Task OnEnteringAsyncCore(Context ctx) => Task.CompletedTask;
    protected virtual Task OnEnterAsyncCore(Context ctx)    => Task.CompletedTask;
    protected virtual Task OnMessageAsyncCore(Context ctx)  => Task.CompletedTask;
    protected virtual Task OnTimeoutAsyncCore(Context ctx)  => Task.CompletedTask;
    protected virtual Task OnExitAsyncCore(Context ctx)     => Task.CompletedTask;
}
```

***

## Composite State (DI-aware, with ILogger)

```csharp
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public sealed class CompositeStateNode : StateNode
{
    private readonly FiniteStateMachine _subFsm;
    private readonly State _initialChild;

    public CompositeStateNode(
        string name,
        ILogger<CompositeStateNode> logger,
        FiniteStateMachine subFsm,
        State initialChild)
        : base(name, logger)
    {
        _subFsm = subFsm ?? throw new System.ArgumentNullException(nameof(subFsm));
        _initialChild = initialChild;
    }

    protected override async Task OnEnterAsyncCore(Context ctx)
    {
        Logger.LogInformation("Composite '{Name}' starting sub-FSM at child '{Child}'.", Name, _initialChild);

        var childResult = await _subFsm.StartAndWaitAsync(_initialChild, ctx.Params)
                                       .ConfigureAwait(false);

        Logger.LogInformation("Composite '{Name}' sub-FSM completed with Result={Result}. Passing to parent.", Name, childResult);
        await ctx.NextState(childResult).ConfigureAwait(false);
    }

    protected override Task OnMessageAsyncCore(Context ctx)
    {
        Logger.LogDebug("Composite '{Name}' forwarding message to child FSM.", Name);
        return _subFsm.ForwardMessageAsync(ctx.Params);
    }

    protected override Task OnTimeoutAsyncCore(Context ctx)
    {
        Logger.LogWarning("Composite '{Name}' forwarding timeout to child FSM.", Name);
        return _subFsm.ForwardTimeoutAsync();
    }
}
```

***

## Finite State Machine (DI-aware, lazy state resolution)

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public sealed class FiniteStateMachine
{
    private readonly IServiceProvider _services;
    private readonly ILogger<FiniteStateMachine> _logger;

    private readonly Dictionary<State, Lazy<StateNode>> _states = new();
    private readonly Dictionary<State, Dictionary<Result, State>> _transitions = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    private State _current = State.None;
    private TaskCompletionSource<Result>? _completionTcs;
    private readonly bool _isRoot;

    public FiniteStateMachine(IServiceProvider services,
                              ILogger<FiniteStateMachine> logger,
                              bool isRoot = true)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger   = logger   ?? throw new ArgumentNullException(nameof(logger));
        _isRoot   = isRoot;
    }

    /// <summary>Register a state lazily via DI (generic type).</summary>
    public void RegisterState<TState>(State state)
        where TState : StateNode
    {
        _states[state] = new Lazy<StateNode>(
            () => ActivatorUtilities.CreateInstance<TState>(_services),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Register a state lazily via a custom factory that uses DI.</summary>
    public void RegisterState(State state, Func<IServiceProvider, StateNode> factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));
        _states[state] = new Lazy<StateNode>(
            () => factory(_services),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Configure transitions for a state: Result → Next State.</summary>
    public void SetTransitions(State state, IDictionary<Result, State> map)
    {
        var dict = new Dictionary<Result, State>();
        foreach (var kvp in map)
            dict[kvp.Key] = kvp.Value;
        _transitions[state] = dict;
    }

    public async Task<Result> StartAndWaitAsync(State initial, IDictionary<string, object>? initialParams = null)
    {
        _completionTcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        await StartAsync(initial, initialParams).ConfigureAwait(false);
        return await _completionTcs.Task.ConfigureAwait(false);
    }

    public async Task StartAsync(State initial, IDictionary<string, object>? initialParams = null)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_current != State.None)
                throw new InvalidOperationException($"FSM already started at state '{_current}'.");

            _logger.LogInformation("FSM starting at state '{Initial}'.", initial);
            await TransitionToAsync(initial, BuildContext(initialParams)).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ForwardMessageAsync(IDictionary<string, object>? message = null)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_current == State.None) return;
            var node = _states[_current].Value;

            var ctx = BuildContext(message);
            _logger.LogDebug("Forwarding message to current state: {State}.", _current);
            await node.OnMessageAsync(ctx).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ForwardTimeoutAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_current == State.None) return;
            var node = _states[_current].Value;

            var ctx = BuildContext(null);
            _logger.LogWarning("Forwarding timeout to current state: {State}.", _current);
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

            _logger.LogInformation("Transition requested: {Current} --({Result})--> {Next}", _current, result, next);

            var ctx = BuildContext(null);
            await TransitionToAsync(next, ctx).ConfigureAwait(false);

            // If sub-FSM reached terminal state (no outgoing transitions), signal completion.
            if (_completionTcs is not null)
            {
                var hasOutgoing = _transitions.TryGetValue(_current, out var outgoing) && outgoing.Count > 0;
                if (!hasOutgoing)
                {
                    _logger.LogInformation("FSM reached terminal state '{State}'. Completing with result '{Result}'.", _current, result);
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

        // Exit previous if it was created
        if (prev != State.None && _states.TryGetValue(prev, out var prevLazy) && prevLazy.IsValueCreated)
        {
            var prevNode = prevLazy.Value;
            _logger.LogTrace("Exiting state '{Prev}'.", prev);
            await prevNode.OnExitAsync(ctx).ConfigureAwait(false);
        }

        // Switch current
        _current = newState;
        var node = lazyNode.Value;

        _logger.LogTrace("Transitioning to state '{New}'.", newState);

        // Enter lifecycle
        await node.OnEnteringAsync(ctx).ConfigureAwait(false);
        await node.OnEnterAsync(ctx).ConfigureAwait(false);
    }
}
```

***

## Example States with DI Logging

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public sealed class InitState : StateNode
{
    public InitState(ILogger<InitState> logger)
        : base("Initialization", logger) { }

    protected override async Task OnEnterAsyncCore(Context ctx)
    {
        Logger.LogInformation("[Init] OnEnter");
        ctx.Params["startedAt"] = DateTime.UtcNow;
        await ctx.NextState(Result.Success);
    }
}

public sealed class LoadingState : StateNode
{
    public LoadingState(ILogger<LoadingState> logger)
        : base("Loading", logger) { }

    protected override async Task OnEnteringAsyncCore(Context ctx)
    {
        Logger.LogDebug("[Loading] OnEntering - preparing I/O...");
        await Task.Delay(50);
    }

    protected override async Task OnEnterAsyncCore(Context ctx)
    {
        Logger.LogInformation("[Loading] OnEnter - fetching resources...");
        await Task.Delay(100);
        await ctx.NextState(Result.Success);
    }
}

public sealed class ProcessingState : StateNode
{
    public ProcessingState(ILogger<ProcessingState> logger)
        : base("Processing", logger) { }

    protected override async Task OnEnterAsyncCore(Context ctx)
    {
        Logger.LogInformation("[Processing] OnEnter - start processing");
        await Task.Delay(100);
        await OnMessageAsyncCore(ctx); // demo: self-trigger message
    }

    protected override async Task OnMessageAsyncCore(Context ctx)
    {
        Logger.LogInformation("[Processing] OnMessage - received event");
        await ctx.NextState(Result.Success);
    }

    protected override async Task OnTimeoutAsyncCore(Context ctx)
    {
        Logger.LogWarning("[Processing] OnTimeout - no events received in time");
        ctx.Errors["timeout"] = "Processing timed out.";
        await ctx.NextState(Result.Failure);
    }
}

public sealed class CompletedState : StateNode
{
    public CompletedState(ILogger<CompletedState> logger)
        : base("Completed", logger) { }

    protected override Task OnEnterAsyncCore(Context ctx)
    {
        Logger.LogInformation("[Completed] OnEnter - done.");
        return Task.CompletedTask;
    }
}

public sealed class ErrorState : StateNode
{
    public ErrorState(ILogger<ErrorState> logger)
        : base("Error", logger) { }

    protected override Task OnEnterAsyncCore(Context ctx)
    {
        Logger.LogError("[Error] OnEnter - error occurred.");
        foreach (var e in ctx.Errors)
            Logger.LogError("  {Key}: {Value}", e.Key, e.Value);
        return Task.CompletedTask;
    }
}
```

***

## Demo Wiring with Dependency Injection

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class Demo
{
    public static async Task RunAsync()
    {
        // Build DI container
        var services = new ServiceCollection()
            .AddLogging(b =>
            {
                b.AddConsole();
                b.SetMinimumLevel(LogLevel.Trace);
            })
            // Register state types for DI (transient is typical)
            .AddTransient<InitState>()
            .AddTransient<LoadingState>()
            .AddTransient<ProcessingState>()
            .AddTransient<CompletedState>()
            .AddTransient<ErrorState>()
            .AddTransient<CompositeStateNode>() // if you want DI to be able to create composites generically
            .BuildServiceProvider();

        // Create FSMs with DI + loggers
        var rootLogger = services.GetRequiredService<ILogger<FiniteStateMachine>>();
        var fsm = new FiniteStateMachine(services, rootLogger, isRoot: true);

        var childLogger = services.GetRequiredService<ILogger<FiniteStateMachine>>();
        var subFsm = new FiniteStateMachine(services, childLogger, isRoot: false);

        // Register child states lazily via DI
        subFsm.RegisterState<LoadingState>(State.Loading);
        subFsm.RegisterState<ProcessingState>(State.Processing);
        subFsm.RegisterState<CompletedState>(State.Completed);

        subFsm.SetTransitions(State.Loading, new Dictionary<Result, State>
        {
            [Result.Success] = State.Processing,
            [Result.Error]   = State.Completed,
            [Result.Failure] = State.Completed
        });

        subFsm.SetTransitions(State.Processing, new Dictionary<Result, State>
        {
            [Result.Success] = State.Completed,
            [Result.Error]   = State.Completed,
            [Result.Failure] = State.Completed
        });

        // Register composite state on root via factory (so we can pass sub-FSM and initial child)
        fsm.RegisterState(State.OrderFlow, sp =>
            new CompositeStateNode(
                name: "Order Flow",
                logger: sp.GetRequiredService<ILogger<CompositeStateNode>>(),
                subFsm: subFsm,
                initialChild: State.Loading));

        // Register other root states via DI
        fsm.RegisterState<InitState>(State.Init);
        fsm.RegisterState<CompletedState>(State.Completed);
        fsm.RegisterState<ErrorState>(State.Error);

        // Wiring root transitions
        fsm.SetTransitions(State.Init, new Dictionary<Result, State>
        {
            [Result.Success] = State.OrderFlow,
            [Result.Error]   = State.Error,
            [Result.Failure] = State.Error
        });

        fsm.SetTransitions(State.OrderFlow, new Dictionary<Result, State>
        {
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

**Entry point:**

```csharp
public static class Program
{
    public static async Task Main() => await Demo.RunAsync();
}
```

***

## Notes & Tips

*   **DI Logging**: Each state receives a typed `ILogger<TState>` via DI, then passes it to the base `StateNode`. The base ensures **consistent logging** before/after every transition method.
*   **Lazy-loading**: States are only created when first entered (`Lazy<StateNode>` + DI `ActivatorUtilities.CreateInstance`).
*   **Composite states**: `CompositeStateNode` drives a sub-FSM and forwards messages/timeouts with logs.
*   **Terminal detection**: The FSM completes when the current state has **no outgoing transitions** configured.
*   **Extending**:
    *   You can add per-state **timeouts** by scheduling a timer that calls `ForwardTimeoutAsync()`.
    *   If you want **structured scopes**, wrap each transition in a `using (Logger.BeginScope(...))`.

***

Would you like me to add **per-state `BeginScope` contexts** and a simple **timeout scheduler** (e.g., `CancellationTokenSource` per state) so timeouts are automatic unless disabled?
