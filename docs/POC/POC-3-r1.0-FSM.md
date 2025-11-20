# MK3-Vibe-2.0-FSM

## Instructions

Using C#, create a finite state machine named "LiteState" which can optionally have composite states, with each state defined as an Enum. States must be lazy-loaded. Defining states must use the State enumeration and a human-readable name. Registering states uses the method named, "State". Add support for deep composite hierarchies.

Each state's transition must be an async method. The state can has an optional transition of OnEntering for transitioning into it, OnEnter for when fully transitioned, optionally an OnMessage for receiving messages sent by the OnEnter, optionally an OnTimeout for when events are not received by the OnMessage in time, and optionally an OnExit for when it is completed.

Each transition must pass a Context class as an argument which contains a property named "Params" of type "Dictionary<string, object>", property named "Errors" of type "Dictionary<string, object>", a property "LastState" which has the enum value of the previous state, and the method "NextState" to trigger moving to the next state.

The "NextState" method has an enum argument named "Result" with the values of "Success", "Error", and "Failure". The NextState method can be called by any of the transitions to move to the next state.

The LiteState core can assign a default Error and Failure state to transition to, so that is does not need to be defined every time.

Include unit tests for the FSM framework using MSTest.

### Extending

Customize this skeleton for your specific enum and domain logic.
Add tracing hooks or diagnostics (e.g., ILogger).
Generate a NuGet-ready library project and a sample app.
Convert any of your existing state flow comments into inline documentation (I remember you prefer preserving original comments when migrating code).

## FSM

```cs

// ================================
// File: LiteState.cs
// Namespace: LiteStateFsm
// ================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteStateFsm
{
    /// <summary>
    /// Result for NextState() routing.
    /// </summary>
    public enum Result
    {
        Success,
        Error,
        Failure
    }

    /// <summary>
    /// The transition context passed to each state's async transition.
    /// </summary>
    public sealed class Context<TState> where TState : Enum
    {
        private readonly LiteState<TState> _fsm;
        internal Context(LiteState<TState> fsm, TState? lastState)
        {
            _fsm = fsm;
            LastState = lastState;
        }

        /// <summary>
        /// Arbitrary parameters associated with the FSM execution.
        /// </summary>
        public Dictionary<string, object> Params { get; } = new();

        /// <summary>
        /// Arbitrary errors captured during execution.
        /// </summary>
        public Dictionary<string, object> Errors { get; } = new();

        /// <summary>
        /// The enum value of the previous leaf state (before the current transition).
        /// </summary>
        public TState? LastState { get; internal set; }

        /// <summary>
        /// Move to the next state, routed by the current state's Next(Result) mapping.
        /// </summary>
        public Task NextState(Result result) => _fsm.Next(result);

        /// <summary>
        /// Send a message to the current state's OnMessage handler (if defined).
        /// Also cancels any pending timeout on the current leaf state.
        /// </summary>
        public Task SendMessageAsync(object message) => _fsm.SendMessageAsync(message);
    }

    /// <summary>
    /// Internal node built from a lazy definition. Supports hierarchy (parent/children).
    /// </summary>
    internal sealed class StateNode<TState> where TState : Enum
    {
        public TState Id { get; }
        public string Name { get; }
        public TState? ParentId { get; internal set; }
        public HashSet<TState> Children { get; } = new();

        // Optional handlers (all async).
        public Func<Context<TState>, Task>? OnEntering { get; internal set; }
        public Func<Context<TState>, Task>? OnEnter { get; internal set; }
        public Func<Context<TState>, object, Task>? OnMessage { get; internal set; }
        public Func<Context<TState>, Task>? OnTimeout { get; internal set; }
        public TimeSpan? Timeout { get; internal set; }
        public Func<Context<TState>, Task>? OnExit { get; internal set; }

        // Routing map: Result -> Next State
        public Dictionary<Result, TState> NextMap { get; } = new();

        public StateNode(TState id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString() => $"{Id} ({Name})";
    }

    /// <summary>
    /// Builder used by the public State(...) DSL method.
    /// </summary>
    public sealed class StateBuilder<TState> where TState : Enum
    {
        private readonly StateDefinition<TState> _def;
        internal StateBuilder(StateDefinition<TState> def) => _def = def;

        public StateBuilder<TState> OnEntering(Func<Context<TState>, Task>? handler)
        { _def.OnEntering = handler; return this; }

        public StateBuilder<TState> OnEnter(Func<Context<TState>, Task>? handler)
        { _def.OnEnter = handler; return this; }

        public StateBuilder<TState> OnMessage(Func<Context<TState>, object, Task>? handler)
        { _def.OnMessage = handler; return this; }

        public StateBuilder<TState> OnTimeout(TimeSpan timeout, Func<Context<TState>, Task>? handler)
        { _def.Timeout = timeout; _def.OnTimeout = handler; return this; }

        public StateBuilder<TState> OnExit(Func<Context<TState>, Task>? handler)
        { _def.OnExit = handler; return this; }

        public StateBuilder<TState> Next(Result result, TState nextState)
        { _def.NextMap[result] = nextState; return this; }

        /// <summary>
        /// Define a composite hierarchy under this state.
        /// </summary>
        public StateBuilder<TState> Composite(Action<CompositeBuilder<TState>> composite)
        {
            var c = new CompositeBuilder<TState>(_def.Fsm, _def.Id);
            composite(c);
            return this;
        }
    }

    /// <summary>
    /// Composite builder to add child states under a parent.
    /// </summary>
    public sealed class CompositeBuilder<TState> where TState : Enum
    {
        private readonly LiteState<TState> _fsm;
        private readonly TState _parent;

        internal CompositeBuilder(LiteState<TState> fsm, TState parent)
        {
            _fsm = fsm;
            _parent = parent;
        }

        public CompositeBuilder<TState> Child(TState id, string name, Action<StateBuilder<TState>>? configure = null)
        {
            _fsm.State(id, name, configure, parentId: _parent);
            return this;
        }
    }

    /// <summary>
    /// Lazy state definition holder (not built until needed).
    /// </summary>
    internal sealed class StateDefinition<TState> where TState : Enum
    {
        public LiteState<TState> Fsm { get; }
        public TState Id { get; }
        public string Name { get; }
        public TState? ParentId { get; }

        public Func<Context<TState>, Task>? OnEntering { get; set; }
        public Func<Context<TState>, Task>? OnEnter { get; set; }
        public Func<Context<TState>, object, Task>? OnMessage { get; set; }
        public Func<Context<TState>, Task>? OnTimeout { get; set; }
        public TimeSpan? Timeout { get; set; }
        public Func<Context<TState>, Task>? OnExit { get; set; }
        public Dictionary<Result, TState> NextMap { get; } = new();

        public StateDefinition(LiteState<TState> fsm, TState id, string name, TState? parentId)
        {
            Fsm = fsm;
            Id = id;
            Name = name;
            ParentId = parentId;
        }

        public StateNode<TState> Build()
        {
            var node = new StateNode<TState>(Id, Name)
            {
                ParentId = ParentId,
                OnEntering = OnEntering,
                OnEnter = OnEnter,
                OnMessage = OnMessage,
                OnTimeout = OnTimeout,
                Timeout = Timeout,
                OnExit = OnExit
            };
            foreach (var kvp in NextMap)
                node.NextMap[kvp.Key] = kvp.Value;
            return node;
        }
    }

    /// <summary>
    /// The LiteState FSM core. Generic over an Enum of states.
    /// Supports lazy-loaded state definitions, async transitions, and deep composite hierarchies.
    /// </summary>
    public sealed class LiteState<TState> where TState : Enum
    {
        private readonly Dictionary<TState, StateDefinition<TState>> _defs = new();
        private readonly Dictionary<TState, StateNode<TState>> _nodes = new(); // lazy materialized
        private readonly SemaphoreSlim _gate = new(1, 1);

        private readonly TState _defaultError;
        private readonly TState _defaultFailure;

        private List<TState> _activeStack = new(); // ancestry path from root to leaf
        private CancellationTokenSource? _timeoutCts;

        public LiteState(TState defaultError, TState defaultFailure)
        {
            _defaultError = defaultError;
            _defaultFailure = defaultFailure;
        }

        /// <summary>
        /// Register a state by enum and human-readable name.
        /// </summary>
        public LiteState<TState> State(TState id, string name, Action<StateBuilder<TState>>? configure = null, TState? parentId = null)
        {
            if (_defs.ContainsKey(id))
                throw new InvalidOperationException($"State '{id}' is already defined.");

            var def = new StateDefinition<TState>(this, id, name, parentId);
            _defs[id] = def;

            // Configure via builder
            if (configure != null)
            {
                var builder = new StateBuilder<TState>(def);
                configure(builder);
            }

            // If parent assigned, track relationship once built.
            return this;
        }

        /// <summary>
        /// Start the FSM at a specific leaf state (async initializes).
        /// </summary>
        public async Task StartAsync(TState initialLeaf, Dictionary<string, object>? initialParams = null)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                _activeStack.Clear();
                var targetPath = GetAncestry(initialLeaf);
                // Perform entering for each node along the path
                var ctx = new Context<TState>(this, lastState: null);
                if (initialParams != null)
                {
                    foreach (var (k, v) in initialParams)
                        ctx.Params[k] = v;
                }

                foreach (var id in targetPath)
                {
                    var node = GetNode(id);
                    if (node.ParentId.HasValue)
                        GetNode(node.ParentId.Value).Children.Add(node.Id);

                    if (node.OnEntering != null)
                        await node.OnEntering(ctx).ConfigureAwait(false);
                }

                _activeStack = targetPath;

                foreach (var id in targetPath)
                {
                    var node = GetNode(id);
                    if (node.OnEnter != null)
                        await node.OnEnter(ctx).ConfigureAwait(false);
                }

                await ArmTimeoutForLeafAsync(ctx).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Send a message to the current leaf's OnMessage (if present).
        /// Cancels the leaf timeout while dispatching the message.
        /// </summary>
        public async Task SendMessageAsync(object message)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var leafId = CurrentLeafOrThrow();
                var leaf = GetNode(leafId);

                CancelTimeout();

                if (leaf.OnMessage != null)
                {
                    var ctx = new Context<TState>(this, lastState: leafId);
                    await leaf.OnMessage(ctx, message).ConfigureAwait(false);
                }

                // Re-arm timeout after message if still on same leaf and timeout defined.
                var ctx2 = new Context<TState>(this, lastState: leafId);
                await ArmTimeoutForLeafAsync(ctx2).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Trigger transition based on Result routing from the current leaf.
        /// </summary>
        internal async Task Next(Result result)
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                CancelTimeout();

                var currentLeaf = CurrentLeafOrThrow();
                var currentNode = GetNode(currentLeaf);

                var nextId = ResolveNext(currentNode, result);

                if (nextId == null)
                    throw new InvalidOperationException($"No route for result '{result}' from state '{currentLeaf}'.");
                
                var ctx = new Context<TState>(this, lastState: currentLeaf);

                // Compute LCA and orchestrate exit/enter across the hierarchy.
                var currentPath = _activeStack;
                var targetPath = GetAncestry(nextId.Value);
                int commonLen = CommonPrefixLength(currentPath, targetPath);

                // Exit from current leaf up to (but excluding) LCA
                for (int i = currentPath.Count - 1; i >= commonLen; i--)
                {
                    var node = GetNode(currentPath[i]);
                    if (node.OnExit != null)
                        await node.OnExit(ctx).ConfigureAwait(false);
                }

                // Entering actions on target path beyond LCA
                for (int i = commonLen; i < targetPath.Count; i++)
                {
                    var node = GetNode(targetPath[i]);
                    // wire parent-child set
                    if (node.ParentId.HasValue)
                        GetNode(node.ParentId.Value).Children.Add(node.Id);
                    if (node.OnEntering != null)
                        await node.OnEntering(ctx).ConfigureAwait(false);
                }

                // Replace active stack with target path
                _activeStack = targetPath;

                // OnEnter actions
                for (int i = commonLen; i < targetPath.Count; i++)
                {
                    var node = GetNode(targetPath[i]);
                    if (node.OnEnter != null)
                        await node.OnEnter(ctx).ConfigureAwait(false);
                }

                await ArmTimeoutForLeafAsync(ctx).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Helper to set timeout for current leaf (if configured).
        /// </summary>
        private async Task ArmTimeoutForLeafAsync(Context<TState> ctx)
        {
            var leafId = CurrentLeaf();
            if (leafId == null) return;
            var leaf = GetNode(leafId.Value);

            if (leaf.Timeout.HasValue && leaf.Timeout.Value > TimeSpan.Zero && leaf.OnTimeout != null)
            {
                CancelTimeout();
                _timeoutCts = new CancellationTokenSource();
                var token = _timeoutCts.Token;
                try
                {
                    // Fire-and-forget delay that triggers OnTimeout unless cancelled
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(leaf.Timeout.Value, token).ConfigureAwait(false);
                            if (!token.IsCancellationRequested)
                            {
                                // Acquire lock to ensure consistency
                                await _gate.WaitAsync().ConfigureAwait(false);
                                try
                                {
                                    // Call timeout handler
                                    await leaf.OnTimeout!(ctx).ConfigureAwait(false);
                                }
                                finally
                                {
                                    _gate.Release();
                                }
                            }
                        }
                        catch (TaskCanceledException) { /* expected */ }
                    });
                }
                catch
                {
                    CancelTimeout();
                    throw;
                }
            }
            else
            {
                CancelTimeout();
            }
        }

        private void CancelTimeout()
        {
            try
            {
                _timeoutCts?.Cancel();
            }
            catch { /* ignore */ }
            finally
            {
                _timeoutCts?.Dispose();
                _timeoutCts = null;
            }
        }

        private TState CurrentLeafOrThrow()
        {
            var id = CurrentLeaf();
            if (id == null)
                throw new InvalidOperationException("FSM has no active state. Call StartAsync(...) first.");
            return id.Value;
        }

        private TState? CurrentLeaf()
        {
            if (_activeStack.Count == 0) return default;
            return _activeStack[^1];
        }

        private TState? ResolveNext(StateNode<TState> node, Result result)
        {
            if (node.NextMap.TryGetValue(result, out var next))
                return next;

            // Built-in fallbacks
            if (result == Result.Error) return _defaultError;
            if (result == Result.Failure) return _defaultFailure;

            // No default for Success; must be explicitly mapped
            return null;
        }

        private int CommonPrefixLength(List<TState> a, List<TState> b)
        {
            int i = 0;
            while (i < a.Count && i < b.Count && EqualityComparer<TState>.Default.Equals(a[i], b[i]))
                i++;
            return i;
        }

        private List<TState> GetAncestry(TState id)
        {
            var result = new List<TState>();
            var cursor = id;
            while (true)
            {
                var node = GetNode(cursor);
                // Build upward path first; we'll reverse at the end.
                result.Add(cursor);
                if (!node.ParentId.HasValue) break;
                cursor = node.ParentId.Value;
            }
            result.Reverse();
            return result;
        }

        private StateNode<TState> GetNode(TState id)
        {
            if (_nodes.TryGetValue(id, out var node))
                return node;

            if (!_defs.TryGetValue(id, out var def))
                throw new InvalidOperationException($"State '{id}' is not defined.");

            node = def.Build();
            _nodes[id] = node;
            return node;
        }
    }
}

```

## Tests

```cs

// ==================================
// File: ExampleStates.cs (optional)
// Namespace: LiteStateFsm.Examples
// ==================================
namespace LiteStateFsm.Examples
{
    /// <summary>
    /// Example enum for unit tests and sample usage.
    /// You can replace with your own enum for your domain.
    /// </summary>
    public enum DemoState
    {
        Root,
        Idle,
        Working,
        Working_Stage1,
        Working_Stage2,
        Done,
        Error,
        Failure
    }
}
```

### LiteStateTests.cs

```cs
// ==================================
// File: LiteStateTests.cs
// Namespace: LiteStateFsm.Tests
// MSTest Unit Tests
// ==================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteStateFsm;
using LiteStateFsm.Examples;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteStateFsm.Tests
{
    [TestClass]
    public class LiteStateTests
    {
        private LiteState<DemoState> BuildFsm(List<string> log)
        {
            var fsm = new LiteState<DemoState>(defaultError: DemoState.Error, defaultFailure: DemoState.Failure);

            // Root (composite) -> Idle, Working (composite), Done
            fsm.State(DemoState.Root, "Root", s =>
            {
                s.OnEntering(async ctx => { log.Add("Root.OnEntering"); await Task.Yield(); });
                s.OnEnter(async ctx => { log.Add("Root.OnEnter"); await Task.Yield(); });

                s.Composite(c =>
                {
                    c.Child(DemoState.Idle, "Idle", s2 =>
                    {
                        s2.OnEntering(async ctx => { log.Add("Idle.OnEntering"); await Task.Yield(); });
                        s2.OnEnter(async ctx =>
                        {
                            log.Add("Idle.OnEnter");
                            await Task.Yield();
                            // Move to Working on success
                            await ctx.NextState(Result.Success);
                        });
                        s2.OnExit(async ctx => { log.Add("Idle.OnExit"); await Task.Yield(); });
                        s2.Next(Result.Success, DemoState.Working);
                    });

                    c.Child(DemoState.Working, "Working", s2 =>
                    {
                        s2.OnEntering(async ctx => { log.Add("Working.OnEntering"); await Task.Yield(); });
                        s2.OnEnter(async ctx => { log.Add("Working.OnEnter"); await Task.Yield(); });

                        s2.Composite(c2 =>
                        {
                            c2.Child(DemoState.Working_Stage1, "Stage1", s3 =>
                            {
                                s3.OnEntering(async ctx => { log.Add("Stage1.OnEntering"); await Task.Yield(); });
                                s3.OnEnter(async ctx =>
                                {
                                    log.Add("Stage1.OnEnter");
                                    await Task.Yield();
                                    // Send a message to transition onward
                                    await ctx.SendMessageAsync("go-stage2");
                                });
                                s3.OnMessage(async (ctx, msg) =>
                                {
                                    log.Add($"Stage1.OnMessage({msg})");
                                    await Task.Yield();
                                    await ctx.NextState(Result.Success);
                                });
                                s3.OnExit(async ctx => { log.Add("Stage1.OnExit"); await Task.Yield(); });
                                s3.Next(Result.Success, DemoState.Working_Stage2);
                            });

                            c2.Child(DemoState.Working_Stage2, "Stage2", s3 =>
                            {
                                s3.OnEntering(async ctx => { log.Add("Stage2.OnEntering"); await Task.Yield(); });
                                s3.OnEnter(async ctx =>
                                {
                                    log.Add("Stage2.OnEnter");
                                    await Task.Yield();
                                    await ctx.NextState(Result.Success);
                                });
                                s3.OnExit(async ctx => { log.Add("Stage2.OnExit"); await Task.Yield(); });
                                s3.Next(Result.Success, DemoState.Done);
                            });
                        });

                        s2.OnExit(async ctx => { log.Add("Working.OnExit"); await Task.Yield(); });
                    });

                    c.Child(DemoState.Done, "Done", s2 =>
                    {
                        s2.OnEntering(async ctx => { log.Add("Done.OnEntering"); await Task.Yield(); });
                        s2.OnEnter(async ctx => { log.Add("Done.OnEnter"); await Task.Yield(); });
                        s2.OnExit(async ctx => { log.Add("Done.OnExit"); await Task.Yield(); });
                    });

                    // Default fallback error/failure states
                    c.Child(DemoState.Error, "Error", s2 =>
                    {
                        s2.OnEntering(async ctx => { log.Add("Error.OnEntering"); await Task.Yield(); });
                        s2.OnEnter(async ctx => { log.Add("Error.OnEnter"); await Task.Yield(); });
                    });

                    c.Child(DemoState.Failure, "Failure", s2 =>
                    {
                        s2.OnEntering(async ctx => { log.Add("Failure.OnEntering"); await Task.Yield(); });
                        s2.OnEnter(async ctx => { log.Add("Failure.OnEnter"); await Task.Yield(); });
                    });
                });
            });

            return fsm;
        }

        [TestMethod]
        public async Task Basic_Async_Transitions_Work()
        {
            var log = new List<string>();
            var fsm = BuildFsm(log);

            // Start at Root->Idle (composite path)
            await fsm.StartAsync(DemoState.Idle);

            // Expect the path transitions to flow to Working->Stage1->Stage2->Done
            // Driven by OnEnter, OnMessage, NextState(Result.Success), etc.
            // Let any pending tasks complete.
            await Task.Delay(50);

            // Verify ordering highlights:
            // Root.OnEntering, Root.OnEnter,
            // Idle.OnEntering, Idle.OnEnter, Idle.OnExit,
            // Working.OnEntering, Working.OnEnter,
            // Stage1.OnEntering, Stage1.OnEnter, Stage1.OnMessage(go-stage2), Stage1.OnExit,
            // Stage2.OnEntering, Stage2.OnEnter, Stage2.OnExit,
            // Done.OnEntering, Done.OnEnter
            CollectionAssert.Contains(log, "Root.OnEntering");
            CollectionAssert.Contains(log, "Idle.OnEnter");
            CollectionAssert.Contains(log, "Working.OnEnter");
            CollectionAssert.Contains(log, "Stage1.OnMessage(go-stage2)");
            CollectionAssert.Contains(log, "Stage2.OnEnter");
            CollectionAssert.Contains(log, "Done.OnEnter");
        }

        [TestMethod]
        public async Task Composite_Deep_Hierarchy_EntryExit_Order_Is_Correct()
        {
            var log = new List<string>();
            var fsm = new LiteState<DemoState>(DemoState.Error, DemoState.Failure);

            // Build a deeper hierarchy manually to verify LCA behavior
            fsm.State(DemoState.Root, "Root", s =>
            {
                s.Composite(c =>
                {
                    c.Child(DemoState.Working, "Working", sW =>
                    {
                        sW.Composite(c2 =>
                        {
                            c2.Child(DemoState.Working_Stage1, "Stage1", s1 =>
                            {
                                s1.OnEnter(async ctx => { log.Add("S1.Enter"); await Task.Yield(); });
                                s1.OnExit(async ctx => { log.Add("S1.Exit"); await Task.Yield(); });
                                s1.Next(Result.Success, DemoState.Working_Stage2);
                            });
                            c2.Child(DemoState.Working_Stage2, "Stage2", s2 =>
                            {
                                s2.OnEntering(async ctx => { log.Add("S2.Entering"); await Task.Yield(); });
                                s2.OnEnter(async ctx => { log.Add("S2.Enter"); await Task.Yield(); });
                                s2.OnExit(async ctx => { log.Add("S2.Exit"); await Task.Yield(); });
                                s2.Next(Result.Success, DemoState.Done);
                            });
                        });
                        sW.OnExit(async ctx => { log.Add("W.Exit"); await Task.Yield(); });
                    });

                    c.Child(DemoState.Done, "Done", sD =>
                    {
                        sD.OnEnter(async ctx => { log.Add("Done.Enter"); await Task.Yield(); });
                    });

                    c.Child(DemoState.Error, "Error");
                    c.Child(DemoState.Failure, "Failure");
                });
            });

            await fsm.StartAsync(DemoState.Working_Stage1);
            await fsm.Next(Result.Success); // Stage1 -> Stage2
            await fsm.Next(Result.Success); // Stage2 -> Done (exiting Working subtree)

            // Expect exits from deeper nodes before parent, enters for target path bottom-up.
            // Key markers:
            // S1.Exit occurs before S2.Entering
            // S2.Exit occurs before W.Exit when moving to Done
            var s1ExitIndex = log.IndexOf("S1.Exit");
            var s2EnteringIndex = log.IndexOf("S2.Entering");
            Assert.IsTrue(s1ExitIndex >= 0 && s2EnteringIndex >= 0 && s1ExitIndex < s2EnteringIndex);

            var s2ExitIndex = log.IndexOf("S2.Exit");
            var wExitIndex = log.IndexOf("W.Exit");
            Assert.IsTrue(s2ExitIndex >= 0 && wExitIndex >= 0 && s2ExitIndex < wExitIndex);

            CollectionAssert.Contains(log, "Done.Enter");
        }

        [TestMethod]
        public async Task Timeout_Triggers_OnTimeout_And_Failure_Fallback()
        {
            var log = new List<string>();
            var fsm = new LiteState<DemoState>(DemoState.Error, DemoState.Failure);

            fsm.State(DemoState.Root, "Root", s =>
            {
                s.Composite(c =>
                {
                    c.Child(DemoState.Idle, "Idle", si =>
                    {
                        si.OnEnter(async ctx =>
                        {
                            log.Add("Idle.Enter");
                            await Task.Yield();
                            // Do not send message; allow timeout on next state
                            await ctx.NextState(Result.Success);
                        });
                        si.Next(Result.Success, DemoState.Working_Stage1);
                    });

                    c.Child(DemoState.Working_Stage1, "Stage1", s1 =>
                    {
                        s1.OnEnter(async ctx =>
                        {
                            log.Add("Stage1.Enter");
                            await Task.Yield();
                        });
                        s1.OnTimeout(TimeSpan.FromMilliseconds(50), async ctx =>
                        {
                            log.Add("Stage1.Timeout");
                            await Task.Yield();
                            await ctx.NextState(Result.Failure); // Will fallback to default failure state
                        });
                    });

                    c.Child(DemoState.Failure, "Failure", sf =>
                    {
                        sf.OnEnter(async ctx =>
                        {
                            log.Add("Failure.Enter");
                            await Task.Yield();
                        });
                    });

                    c.Child(DemoState.Error, "Error");
                });
            });

            await fsm.StartAsync(DemoState.Idle);
            await Task.Delay(150); // enough time to allow timeout and transition

            // Verify OnTimeout fired and went to Failure
            CollectionAssert.Contains(log, "Stage1.Timeout");
            CollectionAssert.Contains(log, "Failure.Enter");
        }

        [TestMethod]
        public async Task Error_Fallback_Without_Explicit_Route()
        {
            var log = new List<string>();
            var fsm = new LiteState<DemoState>(defaultError: DemoState.Error, defaultFailure: DemoState.Failure);

            fsm.State(DemoState.Root, "Root", s =>
            {
                s.Composite(c =>
                {
                    c.Child(DemoState.Idle, "Idle", si =>
                    {
                        si.OnEnter(async ctx =>
                        {
                            log.Add("Idle.Enter");
                            await Task.Yield();
                            await ctx.NextState(Result.Error); // no explicit route -> fallback to Error
                        });
                    });

                    c.Child(DemoState.Error, "Error", se =>
                    {
                        se.OnEnter(async ctx =>
                        {
                            log.Add("Error.Enter");
                            await Task.Yield();
                        });
                    });

                    c.Child(DemoState.Failure, "Failure");
                });
            });

            await fsm.StartAsync(DemoState.Idle);
            await Task.Delay(20);

            CollectionAssert.Contains(log, "Error.Enter");
        }
    }
}

```


## Example

```cs

using LiteStateFsm;
using System.Threading.Tasks;

public enum OrderState
{
    Root,
    Created,
    Charging,
    Shipped,
    Error,
    Failure
}

public static class Example
{
    public static async Task RunAsync()
    {
        var fsm = new LiteState<OrderState>(defaultError: OrderState.Error, defaultFailure: OrderState.Failure);

        fsm.State(OrderState.Root, "Root", s =>
        {
            s.Composite(c =>
            {
                c.Child(OrderState.Created, "Order Created", cs =>
                {
                    cs.OnEnter(async ctx =>
                    {
                        // Start charging
                        await ctx.NextState(Result.Success);
                    });
                    cs.Next(Result.Success, OrderState.Charging);
                });

                c.Child(OrderState.Charging, "Charging Customer", ch =>
                {
                    ch.OnEnter(async ctx =>
                    {
                        // Business logic...
                        await ctx.NextState(Result.Success);
                    });
                    ch.Next(Result.Success, OrderState.Shipped);
                });

                c.Child(OrderState.Shipped, "Shipped");
                c.Child(OrderState.Error, "Error");
                c.Child(OrderState.Failure, "Failure");
            });
        });

        await fsm.StartAsync(OrderState.Created);
    }
}

```
