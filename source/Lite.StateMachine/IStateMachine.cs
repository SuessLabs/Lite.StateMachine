// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lite.StateMachine;

/// <summary>
/// The generic, enum-driven state machine with hierarchical bubbling and command-state timeout handling.
/// </summary>
/// <typeparam name="TStateId">Type of State Id to use (i.e. enum, int, etc.).</typeparam>
public interface IStateMachine<TStateId>
  where TStateId : struct, Enum
{
  /// <summary>Gets the context payload passed between the states, and contains methods for transitioning to the next state.</summary>
  Context<TStateId> Context { get; }

  /// <summary>Gets or sets the default <see cref="ICommandState{TState}"/> timeout in milliseconds (3000ms default). Triggering OnTimeout in the event of elapsed time.</summary>
  int DefaultCommandTimeoutMs { get; set; }

  /// <summary>Gets or sets the default <see cref="IState{TState}"/> timeout in milliseconds (<see cref="Timeout.Infinite"/>ms default). Set timeout to ensure no stuck states (i.e., robotics).</summary>
  int DefaultStateTimeoutMs { get; set; }

  /// <summary>Gets the collection of all registered states.</summary>
  /// <remarks>
  ///   Exposed for validations, debugging, etc.
  ///   Previously: <![CDATA[Dictionary<TState, IState<TState>>]]>.
  /// </remarks>
  List<TStateId> States { get; }

  /// <summary>
  /// Registers a top-level composite parent state (has no parent state) and explicitly sets:
  /// - the initial child (initialChildStateId).
  /// - the next top-level transitions (nextOnOk, nextOnError, nextOnFailure).
  /// </summary>
  /// <param name="stateId">State identifier.</param>
  /// <param name="initialChildStateId">Initial child state identifier.</param>
  /// <param name="onSuccess">Transition to next state on success. NULL if last state to exit <see cref="StateMachine{TStateId}"/>.</param>
  /// <param name="onError">Optional transition to next state on error.</param>
  /// <param name="onFailure">Optional transition to next state on failure.</param>
  /// <returns>State machine instance.</returns>
  /// <typeparam name="TCompositeParent">Composite State Class.</typeparam>
  StateMachine<TStateId> RegisterComposite<TCompositeParent>(TStateId stateId, TStateId initialChildStateId, TStateId? onSuccess = null, TStateId? onError = null, TStateId? onFailure = null)
    where TCompositeParent : class, IState<TStateId>;

  /// <summary>Nested composite (child composite under a parent composite).</summary>
  /// <param name="stateId">State identifier.</param>
  /// <param name="parentStateId">Parent state identifier.</param>
  /// <param name="initialChildStateId">Initial child state identifier.</param>
  /// <param name="onSuccess">Transition to next state on success, or NULL if last state to exit <see cref="StateMachine{TStateId}"/>.</param>
  /// <param name="onError">Optional transition to next state on error.</param>
  /// <param name="onFailure">Optional transition to next state on failure.</param>
  /// <returns>State machine instance.</returns>
  /// <typeparam name="TCompositeParent">Composite State Class.</typeparam>
  StateMachine<TStateId> RegisterCompositeChild<TCompositeParent>(TStateId stateId, TStateId parentStateId, TStateId initialChildStateId, TStateId? onSuccess = null, TStateId? onError = null, TStateId? onFailure = null)
    where TCompositeParent : class, IState<TStateId>;

  /// <summary>Registers a regular or command state (optionally with transitions).</summary>
  /// <param name="stateId">State Id.</param>
  /// <param name="onSuccess">State Id to transition to on success, or null to denote last state and exit <see cref="StateMachine{TStateId}"/>.</param>
  /// <returns>Instance of this class.</returns>
  /// <typeparam name="TState">State class.</typeparam>
  /// <remarks>Example: <![CDATA[RegisterState<T>(StateId.State1, StateId.State2);]]>.</remarks>
  StateMachine<TStateId> RegisterState<TState>(TStateId stateId, TStateId? onSuccess)
    where TState : class, IState<TStateId>;

  /// <summary>
  ///   Registers a new state with the state machine and configures its transitions and hierarchy.
  /// </summary>
  /// <param name="stateId">The unique identifier for the state to register.</param>
  /// <param name="onSuccess">The identifier of the state to transition to when the registered state completes successfully, or null if no transition is defined.</param>
  /// <param name="onError">The identifier of the state to transition to when the registered state encounters an error, or null if no transition is defined.</param>
  /// <param name="onFailure">The identifier of the state to transition to when the registered state fails, or null if no transition is defined.</param>
  /// <param name="parentStateId">The identifier of the parent state if the registered state is part of a composite state; otherwise, null.</param>
  /// <param name="isCompositeParent">true if the registered state is a composite parent state; otherwise, false.</param>
  /// <param name="initialChildStateId">The identifier of the initial child state to activate when entering a composite parent state; otherwise, null.</param>
  /// <returns>The current <see cref="StateMachine{TStateId}"/> instance, enabling method chaining.</returns>
  /// <typeparam name="TState">The type of the state to register. Must implement <see cref="IState{TStateId}"/>.</typeparam>
  /// <exception cref="InvalidOperationException">Thrown if a state with the specified stateId is already registered or if the state factory returns null.</exception>
  /// <remarks>
  ///   Use this method to add states and define their transitions and hierarchy before starting the
  ///   state machine. Registering duplicate state identifiers is not allowed.
  /// </remarks>
  StateMachine<TStateId> RegisterState<TState>(TStateId stateId, TStateId? onSuccess, TStateId? onError, TStateId? onFailure, TStateId? parentStateId = null, bool isCompositeParent = false, TStateId? initialChildStateId = null)
    where TState : class, IState<TStateId>;

  /// <summary>
  ///   Registers a composite's sub-state (regular/leaf or command state) under a composite parent.
  ///   The nextOnOk is nullable: null means this is the last child, so bubble to the parent's OnExit.
  /// </summary>
  /// <typeparam name="TChildClass">Child State Class (non-composite child state).</typeparam>
  /// <param name="stateId">The unique identifier for the state to register.</param>
  /// <param name="parentStateId">The identifier of the parent composite state.</param>
  /// <param name="onSuccess">The identifier of the state to transition to when the state completes successfully, or null to return to the parent composite state.</param>
  /// <param name="onError">The identifier of the state to transition to when the registered state encounters an error, or null if no transition is defined.</param>
  /// <param name="onFailure">The identifier of the state to transition to when the registered state fails, or null if no transition is defined.</param>
  /// <returns>The current <see cref="StateMachine{TStateId}"/> instance, enabling method chaining.</returns>
  StateMachine<TStateId> RegisterSubState<TChildClass>(TStateId stateId, TStateId parentStateId, TStateId? onSuccess = null, TStateId? onError = null, TStateId? onFailure = null)
    where TChildClass : class, IState<TStateId>;

  /// <summary>Starts the machine at the initial state.</summary>
  /// <param name="initialState">Initial startup state.</param>
  /// <param name="parameterStack">Initial <see cref="PropertyBag"/> parameter stack.</param>
  /// <param name="errorStack">Error Stack <see cref="PropertyBag"/>.</param>
  /// <param name="cancellationToken">Cancellation Token.</param>
  /// <returns>Async task of The current <see cref="StateMachine{TStateId}"/> instance, enabling method chaining.</returns>
  /// <exception cref="InvalidOperationException">Thrown if the specified state identifier has not been registered.</exception>
  Task<StateMachine<TStateId>> RunAsync(TStateId initialState, PropertyBag? parameterStack = null, PropertyBag? errorStack = null, CancellationToken cancellationToken = default);
}
