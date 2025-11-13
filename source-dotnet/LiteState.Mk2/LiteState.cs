// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace LiteState.Mk2;

public class AsyncStateMachine
{
  private readonly Dictionary<StateId, StateDefinition> _states = new();

  private StateDefinition? _currentState;
  private StateDefinition? _currentSubState;
  private CancellationTokenSource? _timeoutCts;

  private readonly Channel<(string message, Context context)> _messageChannel =
      Channel.CreateUnbounded<(string, Context)>();

  public AsyncStateMachine()
  {
    _ = ProcessMessagesAsync();
  }

  public void AddState(StateDefinition state) => _states[state.Id] = state;

  public async Task TransitionToAsync(StateId newStateId, Context context)
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

  private async Task TransitionToSubStateAsync(StateId subStateId, Context context)
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

  private async Task ExitCurrentStateAsync(Context context)
  {
    if (_currentSubState?.OnExit != null)
      await _currentSubState.OnExit(context);
    if (_currentState?.OnExit != null)
      await _currentState.OnExit(context);
    _timeoutCts?.Cancel();
  }

  private void SetupTimeout(StateDefinition state, Context context)
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

  public async Task SendMessageAsync(string message, Context context)
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
