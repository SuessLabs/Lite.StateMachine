// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

/*
namespace Sample.Mk4.SampleB;

using System;
using LiteState.Mk4b;

#region Test App

public static class TestApp
{
  public static void Run()
  {
    var aggregator = new EventAggregator();

    var machine = new StateMachine<WorkflowState>(aggregator)
    {
      DefaultCommandTimeoutMs = 3000 // as requested (can override per-command state)
    };

    // Register top-level states
    machine.RegisterState(new StartState());

    var processing = new ProcessingState();
    machine.RegisterState(processing);
    machine.RegisterState(new AwaitMessageState());
    machine.RegisterState(new DoneState());
    machine.RegisterState(new ErrorState());
    machine.RegisterState(new FailedState());

    // Register sub-states inside Processing's submachine
    var sub = processing.Submachine;
    sub.RegisterState(new LoadState());
    sub.RegisterState(new ValidateState());

    // Set initials
    machine.SetInitial(WorkflowState.Start);
    sub.SetInitial(WorkflowState.Load);

    // Start workflow
    machine.Start(parameter: "Hello Damian!");

    // Simulate publishing a message to complete command state
    // Publish after ~1 second (within timeout) — change timing to test OnTimeout
    Task.Delay(1000).ContinueWith(_ => aggregator.Publish("go-next"));

    // Keep console alive to observe async timeout/message handling
    Task.Delay(5000).Wait();
  }
}

#endregion Test App

// Your custom enum (the machine is agnostic to this type)
public enum WorkflowState
{
  Start,
  Processing,     // Composite state
  Load,           // Sub-state of Processing
  Validate,       // Sub-state of Processing
  AwaitMessage,   // Command state
  Done,
  Error,
  Failed
}

// Regular state: Start
public sealed class StartState : BaseState<WorkflowState>
{
  public StartState() : base(WorkflowState.Start)
  {
    // Decide where to go based on outcome
    AddTransition(Result.Ok, WorkflowState.Processing);
    AddTransition(Result.Error, WorkflowState.Error);
    AddTransition(Result.Failure, WorkflowState.Failed);
  }

  public override void OnEntering(Context<WorkflowState> context)
  {
    Console.WriteLine("[Start] OnEntering");
  }
  public override void OnEnter(Context<WorkflowState> context)
  {
    Console.WriteLine($"[Start] OnEnter, Parameter='{context.Parameter}'");

    // Simulate work; then decide outcome
    context.NextState(Result.Ok);
  }
  public override void OnExit(Context<WorkflowState> context)
  {
    Console.WriteLine("[Start] OnExit");
  }
}

// Composite state: Processing (submachine controls Load -> Validate)
public sealed class ProcessingState : CompositeState<WorkflowState>
{
  public ProcessingState() : base(WorkflowState.Processing)
  {
    // When submachine is done and bubbles Outcome:
    // This parent state's transitions will be applied.
    AddTransition(Result.Ok, WorkflowState.AwaitMessage);
    AddTransition(Result.Error, WorkflowState.Error);
    AddTransition(Result.Failure, WorkflowState.Failed);
  }

  public override void OnEntering(Context<WorkflowState> context)
  {
    Console.WriteLine("[Processing] OnEntering");
  }
  public override void OnEnter(Context<WorkflowState> context)
  {
    Console.WriteLine("[Processing] OnEnter (starting submachine)");
  }
  public override void OnExit(Context<WorkflowState> context)
  {
    Console.WriteLine("[Processing] OnExit (submachine exhausted)");
  }
}

// Sub-state: Load (belongs to Processing submachine)
public sealed class LoadState : BaseState<WorkflowState>
{
  public LoadState() : base(WorkflowState.Load)
  {
    AddTransition(Result.Ok, WorkflowState.Validate);
    AddTransition(Result.Error, WorkflowState.Validate);   // Example: still go validate to confirm
    AddTransition(Result.Failure, WorkflowState.Validate); // Example: still go validate
  }

  public override void OnEntering(Context<WorkflowState> context)
  {
    Console.WriteLine("[Load] OnEntering (sub)");
  }
  public override void OnEnter(Context<WorkflowState> context)
  {
    Console.WriteLine("[Load] OnEnter (loading resources)");

    // Simulate outcome
    context.NextState(Result.Ok);
  }
  public override void OnExit(Context<WorkflowState> context)
  {
    Console.WriteLine("[Load] OnExit (sub)");
  }
}

// Sub-state: Validate (last sub-state; no local transition on Ok -> bubbles up)
public sealed class ValidateState : BaseState<WorkflowState>
{
  public ValidateState() : base(WorkflowState.Validate)
  {
    // Local mapping only for non-OK; OK intentionally not mapped to demonstrate bubble-up.
    AddTransition(Result.Error, WorkflowState.Validate);   // example self-loop error check
    AddTransition(Result.Failure, WorkflowState.Validate); // example self-loop failure check
  }

  public override void OnEntering(Context<WorkflowState> context)
  {
    Console.WriteLine("[Validate] OnEntering (sub)");
  }
  public override void OnEnter(Context<WorkflowState> context)
  {
    Console.WriteLine("[Validate] OnEnter (checking data)");

    // Suppose validation passed: bubble up to Processing
    context.NextState(Result.Ok);
  }
  public override void OnExit(Context<WorkflowState> context)
  {
    Console.WriteLine("[Validate] OnExit (sub)");
  }
}

// Command state: AwaitMessage (listens to event aggregator; timeout defaults to 3000ms)
public sealed class AwaitMessageState : CommandState<WorkflowState>
{
  public AwaitMessageState() : base(WorkflowState.AwaitMessage)
  {
    AddTransition(Result.Ok, WorkflowState.Done);
    AddTransition(Result.Error, WorkflowState.Error);
    AddTransition(Result.Failure, WorkflowState.Failed);
  }

  // Optionally override default timeout:
  // public override int? TimeoutOverrideMs => 5000;

  // Filter messages (optional). Here we only accept string messages that begin with "go".
  public override Func<object, bool> MessageFilter => msg =>
      msg is string s && s.StartsWith("go", StringComparison.OrdinalIgnoreCase);

  public override void OnEntering(Context<WorkflowState> context)
  {
    Console.WriteLine("[AwaitMessage] OnEntering");
  }
  public override void OnEnter(Context<WorkflowState> context)
  {
    Console.WriteLine("[AwaitMessage] OnEnter (subscribed; awaiting message)");
  }
  public override void OnExit(Context<WorkflowState> context)
  {
    Console.WriteLine("[AwaitMessage] OnExit (unsubscribed; timer cancelled)");
  }

  public override void OnMessage(Context<WorkflowState> context, object message)
  {
    Console.WriteLine($"[AwaitMessage] OnMessage: '{message}' (timeout cancelled)");
    // Decide outcome based on message content
    if (message is string s && s.Contains("error", StringComparison.OrdinalIgnoreCase))
      context.NextState(Result.Error);
    else
      context.NextState(Result.Ok);
  }

  public override void OnTimeout(Context<WorkflowState> context)
  {
    Console.WriteLine("[AwaitMessage] OnTimeout: no messages received in time.");
    context.NextState(Result.Failure);
  }
}

// Terminal states
public sealed class DoneState : BaseState<WorkflowState>
{
  public DoneState() : base(WorkflowState.Done) { }

  public override void OnEntering(Context<WorkflowState> context) => Console.WriteLine("[Done] OnEntering");
  public override void OnEnter(Context<WorkflowState> context) => Console.WriteLine("[Done] OnEnter — workflow complete.");
  public override void OnExit(Context<WorkflowState> context) => Console.WriteLine("[Done] OnExit");
}

public sealed class ErrorState : BaseState<WorkflowState>
{
  public ErrorState() : base(WorkflowState.Error) { }
  public override void OnEnter(Context<WorkflowState> context) => Console.WriteLine("[Error] OnEnter");
}

public sealed class FailedState : BaseState<WorkflowState>
{
  public FailedState() : base(WorkflowState.Failed) { }
  public override void OnEnter(Context<WorkflowState> context) => Console.WriteLine("[Failed] OnEnter");
}
*/
