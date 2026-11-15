// Copyright Xeno Innovations, Inc. 2026
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lite.StateMachine;

namespace Sample06.PassingEvents;

/// <summary>
///   Demonstrates passing typed order data through command states using the
///   built-in event aggregator and each state's OnMessage handler.
/// </summary>
/// <remarks>
///   State1 creates an order for three notebooks;
///   State2 awaits a simulated price lookup and calculates the total;
///   State3 prints the receipt.
///
///   The shared context holds each outgoing payload until the next state subscribes and publishes it,
///   because the aggregator does not queue events for future states.
///
///   Console output traces the messages and their data.
///   All states and message types are defined in this file.
/// </remarks>
internal class Program
{
  private enum StateId
  {
    State1,
    State2,
    State3,
  }

  private static async Task Main()
  {
    Console.WriteLine("Passing events: State1 creates an order, State2 prices it, State3 prints the receipt.");
    var events = new EventAggregator();
    var machine = new StateMachine<StateId>(eventAggregator: events)
      .RegisterState<State1>(StateId.State1, onSuccess: StateId.State2)
      .RegisterState<State2>(StateId.State2, onSuccess: StateId.State3)
      .RegisterState<State3>(StateId.State3);

    using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    await machine.RunAsync(StateId.State1, cancellation.Token);

    if (!machine.Context.ParameterAsBool("Completed"))
      throw new InvalidOperationException("The sample did not produce its receipt.");

    Console.WriteLine("Done: each state received its typed payload through OnMessage.");
  }

  private sealed record OrderCreated(string Product, int Quantity);

  private sealed record PriceRequested(string Product, int Quantity);

  private sealed record OrderPriced(string Product, int Quantity, decimal Total);

  // The aggregator only delivers to the current command state; it does not
  // queue events for future states. Context carries each outgoing payload until
  // the next state's OnEnter publishes it after that state's subscription exists.
  private abstract class EventState : ICommandState<StateId>
  {
    public abstract IReadOnlyCollection<Type> SubscribedMessageTypes { get; }

    public abstract Task OnEnter(Context<StateId> context);

    public Task OnEntering(Context<StateId> context) => Task.CompletedTask;

    public Task OnExit(Context<StateId> context) => Task.CompletedTask;

    public abstract Task OnMessage(Context<StateId> context, object message);

    public Task OnTimeout(Context<StateId> context)
    {
      Console.WriteLine($"{context.CurrentStateId}: timed out waiting for its event.");
      context.NextState(Result.Error);
      return Task.CompletedTask;
    }
  }

  private sealed class State1 : EventState
  {
    public override IReadOnlyCollection<Type> SubscribedMessageTypes => [typeof(OrderCreated)];

    public override Task OnEnter(Context<StateId> context)
    {
      Console.WriteLine("State1: publishing OrderCreated for 3 notebooks.");
      context.EventAggregator!.Publish(new OrderCreated("Notebook", 3));
      return Task.CompletedTask;
    }

    public override Task OnMessage(Context<StateId> context, object message)
    {
      if (message is OrderCreated order)
      {
        Console.WriteLine($"State1.OnMessage: received {order.Product}, quantity {order.Quantity}; passing data to State2.");
        context.Parameters["NextMessage"] = new PriceRequested(order.Product, order.Quantity);
        context.NextState(Result.Success);
      }

      return Task.CompletedTask;
    }
  }

  private sealed class State2 : EventState
  {
    public override IReadOnlyCollection<Type> SubscribedMessageTypes => [typeof(PriceRequested)];

    public override Task OnEnter(Context<StateId> context)
    {
      Console.WriteLine("State2: publishing the PriceRequested payload passed from State1.");
      context.EventAggregator!.Publish((PriceRequested)context.Parameters["NextMessage"]!);
      return Task.CompletedTask;
    }

    public override async Task OnMessage(Context<StateId> context, object message)
    {
      if (message is not PriceRequested order)
        return;

      Console.WriteLine($"State2.OnMessage: looking up the price for {order.Product}...");

      await Task.Delay(100); // Simulate an asynchronous price lookup.

      var total = order.Quantity * 4.50m;
      Console.WriteLine($"State2.OnMessage: {order.Quantity} x 4.50 = {total:F2}; passing data to State3.");
      context.Parameters["NextMessage"] = new OrderPriced(order.Product, order.Quantity, total);
      context.NextState(Result.Success);
    }
  }

  private sealed class State3 : EventState
  {
    public override IReadOnlyCollection<Type> SubscribedMessageTypes => [typeof(OrderPriced)];

    public override Task OnEnter(Context<StateId> context)
    {
      Console.WriteLine("State3: publishing the OrderPriced payload passed from State2.");
      context.EventAggregator!.Publish((OrderPriced)context.Parameters["NextMessage"]!);
      return Task.CompletedTask;
    }

    public override Task OnMessage(Context<StateId> context, object message)
    {
      if (message is OrderPriced order)
      {
        Console.WriteLine($"State3.OnMessage: receipt - {order.Quantity} x {order.Product}, total {order.Total:F2}.");
        context.Parameters.Remove("NextMessage");
        context.Parameters["Completed"] = true;
        context.NextState(Result.Success);
      }

      return Task.CompletedTask;
    }
  }
}
