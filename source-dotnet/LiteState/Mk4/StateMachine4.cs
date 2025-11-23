// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace LiteState.Mk4;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public enum Result
{
  Ok,
  Error,
  Failure
}

/// <summary>
/// Command-state interface: receives messages and can time out.
/// </summary>
public interface ICommandState<TState> : IState<TState> where TState : struct, Enum
{
  /// <summary>
  /// Optional message filter; return true to deliver to this state, false to ignore.
  /// Default: accept all messages.
  /// </summary>
  Func<object, bool> MessageFilter => _ => true;

  /// <summary>
  /// Optional override of timeout for this state; null uses machine default.
  /// </summary>
  int? TimeoutMs => null;

  /// <summary>
  /// Receives a message from the event aggregator.
  /// </summary>
  void OnMessage(Context<TState> context, object message);

  /// <summary>
  /// Fires when no messages are received within the timeout window.
  /// </summary>
  void OnTimeout(Context<TState> context);
}

/// <summary>
/// Composite (hierarchical) state interface: has an owned submachine.
/// </summary>
public interface ICompositeState<TState> : IState<TState> where TState : struct, Enum
{
  StateMachine<TState> Submachine { get; }
}

/// <summary>
/// Simple event aggregator for delivering messages to the current command state.
/// </summary>
public interface IEventAggregator
{
  void Publish(object message);

  IDisposable Subscribe(Func<object, bool> handler);
}

/// <summary>
/// Base interface for all states.
/// </summary>
public interface IState<TState> where TState : struct, Enum
{
  TState Id { get; }

  /// <summary>
  /// Indicates hierarchical state.
  /// </summary>
  bool IsComposite { get; }

  /// <summary>
  /// Outcome-based transitions local to this (sub)machine.
  /// If the current state cannot resolve an outcome, bubbling occurs to parent composite.
  /// </summary>
  IReadOnlyDictionary<Result, TState> Transitions { get; }

  void OnEnter(Context<TState> context);

  // Transition hooks:
  void OnEntering(Context<TState> context);  // before entering

  // entered
  void OnExit(Context<TState> context);      // leaving
}

/// <summary>
/// A simple base implementation for states with convenient transition builder.
/// </summary>
public abstract class BaseState<TState> : IState<TState> where TState : struct, Enum
{
  private readonly Dictionary<Result, TState> _transitions = new();

  protected BaseState(TState id) => Id = id;

  public TState Id { get; }

  public virtual bool IsComposite => false;

  public IReadOnlyDictionary<Result, TState> Transitions => _transitions;

  public void AddTransition(Result outcome, TState target)
  {
    _transitions[outcome] = target;
  }

  public virtual void OnEnter(Context<TState> context)
  { }

  public virtual void OnEntering(Context<TState> context)
  { }

  public virtual void OnExit(Context<TState> context)
  { }
}

/// <summary>
/// A base class for command states, adding no behavior itself (machine handles timer/subscriptions).
/// </summary>
public abstract class CommandState<TState> : BaseState<TState>, ICommandState<TState> where TState : struct, Enum
{
  protected CommandState(TState id) : base(id)
  {
  }

  public virtual new Func<object, bool> MessageFilter => _ => true;

  public virtual int? TimeoutOverrideMs => null;

  Func<object, bool> ICommandState<TState>.MessageFilter => MessageFilter;

  int? ICommandState<TState>.TimeoutMs => TimeoutOverrideMs;

  public virtual void OnMessage(Context<TState> context, object message)
  { }

  public virtual void OnTimeout(Context<TState> context)
  { }
}

/// <summary>
/// A base class for composite states. The submachine is injected/assigned externally.
/// </summary>
public abstract class CompositeState<TState> : BaseState<TState>, ICompositeState<TState> where TState : struct, Enum
{
  protected CompositeState(TState id) : base(id)
  {
  }

  public override bool IsComposite => true;

  public StateMachine<TState> Submachine { get; internal set; } = default!;
}

/// <summary>
/// Context passed to every state. Provides a "Parameter" and a NextState(Result) trigger.
/// </summary>
public sealed class Context<TState> where TState : struct, Enum
{
  private readonly StateMachine<TState> _machine;

  internal Context(StateMachine<TState> machine) => _machine = machine;

  /// <summary>
  /// Arbitrary parameter provided by caller to the current action.
  /// </summary>
  public string Parameter { get; set; } = string.Empty;

  /// <summary>
  /// Signals transitioning by outcome. This uses the current state's mapping,
  /// and if none exists locally (composite submachine exhausted),
  /// it bubbles to the parent state's OnExit and applies the parent's mapping.
  /// </summary>
  public void NextState(Result result) => _machine.InternalNextState(result);
}

public sealed class EventAggregator : IEventAggregator
{
  private readonly List<Func<object, bool>> _subscribers = new();

  public void Publish(object message)
  {
    // Fan-out; handlers decide whether to consume or ignore.
    foreach (var sub in _subscribers.ToArray())
    {
      try
      {
        sub(message);
      }
      catch
      {
        // Swallow to avoid breaking publication loop.
      }
    }
  }

  public IDisposable Subscribe(Func<object, bool> handler)
  {
    if (handler == null) throw new ArgumentNullException(nameof(handler));
    _subscribers.Add(handler);

    return new Subscription(() => _subscribers.Remove(handler));
  }

  private sealed class Subscription : IDisposable
  {
    private readonly Action _unsubscribe;
    private int _disposed;

    public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;

    public void Dispose()
    {
      if (Interlocked.Exchange(ref _disposed, 1) == 0)
        _unsubscribe();
    }
  }
}

/// <summary>
/// The generic, enum-driven state machine with hierarchical bubbling and command-state timeout handling.
/// </summary>
public sealed class StateMachine<TState> where TState : struct, Enum
{
  private readonly IEventAggregator? _eventAggregator;
  private readonly ICompositeState<TState>? _ownerCompositeState;
  private readonly StateMachine<TState>? _parentMachine;
  private readonly Dictionary<TState, IState<TState>> _states = new();
  private IState<TState>? _current;
  private TState _initial;
  private bool _started;
  private IDisposable? _subscription;
  private CancellationTokenSource? _timeoutCts;

  public StateMachine(IEventAggregator? eventAggregator = null)
  {
    _eventAggregator = eventAggregator;
  }

  private StateMachine(StateMachine<TState> parentMachine, ICompositeState<TState> ownerCompositeState, IEventAggregator? eventAggregator)
  {
    _parentMachine = parentMachine;
    _ownerCompositeState = ownerCompositeState;
    _eventAggregator = eventAggregator;
  }

  public Context<TState> Context { get; } = default!;
  public int DefaultTimeoutMs { get; set; } = 3000;

  public void RegisterState(IState<TState> state)
  {
    if (state == null) throw new ArgumentNullException(nameof(state));
    _states[state.Id] = state;

    // Wire composite submachine instance if needed.
    if (state is ICompositeState<TState> comp)
    {
      comp.Submachine = new StateMachine<TState>(this, comp, _eventAggregator)
      {
        DefaultTimeoutMs = DefaultTimeoutMs
      };
    }
  }

  public void SetInitial(TState initial) => _initial = initial;

  /// <summary>
  /// Starts the machine at the initial state.
  /// </summary>
  public void Start(string parameter = "")
  {
    if (_started) throw new InvalidOperationException("State machine already started.");
    if (!_states.TryGetValue(_initial, out var initialState))
      throw new InvalidOperationException($"Initial state '{_initial}' is not registered.");

    _started = true;
    var ctx = new Context<TState>(this) { Parameter = parameter };
    typeof(StateMachine<TState>)
        .GetProperty(nameof(Context))!
        .SetValue(this, ctx);

    EnterState(initialState);
  }

  /// <summary>
  /// Internal transition logic used by Context.NextState.
  /// </summary>
  internal void InternalNextState(Result outcome)
  {
    if (_current == null)
      throw new InvalidOperationException("No current state.");

    var current = _current;

    // 1) Try local mapping (submachine level).
    if (current.Transitions.TryGetValue(outcome, out var target))
    {
      ExitCurrent();

      var next = _states[target];

      EnterState(next);
      return;
    }

    // 2) If this is within a submachine and cannot resolve mapping,
    // bubble up: invoke parent's OnExit, then parent machine transitions.
    if (_parentMachine != null && _ownerCompositeState != null)
    {
      // Leave last sub-state
      ExitCurrent();

      // Parent composite state's exit hook
      _ownerCompositeState.OnExit(Context);

      // Continue from parent machine using the same outcome
      _parentMachine.InternalNextState(outcome);
      return;
    }

    // 3) Top-level cannot resolve: this is terminal; just exit.
    ExitCurrent();
  }

  private void CancelTimerAndSubscription()
  {
    try { _subscription?.Dispose(); } catch { /* ignore */ }

    _subscription = null;

    try { _timeoutCts?.Cancel(); } catch { /* ignore */ }

    _timeoutCts?.Dispose();
    _timeoutCts = null;
  }

  private void EnterState(IState<TState> state)
  {
    _current = state;
    state.OnEntering(Context);
    state.OnEnter(Context);

    // If composite: start its submachine at its own initial (must be set).
    if (state is ICompositeState<TState> comp)
    {
      // inherit default timeout
      comp.Submachine.DefaultTimeoutMs = DefaultTimeoutMs;

      // Compose: ensure submachine has initial and is registered.
      // Submachine will drive transitions until it bubbles up
      comp.Submachine.Start(Context.Parameter);
      return;
    }

    // If command state: subscribe to aggregator and start timeout
    if (state is ICommandState<TState> cmd)
    {
      SetupCommandState(cmd);
    }
  }

  private void ExitCurrent()
  {
    CancelTimerAndSubscription();
    _current?.OnExit(Context);
  }

  private void SetupCommandState(ICommandState<TState> cmd)
  {
    CancelTimerAndSubscription();

    // Subscribe to messages
    if (_eventAggregator != null)
    {
      _subscription = _eventAggregator.Subscribe(msg =>
      {
        // Filter before delivery
        if (!cmd.MessageFilter(msg)) return false;

        // Cancel timeout upon first relevant message delivery
        _timeoutCts?.Cancel();

        cmd.OnMessage(Context, msg);
        return true;
      });
    }

    // Start timeout
    var timeoutMs = cmd.TimeoutMs ?? DefaultTimeoutMs;
    _timeoutCts = new CancellationTokenSource();

    _ = Task.Run(async () =>
    {
      try
      {
        await Task.Delay(timeoutMs, _timeoutCts.Token);
        // If we arrived here, no message within timeout
        cmd.OnTimeout(Context);
      }
      catch (TaskCanceledException)
      {
        // Timer cancelled by message or exit
      }
    });
  }
}
