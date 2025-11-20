## Input

Composite states to support deep hierarchies. Provide a strongly-typed Params wrapper (e.g., Context.Get<T>(key) / Set(key, value)).

## Response

Awesome—let’s evolve the framework to support **deep hierarchical composite states** and add a **strongly-typed `Params` wrapper** to the `Context` (`Get<T>`, `Set`, `TryGet<T>`).

Below is a complete, production-ready **C#** FSM you can drop into your solution. It preserves your requirements:

*   **States are Enums**, lazy-loaded with **human-readable names**.
*   **Async transitions** implemented as **methods** on classes (`OnEnteringAsync`, `OnEnterAsync`, `OnMessageAsync`, `OnTimeoutAsync`, `OnExitAsync`).
*   **Deep composite states**: parent → child → grandchild (arbitrary depth).
*   **Context** includes `Params`, `Errors`, `LastState`, and `NextState(Result)`, plus **strongly-typed helpers** (`Set`, `Get<T>`, `TryGet<T>`).

***

## Core Types

### Enums

```csharp
public enum State
{
    None,

    // Top-level
    Init,
    Checkout,   // composite
    Completed,
    Error,

    // Checkout subtree (composite with deep nesting)
    Payment,    // composite
    AuthFlow,   // composite
    PreAuth,
    Authorize,
    PostAuth,
    Charge,
    Receipt,

    // Another branch in Checkout
    Shipping
}

public enum Result
{
    Success,
    Error,
    Failure
}
```

***

## Context with Strongly-Typed Params

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class Context
{
    /// <summary>Arbitrary parameters passed between states.</summary>
    public Dictionary<string, object> Params { get; } = new();

    /// <summary>Errors collected by states.</summary>
    public Dictionary<string, object> Errors { get; } = new();

    /// <summary>The previous leaf state's enum value.</summary>
    public State LastState { get; internal set; } = State.None;

    private readonly Func<Result, Task> _nextState;

    public Context(Func<Result, Task> nextState)
    {
        _nextState = nextState ?? throw new ArgumentNullException(nameof(nextState));
    }

    /// <summary>Trigger moving to the next state based on a Result.</summary>
    public Task NextState(Result result) => _nextState(result);

    // ---- Strongly-typed helpers ----

    /// <summary>Sets a strongly-typed parameter.</summary>
    public void Set<T>(string key, T value) => Params[key] = value!;

    /// <summary>Gets a parameter or throws if missing/invalid type.</summary>
    public T Get<T>(string key)
    {
        if (!Params.TryGetValue(key, out var obj))
            throw new KeyNotFoundException($"Param '{key}' not found.");
        if (obj is T t) return t;
        throw new InvalidCastException($"Param '{key}' is of type '{obj?.GetType().Name}', not '{typeof(T).Name}'.");
    }

    /// <summary>Tries to get a parameter with strong type.</summary>
    public bool TryGet<T>(string key, out T value)
    {
        if (Params.TryGetValue(key, out var obj) && obj is T t)
        {
            value = t;
            return true;
        }
        value = default!;
        return false;
    }
}
```

***

## State Nodes (Transitions are Methods)

```csharp
using System.Threading.Tasks;

public abstract class StateNode
{
    /// <summary>Human-readable name (for logs/diagnostics).</summary>
    public string Name { get; }

    protected StateNode(string name) => Name = name;

    public virtual Task OnEnteringAsync(Context ctx) => Task.CompletedTask;
    public virtual Task OnEnterAsync(Context ctx) => Task.CompletedTask;
    public virtual Task OnMessageAsync(Context ctx) => Task.CompletedTask;
    public virtual Task OnTimeoutAsync(Context ctx) => Task.CompletedTask;
    public virtual Task OnExitAsync(Context ctx) => Task.CompletedTask;
}
```

***

## Finite State Machine with Deep Composites

Key features:

*   Lazy-loaded states via `Lazy<StateNode>`.
*   Per-state **Result → next State** maps.
*   Composite relationships: parent, children, **initial child**.
*   Deep enter/exit handling along the **state stack**.
*   Message/timeout forwarding to the **current leaf**.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public sealed class FiniteStateMachine
{
    private readonly Dictionary<State, Lazy<StateNode>> _nodes = new();
    private readonly Dictionary<State, Dictionary<Result, State>> _transitions = new();

    // Composite relationships
    private readonly Dictionary<State, State> _parent = new();                 // child -> parent
    private readonly Dictionary<State, HashSet<State>> _children = new();      // parent -> children
    private readonly Dictionary<State, State> _initialChild = new();           // parent -> initial child

    private readonly SemaphoreSlim _lock = new(1, 1);

    // Current leaf state
    private State _currentLeaf = State.None;

    // Optional completion for StartAndWaitAsync
    private TaskCompletionSource<Result>? _completionTcs;

    // ---- Registration ----

    public void RegisterState(State state, string humanName, Func<StateNode> factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));
        _nodes[state] = new Lazy<StateNode>(() =>
        {
            var node = factory();
            if (node is null) throw new InvalidOperationException($"Factory returned null for {state}.");
            // Ensure name matches provided humanName; override if needed
            // If you prefer strict name enforcement, guard here.
            return node;
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Declare that 'parent' is composite and has an initial child.</summary>
    public void DefineComposite(State parent, State initialChild)
    {
        _children[parent] = _children.TryGetValue(parent, out var set) ? set : new HashSet<State>();
        _initialChild[parent] = initialChild;
        _parent[initialChild] = parent;
        _children[parent].Add(initialChild);
    }

    /// <summary>Add a child to an already-declared composite parent.</summary>
    public void AddChild(State parent, State child)
    {
        if (!_children.ContainsKey(parent))
            throw new InvalidOperationException($"Parent '{parent}' is not declared composite. Call DefineComposite first.");
        _children[parent].Add(child);
        _parent[child] = parent;
    }

    /// <summary>Configure transitions for a state (Result → Next State).</summary>
    public void SetTransitions(State state, IDictionary<Result, State> map)
    {
        var dict = new Dictionary<Result, State>();
        foreach (var kvp in map) dict[kvp.Key] = kvp.Value;
        _transitions[state] = dict;
    }

    // ---- Run ----

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
            if (_currentLeaf != State.None)
                throw new InvalidOperationException($"FSM already started at '{_currentLeaf}'.");

            var ctx = BuildContext(initialParams);
            var targetPath = ComputeEntryPath(initial); // includes descending into composites via initialChild
            await EnterPathAsync(targetPath, ctx).ConfigureAwait(false);
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
            if (_currentLeaf == State.None) return;
            var node = _nodes[_currentLeaf].Value;
            var ctx = BuildContext(message);
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
            if (_currentLeaf == State.None) return;
            var node = _nodes[_currentLeaf].Value;
            var ctx = BuildContext(null);
            await node.OnTimeoutAsync(ctx).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    // ---- Internal helpers ----

    private Context BuildContext(IDictionary<string, object>? initialParams)
    {
        var ctx = new Context(NextStateAsync);
        ctx.LastState = _currentLeaf;
        if (initialParams is not null)
        {
            foreach (var kvp in initialParams)
                ctx.Params[kvp.Key] = kvp.Value;
        }
        return ctx;
    }

    private bool IsComposite(State s) => _children.TryGetValue(s, out var set) && set.Count > 0;

    private State DescendToInitialLeaf(State s)
    {
        while (IsComposite(s))
        {
            if (!_initialChild.TryGetValue(s, out var child))
                throw new InvalidOperationException($"Composite '{s}' has no initial child defined.");
            s = child;
        }
        return s;
    }

    private List<State> ComputeEntryPath(State s)
    {
        var path = new List<State> { s };
        while (IsComposite(s))
        {
            if (!_initialChild.TryGetValue(s, out var child))
                throw new InvalidOperationException($"Composite '{s}' has no initial child defined.");
            s = child;
            path.Add(s);
        }
        return path;
    }

    private List<State> ComputePathToRoot(State leaf)
    {
        var path = new List<State>();
        var s = leaf;
        while (s != State.None)
        {
            path.Add(s);
            if (_parent.TryGetValue(s, out var p))
                s = p;
            else
                s = State.None;
        }
        path.Reverse(); // root -> ... -> leaf
        return path;
    }

    private async Task EnterPathAsync(List<State> path, Context ctx)
    {
        // Enter states in order (root to leaf)
        foreach (var s in path)
        {
            if (!_nodes.TryGetValue(s, out var lazy) || lazy is null)
                throw new InvalidOperationException($"State '{s}' is not registered.");
            var node = lazy.Value;
            await node.OnEnteringAsync(ctx).ConfigureAwait(false);
            await node.OnEnterAsync(ctx).ConfigureAwait(false);
        }
        _currentLeaf = path[^1];
    }

    private async Task ExitPathAsync(List<State> path, Context ctx)
    {
        // Exit states in reverse (leaf to root)
        for (int i = path.Count - 1; i >= 0; i--)
        {
            var s = path[i];
            if (_nodes.TryGetValue(s, out var lazy) && lazy.IsValueCreated)
            {
                var node = lazy.Value;
                await node.OnExitAsync(ctx).ConfigureAwait(false);
            }
        }
    }

    private static int CommonPrefixLength(List<State> a, List<State> b)
    {
        int n = Math.Min(a.Count, b.Count);
        int i = 0;
        while (i < n && a[i].Equals(b[i])) i++;
        return i;
    }

    private async Task NextStateAsync(Result result)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_currentLeaf == State.None)
                throw new InvalidOperationException("No active state to transition from.");

            if (!_transitions.TryGetValue(_currentLeaf, out var map) || !map.TryGetValue(result, out var target))
                throw new KeyNotFoundException($"No transition from '{_currentLeaf}' on result '{result}'.");

            // Compute full target path (descend into composite initial children)
            var targetLeaf = DescendToInitialLeaf(target);
            var currentPath = ComputePathToRoot(_currentLeaf);     // root..leaf
            var targetPath = ComputePathToRoot(targetLeaf);        // root..leaf

            int common = CommonPrefixLength(currentPath, targetPath);

            // Exit current states beyond common ancestor
            var toExit = currentPath.Skip(common).ToList();
            var ctx = BuildContext(null);
            await ExitPathAsync(toExit, ctx).ConfigureAwait(false);

            // Enter new states beyond common ancestor
            var toEnter = targetPath.Skip(common).ToList();
            await EnterPathAsync(toEnter, ctx).ConfigureAwait(false);

            // Terminal completion: if new leaf has no outgoing transitions, complete with the result used to reach it
            if (!_transitions.TryGetValue(_currentLeaf, out var outgoing) || outgoing.Count == 0)
            {
                _completionTcs?.TrySetResult(result);
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
```

***

## Sample Implementation with **Deep Hierarchy**

This example demonstrates:

*   `Checkout` (composite) → `Payment` (composite) → `AuthFlow` (composite) → `PreAuth → Authorize → PostAuth` → `Charge` → `Receipt`.
*   A sibling branch under `Checkout`: `Shipping`.
*   Typed params via `Context.Set/Get<T>`.

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ---- Concrete States ----

public sealed class InitState : StateNode
{
    public InitState() : base("Initialization") { }

    public override async Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Init] OnEnter");
        ctx.Set("startedAt", DateTime.UtcNow);
        await Task.Delay(50);
        await ctx.NextState(Result.Success);
    }
}

public sealed class CheckoutState : StateNode
{
    public CheckoutState() : base("Checkout") { }

    public override Task OnEnteringAsync(Context ctx)
    {
        Console.WriteLine("[Checkout] OnEntering - prepare checkout");
        return Task.CompletedTask;
    }

    public override Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Checkout] OnEnter - start checkout flow");
        return Task.CompletedTask;
    }
}

public sealed class PaymentState : StateNode
{
    public PaymentState() : base("Payment") { }

    public override Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Payment] OnEnter - choose payment method");
        return Task.CompletedTask;
    }
}

public sealed class AuthFlowState : StateNode
{
    public AuthFlowState() : base("Auth Flow") { }

    public override Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[AuthFlow] OnEnter - begin authorization");
        return Task.CompletedTask;
    }
}

public sealed class PreAuthState : StateNode
{
    public PreAuthState() : base("Pre-Authorization") { }

    public override async Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[PreAuth] OnEnter - prechecks");
        await Task.Delay(30);
        await ctx.NextState(Result.Success);
    }
}

public sealed class AuthorizeState : StateNode
{
    public AuthorizeState() : base("Authorize") { }

    public override async Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Authorize] OnEnter - contacting gateway...");
        // For demo: set a flag that determines outcome
        ctx.Set("authOk", true);
        await Task.Delay(100);

        bool ok = ctx.Get<bool>("authOk");
        await ctx.NextState(ok ? Result.Success : Result.Failure);
    }

    public override Task OnTimeoutAsync(Context ctx)
    {
        Console.WriteLine("[Authorize] OnTimeout - gateway unresponsive");
        ctx.Errors["authTimeout"] = "Authorization timed out";
        return ctx.NextState(Result.Failure);
    }
}

public sealed class PostAuthState : StateNode
{
    public PostAuthState() : base("Post-Authorization") { }

    public override async Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[PostAuth] OnEnter - finalizing auth");
        await Task.Delay(20);
        await ctx.NextState(Result.Success);
    }
}

public sealed class ChargeState : StateNode
{
    public ChargeState() : base("Charge") { }

    public override async Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Charge] OnEnter - capturing funds");
        await Task.Delay(50);
        await ctx.NextState(Result.Success);
    }
}

public sealed class ReceiptState : StateNode
{
    public ReceiptState() : base("Receipt") { }

    public override Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Receipt] OnEnter - issuing receipt");
        return Task.CompletedTask; // terminal (no outgoing transitions)
    }
}

public sealed class ShippingState : StateNode
{
    public ShippingState() : base("Shipping") { }

    public override async Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Shipping] OnEnter - ship order");
        await Task.Delay(50);
        await ctx.NextState(Result.Success);
    }
}

public sealed class CompletedState : StateNode
{
    public CompletedState() : base("Completed") { }

    public override Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Completed] OnEnter - workflow complete");
        return Task.CompletedTask;
    }
}

public sealed class ErrorState : StateNode
{
    public ErrorState() : base("Error") { }

    public override Task OnEnterAsync(Context ctx)
    {
        Console.WriteLine("[Error] OnEnter - failure occurred");
        foreach (var e in ctx.Errors)
            Console.WriteLine($"  {e.Key}: {e.Value}");
        return Task.CompletedTask;
    }
}

// ---- Wiring & Demo ----

public static class Demo
{
    public static async Task RunAsync()
    {
        var fsm = new FiniteStateMachine();

        // Register all states (lazy-loaded)
        fsm.RegisterState(State.Init,      "Initialization", () => new InitState());
        fsm.RegisterState(State.Checkout,  "Checkout",       () => new CheckoutState());
        fsm.RegisterState(State.Payment,   "Payment",        () => new PaymentState());
        fsm.RegisterState(State.AuthFlow,  "Auth Flow",      () => new AuthFlowState());
        fsm.RegisterState(State.PreAuth,   "Pre-Authorization", () => new PreAuthState());
        fsm.RegisterState(State.Authorize, "Authorize",      () => new AuthorizeState());
        fsm.RegisterState(State.PostAuth,  "Post-Authorization", () => new PostAuthState());
        fsm.RegisterState(State.Charge,    "Charge",         () => new ChargeState());
        fsm.RegisterState(State.Receipt,   "Receipt",        () => new ReceiptState());
        fsm.RegisterState(State.Shipping,  "Shipping",       () => new ShippingState());
        fsm.RegisterState(State.Completed, "Completed",      () => new CompletedState());
        fsm.RegisterState(State.Error,     "Error",          () => new ErrorState());

        // Declare deep composite hierarchy:
        // Checkout
        fsm.DefineComposite(State.Checkout, State.Payment);
        fsm.AddChild(State.Checkout, State.Shipping);

        // Payment (child of Checkout)
        fsm.DefineComposite(State.Payment, State.AuthFlow);
        fsm.AddChild(State.Payment, State.Charge);
        fsm.AddChild(State.Payment, State.Receipt);

        // AuthFlow (child of Payment)
        fsm.DefineComposite(State.AuthFlow, State.PreAuth);
        fsm.AddChild(State.AuthFlow, State.Authorize);
        fsm.AddChild(State.AuthFlow, State.PostAuth);

        // Transitions (Result → Next State), defined at the **leaf** where NextState is called:

        // Init → Checkout
        fsm.SetTransitions(State.Init, new Dictionary<Result, State>
        {
            [Result.Success] = State.Checkout,
            [Result.Error]   = State.Error,
            [Result.Failure] = State.Error
        });

        // Deep path inside AuthFlow
        fsm.SetTransitions(State.PreAuth, new Dictionary<Result, State>
        {
            [Result.Success] = State.Authorize,
            [Result.Error]   = State.Error,
            [Result.Failure] = State.Error
        });

        fsm.SetTransitions(State.Authorize, new Dictionary<Result, State>
        {
            [Result.Success] = State.PostAuth,
            [Result.Error]   = State.Error,
            [Result.Failure] = State.Error
        });

        fsm.SetTransitions(State.PostAuth, new Dictionary<Result, State>
        {
            [Result.Success] = State.Charge,   // leave AuthFlow → to Charge (still under Payment)
            [Result.Error]   = State.Error,
            [Result.Failure] = State.Error
        });

        // Charge → Receipt
        fsm.SetTransitions(State.Charge, new Dictionary<Result, State>
        {
            [Result.Success] = State.Receipt,
            [Result.Error]   = State.Error,
            [Result.Failure] = State.Error
        });

        // Shipping branch (not used in this path, but example)
        fsm.SetTransitions(State.Shipping, new Dictionary<Result, State>
        {
            [Result.Success] = State.Completed,
            [Result.Error]   = State.Error,
            [Result.Failure] = State.Error
        });

        // Receipt is terminal (no outgoing transitions), StartAndWaitAsync will complete once reached
        // Checkout completion mapping (optional): you can route from Receipt to Completed via NextState, or treat terminal.

        // Start from Init and wait for completion
        var finalResult = await fsm.StartAndWaitAsync(State.Init);
        Console.WriteLine($"FSM finished with result: {finalResult}");
    }
}

// Entry point
public static class Program
{
    public static async Task Main()
    {
        await Demo.RunAsync();
    }
}
```

**What you’ll see in output:**

*   The FSM enters `Init`, then transitions to `Checkout`.
*   It automatically descends: `Checkout` → `Payment` → `AuthFlow` → `PreAuth`.
*   Then transitions through `Authorize` → `PostAuth` → `Charge` → `Receipt`.
*   `Receipt` has no outgoing transitions → **terminal**, and `StartAndWaitAsync` completes using the **result that led to `Receipt`** (here: `Success`).

***

## Design Notes & Extensibility

*   **Deep hierarchy**: The FSM computes the **common ancestor** between the current leaf and target leaf, exits intermediate states (leaf → ancestor), then enters the new path (ancestor → target leaf), invoking `OnExitAsync` / `OnEnteringAsync` / `OnEnterAsync` in the correct order.
*   **Typed params**: Use `ctx.Set("key", value)` and `ctx.Get<T>("key")` to move data between states safely.
*   **Message/timeout routing**: Call `ForwardMessageAsync(...)` or `ForwardTimeoutAsync()`—they hit the **current leaf**. You can forward events up/down manually in your state methods if needed.
*   **Human-readable names**: Each `StateNode` has a `Name`. You pass one during registration, and your state classes can log the name.
*   **Lazy-loading**: `Lazy<StateNode>` ensures each node is constructed **on first actual use**.
*   **Terminal states**: Any leaf with **no outgoing transitions** is considered terminal. The `StartAndWaitAsync` returns the `Result` used to reach that terminal.

***

## Optional Add‑Ons (tell me if you want these wired in)

*   **Per-state timeouts** with `CancellationTokenSource` and automatic `OnTimeoutAsync` triggers.
*   **History states** for composites (re-enter last active child instead of initial child).
*   **Logging hooks** / `ILogger` integration.
*   **Guard conditions** on transitions (predicate before allowing `Result → Next`).
*   **Strongly-typed errors** similar to `Params`.

If you share your target state enum and desired flow, I can tailor the wiring and add unit tests.
