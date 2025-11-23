// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

namespace Sample.Mk3;

using System;
using LiteState.Mk3;

// Your custom enum; the machine is generic and agnostic to which enum you use.
public enum Workflow
{
  Root,       // Composite parent
  Validate,   // Child 1
  Process,    // Child 2
  Persist,    // Child 3
  Done        // Terminal top-level state
}

#region Test App

internal class Program
{
  private static void Main()
  {
    var sm = new StateMachine<Workflow>();

    // Build composite parent with its children
    var root = new RootState()
        .AddChild(new ValidateState())
        .AddChild(new ProcessState())
        .AddChild(new PersistState());

    // Register all states
    sm.Register(root);
    sm.Register(new DoneState());

    // Run with a parameter; returns overall pass/fail (bool)
    bool success = sm.Run(Workflow.Root, parameter: "Hello Damian!");

    Console.WriteLine($"State machine finished: {(success ? "SUCCESS" : "FAILURE")}");
  }
}

#endregion Test App

// A simple terminal state
public sealed class DoneState : StateBase<Workflow>
{
  public DoneState() : base(Workflow.Done)
  {
  }

  public override bool OnEnter(Context<Workflow> ctx)
  {
    Console.WriteLine("[Done] OnEnter");
    return true;
  }

  public override bool OnEntering(Context<Workflow> ctx)
  {
    Console.WriteLine("[Done] OnEntering");
    return true;
  }

  public override bool OnExit(Context<Workflow> ctx)
  {
    Console.WriteLine("[Done] OnExit (End of run)");
    return true;
  }
}

// Child 3
public sealed class PersistState : StateBase<Workflow>
{
  public PersistState() : base(Workflow.Persist)
  {
  }

  public override bool OnEnter(Context<Workflow> ctx)
  {
    Console.WriteLine("[Persist] OnEnter");
    return true;
  }

  public override bool OnEntering(Context<Workflow> ctx)
  {
    Console.WriteLine("[Persist] OnEntering");
    return true;
  }

  public override bool OnExit(Context<Workflow> ctx)
  {
    Console.WriteLine("[Persist] OnExit");
    return true;
  }
}

// Child 2
public sealed class ProcessState : StateBase<Workflow>
{
  public ProcessState() : base(Workflow.Process)
  {
  }

  public override bool OnEnter(Context<Workflow> ctx)
  {
    Console.WriteLine("[Process] OnEnter");
    // Example: You could also request a next top-level state here.
    // ctx.NextState(Workflow.Done);
    return true;
  }

  public override bool OnEntering(Context<Workflow> ctx)
  {
    Console.WriteLine("[Process] OnEntering");
    return true;
  }

  public override bool OnExit(Context<Workflow> ctx)
  {
    Console.WriteLine("[Process] OnExit");
    return true;
  }
}

// Parent composite state
public sealed class RootState : CompositeState<Workflow>
{
  public RootState() : base(Workflow.Root)
  {
  }

  public override bool OnEnter(Context<Workflow> ctx)
  {
    Console.WriteLine("[Root] OnEnter");
    return true;
  }

  public override bool OnEntering(Context<Workflow> ctx)
  {
    Console.WriteLine($"[Root] OnEntering (Parameter='{ctx.Parameter}')");
    return true;
  }

  public override bool OnExit(Context<Workflow> ctx)
  {
    Console.WriteLine("[Root] OnExit -> NextState(Done)");
    ctx.NextState(Workflow.Done);
    return true;
  }
}

// Child 1
public sealed class ValidateState : StateBase<Workflow>
{
  public ValidateState() : base(Workflow.Validate)
  {
  }

  public override bool OnEnter(Context<Workflow> ctx)
  {
    Console.WriteLine($"[Validate] OnEnter (Parameter length={ctx.Parameter?.Length ?? 0})");
    // Simulate validation pass/fail; return false to stop machine
    return !string.IsNullOrWhiteSpace(ctx.Parameter);
  }

  public override bool OnEntering(Context<Workflow> ctx)
  {
    Console.WriteLine("[Validate] OnEntering");
    return true;
  }

  public override bool OnExit(Context<Workflow> ctx)
  {
    Console.WriteLine("[Validate] OnExit");
    return true;
  }
}
