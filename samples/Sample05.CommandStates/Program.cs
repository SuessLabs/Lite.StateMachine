// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using Lite.StateMachine;

namespace Sample05.CommandStates;

/// <summary>
///   Demonstrates command states that request simulated device actions and wait
///   for their results in OnMessage before choosing the next state.
/// </summary>
/// <remarks>
///   State1 connects successfully;
///   State2 deliberately fails to start the device;
///   State3 recovers by closing its safety door;
///   State4 simulates a sensor error;
///   and State5 cleans up and disconnects.
///
///   Console output explains each action, result, and transition
///   through the success, failure, and error paths.
///
///   All states and the simulated worker are defined in this file; no hardware is required.
/// </remarks>
internal class Program
{
  private enum StateId
  {
    State1,
    State2,
    State3,
    State4,
    State5,
  }

  private static async Task Main()
  {
    Console.WriteLine("Command states: perform actions and follow their results.");
    Console.WriteLine("Expected: State1 --Success--> State2 --Failure--> State3 --Success--> State4 --Error--> State5.");
    var events = new EventAggregator();

    // A stand-in for a device or application service. Failures are deliberate
    // so every run demonstrates success, expected failure, and error handling.
    using var worker = events.Subscribe(message =>
    {
      if (message is not ActionCommand command)
        return;

      var result = command.State switch
      {
        StateId.State2 => Result.Failure,
        StateId.State4 => Result.Error,
        _ => Result.Success,
      };
      Console.WriteLine($"  Worker: {command.Action} => {result}");
      events.Publish(new ActionCompleted(command.State, result));
    },
    typeof(ActionCommand));

    var machine = new StateMachine<StateId>(eventAggregator: events)
      .RegisterState<State1>(StateId.State1, onSuccess: StateId.State2)
      .RegisterState<State2>(StateId.State2, onFailure: StateId.State3)
      .RegisterState<State3>(StateId.State3, onSuccess: StateId.State4)
      .RegisterState<State4>(StateId.State4, onError: StateId.State5)
      .RegisterState<State5>(StateId.State5);

    using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    await machine.RunAsync(StateId.State1, cancellation.Token);

    if (machine.Context.CurrentStateId != StateId.State5 ||
        !machine.Context.ParameterAsBool("Completed"))
    {
      throw new InvalidOperationException("The sample did not complete its expected actions.");
    }

    Console.WriteLine("Done: success, failure recovery, and error cleanup were demonstrated.");
  }

  private sealed record ActionCommand(StateId State, string Action);

  private sealed record ActionCompleted(StateId State, Result Result);

  // Shared command plumbing; each numbered state below defines its action.
  private abstract class CommandState : ICommandState<StateId>
  {
    public IReadOnlyCollection<Type> SubscribedMessageTypes => [typeof(ActionCompleted)];

    protected abstract string Action { get; }

    public Task OnEnter(Context<StateId> context)
    {
      Console.WriteLine($"{context.CurrentStateId}: requesting '{Action}'. Waiting for ActionCompleted in OnMessage.");
      context.EventAggregator!.Publish(new ActionCommand(context.CurrentStateId, Action));

      // Returning from OnEnter does not advance a command state.
      // OnMessage must call NextState after receiving the action's result.
      return Task.CompletedTask;
    }

    public Task OnEntering(Context<StateId> context) => Task.CompletedTask;

    public Task OnExit(Context<StateId> context)
    {
      Console.WriteLine($"{context.CurrentStateId}: exiting.");
      return Task.CompletedTask;
    }

    public Task OnMessage(Context<StateId> context, object message)
    {
      if (message is ActionCompleted completed && completed.State == context.CurrentStateId)
      {
        Console.WriteLine($"{context.CurrentStateId}.OnMessage: action returned {completed.Result}.");
        if (context.CurrentStateId == StateId.State5 && completed.Result == Result.Success)
          context.Parameters["Completed"] = true;

        context.NextState(completed.Result);
      }

      return Task.CompletedTask;
    }

    public Task OnTimeout(Context<StateId> context)
    {
      Console.WriteLine($"{context.CurrentStateId}: no action result arrived; ending with Error.");
      context.NextState(Result.Error);
      return Task.CompletedTask;
    }
  }

  private sealed class State1 : CommandState
  {
    protected override string Action => "Connect to the device (success expected)";
  }

  private sealed class State2 : CommandState
  {
    protected override string Action => "Start the device while its safety door is open (failure expected)";
  }

  private sealed class State3 : CommandState
  {
    protected override string Action => "Recover from State2's failure by closing the safety door (success expected)";
  }

  private sealed class State4 : CommandState
  {
    protected override string Action => "Read a sensor with a simulated communication fault (error expected)";
  }

  private sealed class State5 : CommandState
  {
    protected override string Action => "Clean up after State4's error and disconnect (success expected)";
  }
}
