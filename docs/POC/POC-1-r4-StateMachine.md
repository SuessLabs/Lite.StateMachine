# LiteState PoC v1.4

Methods for states

## Core

```cs
// Interfaces
namespace LiteState.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IState
    {
        StateId Id { get; }
        Task OnEntering(Dictionary<string, object> context);
        Task OnEnter(Dictionary<string, object> context);
        Task OnExit(Dictionary<string, object> context);
        Task OnTimeout(Dictionary<string, object> context);
        Task OnMessage(string message, Dictionary<string, object> context);
    }

    public interface ICompositeState : IState
    {
        IReadOnlyDictionary<StateId, IState> SubStates { get; }
        StateId? InitialSubState { get; }
    }

    public interface IStateMachine
    {
        Task TransitionToAsync(StateId stateId, Dictionary<string, object> context);
        Task SendMessageAsync(string message, Dictionary<string, object> context);
    }
}

// State History Tracking

namespace LiteState.Models
{
    public class StateHistory
    {
        private readonly Dictionary<StateId, StateId?> _history = new();

        public void Record(StateId parent, StateId child)
        {
            _history[parent] = child;
        }

        public StateId? GetLast(StateId parent)
        {
            return _history.TryGetValue(parent, out var last) ? last : null;
        }
    }
}

// Composite State With History

namespace LiteState.Models
{
    using LiteState.Interfaces;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class CompositeState : StateDefinition, ICompositeState
    {
        public Dictionary<StateId, IState> SubStates { get; } = new();
        public StateId? InitialSubState { get; set; }

        IReadOnlyDictionary<StateId, IState> ICompositeState.SubStates => SubStates;

        public CompositeState(StateId id) : base(id) { }

        public void AddSubState(IState state) => SubStates[state.Id] = state;
    }
}

// AsyncStateMachine with DI and History
//// In your Startup.cs or Program.cs
//// services.AddSingleton<IStateMachine, AsyncStateMachine>();
namespace LiteState.Services
{
    using LiteState.Interfaces;
    using LiteState.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    public class AsyncStateMachine : IStateMachine
    {
        private readonly Dictionary<StateId, IState> _states = new();
        private readonly StateHistory _history = new();
        private IState? _currentState;
        private IState? _currentSubState;
        private CancellationTokenSource? _timeoutCts;
        private readonly Channel<(string message, Dictionary<string, object> context)> _messageChannel =
            Channel.CreateUnbounded<(string, Dictionary<string, object>)>();

        public AsyncStateMachine()
        {
            _ = ProcessMessagesAsync();
        }

        public void AddState(IState state) => _states[state.Id] = state;

        public async Task TransitionToAsync(StateId newStateId, Dictionary<string, object> context)
        {
            if (!_states.TryGetValue(newStateId, out var newState))
                throw new InvalidOperationException($"State {newStateId} not defined.");

            await ExitCurrentStateAsync(context);

            await newState.OnEntering(context);
            _currentState = newState;

            if (newState is ICompositeState composite)
            {
                var subStateId = _history.GetLast(newStateId) ?? composite.InitialSubState;
                if (subStateId.HasValue)
                    await TransitionToSubStateAsync(subStateId.Value, context);
            }

            await newState.OnEnter(context);
            SetupTimeout(newState, context);
        }

        private async Task TransitionToSubStateAsync(StateId subStateId, Dictionary<string, object> context)
        {
            if (_currentState is ICompositeState composite && composite.SubStates.TryGetValue(subStateId, out var subState))
            {
                _currentSubState = subState;
                _history.Record(_currentState.Id, subStateId);
                await subState.OnEntering(context);
                await subState.OnEnter(context);
                SetupTimeout(subState, context);
            }
        }

        private async Task ExitCurrentStateAsync(Dictionary<string, object> context)
        {
            if (_currentSubState != null)
                await _currentSubState.OnExit(context);
            if (_currentState != null)
                await _currentState.OnExit(context);
            _timeoutCts?.Cancel();
        }

        private void SetupTimeout(IState state, Dictionary<string, object> context)
        {
            _timeoutCts = new CancellationTokenSource();
            _ = Task.Delay(TimeSpan.FromSeconds(10), _timeoutCts.Token)
                .ContinueWith(async t =>
                {
                    if (!t.IsCanceled)
                        await state.OnTimeout(context);
                });
        }

        public async Task SendMessageAsync(string message, Dictionary<string, object> context)
        {
            await _messageChannel.Writer.WriteAsync((message, context));
        }

        private async Task ProcessMessagesAsync()
        {
            await foreach (var (message, context) in _messageChannel.Reader.ReadAllAsync())
            {
                if (_currentSubState != null)
                    await _currentSubState.OnMessage(message, context);
                else if (_currentState != null)
                    await _currentState.OnMessage(message, context);
            }
        }
    }
}
```
