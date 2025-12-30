// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

/*
using System;
using System.Threading.Tasks;

namespace Lite.StateMachine.Tests.StateTests;

[TestClass]
public class CommandStateTests
{
  public const string MessageTypeTest = "MessageType-Sent";
  public const string ParameterKeyTest = "TestKey";
  public const string TestValueBegin = "Initial-Value";
  public const string TestValueEnd = "Expected-Value";

  public enum ParentState
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

    var machine = new StateMachine<ParentState>(eventAggregator: aggregator)
    {
      // Set default timeout to 3 seconds (can override per-command state)
      DefaultCommandTimeoutMs = 3000,
    };

    // Register top-level states
    machine.RegisterState<StartState>(ParentState.Start);
    machine.RegisterState<ProcessingState>(ParentState.Processing, subStates: (sub) =>
    {
      // Register sub-states inside Processing's submachine
      sub.RegisterState<LoadState>(ParentState.Load);
      sub.RegisterState<ValidateState>(ParentState.Validate);
      sub.SetInitial(ParentState.Load);
    });
    machine.RegisterState<AwaitMessageState>(ParentState.AwaitMessage);
    machine.RegisterState<Workflow_DoneState>(ParentState.Done);
    machine.RegisterState<ErrorState>(ParentState.Error);
    machine.RegisterState<FailedState>(ParentState.Failed);

    // Set initial state
    machine.SetInitial(ParentState.Start);

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
  public sealed class AwaitMessageState : CommandState<ParentState>
  {
    public AwaitMessageState()
     : base(ParentState.AwaitMessage)
    {
      AddTransition(Result.Ok, ParentState.Done);
      AddTransition(Result.Error, ParentState.Error);
      AddTransition(Result.Failure, ParentState.Failed);
    }

    /// <summary>Gets the optional override of the default timeout.</summary>
    public override int? TimeoutOverrideMs => 3000;

    // Filter messages (optional). Here we only accept string messages that begin with "go".
    ////public override Func<object, bool> MessageFilter => msg =>
    ////  msg is string s && s.StartsWith("go", StringComparison.OrdinalIgnoreCase);

    public override void OnEnter(Context<ParentState> context) =>
      Console.WriteLine("[AwaitMessage] OnEnter (subscribed; awaiting message)");

    public override void OnEntering(Context<ParentState> context) =>
      Console.WriteLine("[AwaitMessage] OnEntering");

    public override void OnExit(Context<ParentState> context) =>
      Console.WriteLine("[AwaitMessage] OnExit (unsubscribed; timer cancelled)");

    public override void OnMessage(Context<ParentState> context, object message)
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

    public override void OnTimeout(Context<ParentState> context)
    {
      Console.WriteLine("[AwaitMessage] OnTimeout: no messages received in time.");
      context.NextState(Result.Failure);
    }
  }

  // Terminal states
  public sealed class Workflow_DoneState : BaseState<ParentState>
  {
    public Workflow_DoneState()
      : base(ParentState.Done)
    {
    }

    public override void OnEnter(Context<ParentState> context) =>
      Console.WriteLine("[Done] OnEnter — workflow complete.");

    public override void OnEntering(Context<ParentState> context) =>
      Console.WriteLine("[Done] OnEntering");

    public override void OnExit(Context<ParentState> context) =>
      Console.WriteLine("[Done] OnExit");
  }

  public sealed class ErrorState : BaseState<ParentState>
  {
    public ErrorState()
      : base(ParentState.Error)
    {
    }

    public override void OnEnter(Context<ParentState> context) =>
      Console.WriteLine("[Error] OnEnter");
  }

  public sealed class FailedState : BaseState<ParentState>
  {
    public FailedState()
      : base(ParentState.Failed)
    {
    }

    public override void OnEnter(Context<ParentState> context) =>
      Console.WriteLine("[Failed] OnEnter");
  }

  // Sub-state: Load (belongs to Processing submachine)
  public sealed class LoadState : BaseState<ParentState>
  {
    public LoadState()
      : base(ParentState.Load)
    {
      AddTransition(Result.Ok, ParentState.Validate);
      AddTransition(Result.Error, ParentState.Validate);   // Example: still go validate to confirm
      AddTransition(Result.Failure, ParentState.Validate); // Example: still go validate
    }

    public override void OnEnter(Context<ParentState> context)
    {
      Console.WriteLine("[Load] OnEnter (loading resources)");

      // Simulate outcome
      context.NextState(Result.Ok);
    }

    public override void OnEntering(Context<ParentState> context) =>
      Console.WriteLine("[Load] OnEntering (sub)");

    public override void OnExit(Context<ParentState> context) =>
      Console.WriteLine("[Load] OnExit (sub)");
  }

  // Composite state: Processing (submachine controls Load -> Validate)
  public sealed class ProcessingState : CompositeState<ParentState>
  {
    public ProcessingState()
      : base(ParentState.Processing)
    {
      // When submachine is done and bubbles Outcome:
      // This parent state's transitions will be applied.
      AddTransition(Result.Ok, ParentState.AwaitMessage);
      AddTransition(Result.Error, ParentState.Error);
      AddTransition(Result.Failure, ParentState.Failed);
    }

    public override void OnEnter(Context<ParentState> context) =>
      Console.WriteLine("[Processing] OnEnter (starting submachine)");

    public override void OnEntering(Context<ParentState> context) =>
      Console.WriteLine("[Processing] OnEntering");

    public override void OnExit(Context<ParentState> context) =>
      Console.WriteLine("[Processing] OnExit (submachine exhausted)");
  }

  // Regular state: Start
  public sealed class StartState : BaseState<ParentState>
  {
    public StartState()
      : base(ParentState.Start)
    {
      // Decide where to go based on outcome
      AddTransition(Result.Ok, ParentState.Processing);
      AddTransition(Result.Error, ParentState.Error);
      AddTransition(Result.Failure, ParentState.Failed);
    }

    public override void OnEnter(Context<ParentState> context)
    {
      Console.WriteLine($"[Start] OnEnter, Parameter='{context.Parameters[ParameterKeyTest]}'");

      // Simulate work; then decide outcome
      context.NextState(Result.Ok);
    }

    public override void OnEntering(Context<ParentState> context) =>
      Console.WriteLine("[Start] OnEntering");

    public override void OnExit(Context<ParentState> context) =>
      Console.WriteLine("[Start] OnExit");
  }

  // Sub-state: Validate (last sub-state; no local transition on Ok -> bubbles up)
  public sealed class ValidateState : BaseState<ParentState>
  {
    public ValidateState()
      : base(ParentState.Validate)
    {
      // Local mapping only for non-OK; OK intentionally not mapped to demonstrate bubble-up.
      AddTransition(Result.Error, ParentState.Validate);   // example self-loop error check
      AddTransition(Result.Failure, ParentState.Validate); // example self-loop failure check
    }

    public override void OnEnter(Context<ParentState> context)
    {
      Console.WriteLine("[Validate] OnEnter (checking data)");

      // Suppose validation passed: bubble up to Processing
      context.NextState(Result.Ok);
    }

    public override void OnEntering(Context<ParentState> context)
    {
      Console.WriteLine("[Validate] OnEntering (sub)");
    }

    public override void OnExit(Context<ParentState> context)
    {
      Console.WriteLine("[Validate] OnExit (sub)");
    }
  }
}
*/
