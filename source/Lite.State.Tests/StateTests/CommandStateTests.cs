// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Lite.State.Tests.StateTests;

[TestClass]
public class CommandStateTests
{
  public const string MessageTypeTest = "MessageType-Sent";
  public const string ParameterKeyTest = "TestKey";
  public const string TestValueBegin = "Initial-Value";
  public const string TestValueEnd = "Expected-Value";

  public enum WorkflowState
  {
    Start,
    Processing,     // Composite state
    Load,           // Sub-state of Processing
    Validate,       // Sub-state of Processing
    AwaitMessage,   // Command state
    Done,
    Error,
    Failed,
  }

  [TestMethod]
  public void TransitionWithErrorToSuccessTest()
  {
    // Assemble
    var aggregator = new EventAggregator();

    var machine = new StateMachine<WorkflowState>(aggregator)
    {
      // Set default timeout to 3 seconds (can override per-command state)
      DefaultTimeoutMs = 3000,
    };

    // Register top-level states
    machine.RegisterState(WorkflowState.Start, () => new StartState());
    machine.RegisterState(WorkflowState.Processing, () => new ProcessingState());
    machine.RegisterState(WorkflowState.Processing, (sub) =>
    {
      // Register sub-states inside Processing's submachine
      sub.RegisterState(WorkflowState.Load, () => new LoadState());
      sub.RegisterState(WorkflowState.Validate, () => new ValidateState());
      sub.SetInitial(WorkflowState.Load);
    });
    machine.RegisterState(WorkflowState.AwaitMessage, () => new AwaitMessageState());
    machine.RegisterState(WorkflowState.Done, () => new DoneState());
    machine.RegisterState(WorkflowState.Error, () => new ErrorState());
    machine.RegisterState(WorkflowState.Failed, () => new FailedState());

    // Set initial state
    machine.SetInitial(WorkflowState.Start);

    // ====================
    // Act - Start workflow
    var ctx = new PropertyBag { { ParameterKeyTest, TestValueBegin } };
    machine.Start(ctx);

    // Act - Simulate publishing a message to complete command state
    // Publish after ~1 second (within timeout) — change timing to test OnTimeout
    var msgObject = new PropertyBag() { { MessageTypeTest, TestValueBegin } };
    Task.Delay(1000).ContinueWith(_ => aggregator.Publish(msgObject));

    // Keep console alive to observe async timeout/message handling
    Task.Delay(5000).Wait();

    // =================
    // Assert
    var ctxFinal = machine.Context.Parameters;
    Assert.IsNotNull(ctxFinal);
    Assert.AreEqual(TestValueEnd, ctxFinal[ParameterKeyTest]);
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
    ////public override Func<object, bool> MessageFilter => msg =>
    ////  msg is string s && s.StartsWith("go", StringComparison.OrdinalIgnoreCase);

    public override void OnEnter(Context<WorkflowState> context)
    {
      Console.WriteLine("[AwaitMessage] OnEnter (subscribed; awaiting message)");
    }

    public override void OnEntering(Context<WorkflowState> context)
    {
      Console.WriteLine("[AwaitMessage] OnEntering");
    }

    public override void OnExit(Context<WorkflowState> context)
    {
      Console.WriteLine("[AwaitMessage] OnExit (unsubscribed; timer cancelled)");
    }

    public override void OnMessage(Context<WorkflowState> context, object message)
    {
      Console.WriteLine($"[AwaitMessage] OnMessage: '{message}' (timeout cancelled)");

      if (message is PropertyBag prop &&
          prop is not null &&
          prop.ContainsKey(MessageTypeTest) &&
          prop[MessageTypeTest].Equals(TestValueBegin))
      {
        context.Parameters[ParameterKeyTest] = TestValueEnd;
        context.NextState(Result.Ok);
      }
      else
      {
        context.NextState(Result.Error);
      }

      // Decide outcome based on message content
      ////if (message is string s && s.Contains("error", StringComparison.OrdinalIgnoreCase))
      ////  context.NextState(Result.Error);
      ////else
      ////  context.NextState(Result.Ok);
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
    public DoneState() : base(WorkflowState.Done)
    {
    }

    public override void OnEnter(Context<WorkflowState> context) =>
      Console.WriteLine("[Done] OnEnter — workflow complete.");

    public override void OnEntering(Context<WorkflowState> context) =>
      Console.WriteLine("[Done] OnEntering");

    public override void OnExit(Context<WorkflowState> context) =>
      Console.WriteLine("[Done] OnExit");
  }

  public sealed class ErrorState : BaseState<WorkflowState>
  {
    public ErrorState() : base(WorkflowState.Error)
    {
    }

    public override void OnEnter(Context<WorkflowState> context) =>
      Console.WriteLine("[Error] OnEnter");
  }

  public sealed class FailedState : BaseState<WorkflowState>
  {
    public FailedState() : base(WorkflowState.Failed)
    {
    }

    public override void OnEnter(Context<WorkflowState> context) =>
      Console.WriteLine("[Failed] OnEnter");
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

    public override void OnEnter(Context<WorkflowState> context)
    {
      Console.WriteLine("[Load] OnEnter (loading resources)");

      // Simulate outcome
      context.NextState(Result.Ok);
    }

    public override void OnEntering(Context<WorkflowState> context) =>
      Console.WriteLine("[Load] OnEntering (sub)");

    public override void OnExit(Context<WorkflowState> context) =>
      Console.WriteLine("[Load] OnExit (sub)");
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

    public override void OnEnter(Context<WorkflowState> context) =>
      Console.WriteLine("[Processing] OnEnter (starting submachine)");

    public override void OnEntering(Context<WorkflowState> context) =>
      Console.WriteLine("[Processing] OnEntering");

    public override void OnExit(Context<WorkflowState> context) =>
      Console.WriteLine("[Processing] OnExit (submachine exhausted)");
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

    public override void OnEnter(Context<WorkflowState> context)
    {
      Console.WriteLine($"[Start] OnEnter, Parameter='{context.Parameters[ParameterKeyTest]}'");

      // Simulate work; then decide outcome
      context.NextState(Result.Ok);
    }

    public override void OnEntering(Context<WorkflowState> context) =>
      Console.WriteLine("[Start] OnEntering");

    public override void OnExit(Context<WorkflowState> context) =>
      Console.WriteLine("[Start] OnExit");
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

    public override void OnEnter(Context<WorkflowState> context)
    {
      Console.WriteLine("[Validate] OnEnter (checking data)");

      // Suppose validation passed: bubble up to Processing
      context.NextState(Result.Ok);
    }

    public override void OnEntering(Context<WorkflowState> context)
    {
      Console.WriteLine("[Validate] OnEntering (sub)");
    }

    public override void OnExit(Context<WorkflowState> context)
    {
      Console.WriteLine("[Validate] OnExit (sub)");
    }
  }
}
