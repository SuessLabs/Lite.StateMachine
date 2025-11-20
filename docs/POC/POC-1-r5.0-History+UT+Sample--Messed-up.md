# LiteState POC v1.5.0

This sample gets messed up. Don't use!!

* Optional History Tracking + Unit Testing

## Core - Composit State

```cs
// Updated FSM Code with History Support
public class CompositeState : StateDefinition
{
    public Dictionary<StateId, StateDefinition> SubStates { get; } = new();
    public StateId? InitialSubState { get; set; }
    public bool EnableHistory { get; set; } = false;
    private StateId? _lastActiveSubState;

    public CompositeState(StateId id) : base(id) { }

    public void AddSubState(StateDefinition state) => SubStates[state.Id] = state;

    public StateId GetNextSubState()
    {
        if (EnableHistory && _lastActiveSubState.HasValue)
            return _lastActiveSubState.Value;
        return InitialSubState ?? throw new InvalidOperationException("No initial sub-state defined.");
    }

    public void SetLastActive(StateId subStateId)
    {
        if (EnableHistory)
            _lastActiveSubState = subStateId;
    }
}
```

## Core

```
public class LiteStateMachine
{
    private readonly Dictionary<StateId, StateDefinition> _states = new();
    private StateDefinition? _currentState;

    public void AddState(StateDefinition state) => _states[state.Id] = state;

    public async Task TransitionToAsync(StateId newStateId, Dictionary<string, object> context)
    {
        if (_currentState?.OnExit != null)
            await _currentState.OnExit(context);

        _currentState = _states[newStateId];

        if (_currentState.OnEnter != null)
            await _currentState.OnEnter(context);
    }

    public async Task SendMessageAsync(string message, Dictionary<string, object> context)
    {
        if (_currentState?.OnMessage != null)
            await _currentState.OnMessage(message, context);
    }
}
```

## UnitTests

```cs
// MSTest Unit Tests for LiteStateMachine

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

[TestClass]
public class LiteStateMachineTests
{
    [TestMethod]
    public async Task Transition_ShouldCallOnEnter()
    {
        var fsm = new LiteStateMachine();
        bool entered = false;

        var state = new StateDefinition(StateId.Loading)
        {
            OnEnter = async ctx => entered = true
        };

        fsm.AddState(state);
        await fsm.TransitionToAsync(StateId.Loading, new Dictionary<string, object>());

        Assert.IsTrue(entered);
    }

    [TestMethod]
    public async Task Message_ShouldInvokeOnMessage()
    {
        var fsm = new LiteStateMachine();
        string received = null;

        var state = new StateDefinition(StateId.Processing)
        {
            OnEnter = async ctx => { },
            OnMessage = async (msg, ctx) => received = msg
        };

        fsm.AddState(state);
        await fsm.TransitionToAsync(StateId.Processing, new Dictionary<string, object>());
        await fsm.SendMessageAsync("TestMessage", new Dictionary<string, object>());

        Assert.AreEqual("TestMessage", received);
    }

    [TestMethod]
    public async Task Exit_ShouldInvokeOnExit()
    {
        var fsm = new LiteStateMachine();
        bool exited = false;

        var state1 = new StateDefinition(StateId.Loading)
        {
            OnExit = async ctx => exited = true
        };

        var state2 = new StateDefinition(StateId.Completed);

        fsm.AddState(state1);
        fsm.AddState(state2);

        await fsm.TransitionToAsync(StateId.Loading, new Dictionary<string, object>());
        await fsm.TransitionToAsync(StateId.Completed, new Dictionary<string, object>());

        Assert.IsTrue(exited);
    }
}
```

## Sample

```cs

class Program
{
    static async Task Main()
    {
        var fsm = new AsyncStateMachine();

        var root = new CompositeState(StateId.Root)
        {
            OnEnter = async ctx => Console.WriteLine("Root entered."),
            InitialSubState = StateId.Loading,
            EnableHistory = true
        };

        var loading = new StateDefinition(StateId.Loading)
        {
            OnEnter = async ctx => Console.WriteLine("Loading..."),
            OnTimeout = async ctx => Console.WriteLine("Loading timed out."),
            OnExit = async ctx => Console.WriteLine("Exiting Loading.")
        };

        var processing = new CompositeState(StateId.Processing)
        {
            OnEnter = async ctx => Console.WriteLine("Processing started."),
            InitialSubState = StateId.SubProcessing,
            EnableHistory = true
        };

        var subProcessing = new StateDefinition(StateId.SubProcessing)
        {
            OnEnter = async ctx => Console.WriteLine("Sub-processing started."),
            OnMessage = async (msg, ctx) => Console.WriteLine($"SubProcessing received: {msg}")
        };

        processing.AddSubState(subProcessing);
        root.AddSubState(loading);
        root.AddSubState(processing);

        fsm.AddState(root);
        fsm.AddState(processing);

        var context = new Dictionary<string, object>();

        await fsm.TransitionToAsync(StateId.Root, context);
        await Task.Delay(2000);
        await fsm.TransitionToAsync(StateId.Processing, context);
        await fsm.SendMessageAsync("Hello FSM!", context);

        // Simulate going back to Root and then returning to Processing with history
        await fsm.TransitionToAsync(StateId.Root, context);
        await fsm.TransitionToAsync(StateId.Processing, context); // Should resume SubProcessing
    }
}
```
