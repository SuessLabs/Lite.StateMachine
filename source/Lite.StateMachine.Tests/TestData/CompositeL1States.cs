// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using static Lite.StateMachine.Tests.StateTests.CompositeStateTest;

namespace Lite.StateMachine.Tests.TestData;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

public class BaseState : IState<CompositeL1StateId>
{
  public void Log(string method, string message = "")
  {
    ////System.Console.WriteLine($"[{this.GetType().Name}] [{method}] {message}");
    System.Diagnostics.Debug.WriteLine($"[{this.GetType().Name}] [{method}] {message}");
  }

  public virtual Task OnEnter(Context<CompositeL1StateId> context)
  {
    Log("OnEnter");
    return Task.CompletedTask;
  }

  public virtual Task OnEntering(Context<CompositeL1StateId> context)
  {
    Log("OnEntering");
    return Task.CompletedTask;
  }

  public virtual Task OnExit(Context<CompositeL1StateId> context)
  {
    Log("OnExit");
    return Task.CompletedTask;
  }
}

public class CompositeL1_State1() : BaseState
{
  public override Task OnEnter(Context<CompositeL1StateId> context)
  {
    Log("OnEnter", "=> OK");
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}

/// <summary>Composite Parent State.</summary>
/// <param name="id">State Id.</param>
public class CompositeL1_State2() : BaseState
{
  public override Task OnEnter(Context<CompositeL1StateId> context)
  {
    Log("OnEnter", "=> OK");
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }

  public override Task OnExit(Context<CompositeL1StateId> context)
  {
    // NOTE: The true exit point of the state. You MUST return a Result here, else you'll hang!!
    Log("OnExit", "=> OK");
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}

public class CompositeL1_State2_Sub1() : BaseState
{
  public override Task OnEnter(Context<CompositeL1StateId> context)
  {
    Log("OnEnter", "=> GO-TO Child");
    context.Parameters.Add(ParameterSubStateEntered, SUCCESS);
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}

public class CompositeL1_State2_Sub2() : BaseState
{
  public override Task OnEnter(Context<CompositeL1StateId> context)
  {
    Log("OnEnter", "=> OK");
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}

public class CompositeL1_State3() : BaseState
{
  public override Task OnEnter(Context<CompositeL1StateId> context)
  {
    Log("OnEnter", "=> OK");
    context.NextState(Result.Ok);
    return Task.CompletedTask;
  }
}

#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
