# Lite.State - Feature Requests

**Table of Contents:**

* [Lite.State - Feature Requests](#litestate---feature-requests)
  * [Adopted Feature Requests](#adopted-feature-requests)
  * [Defining States](#defining-states)
    * [Basic State](#basic-state)
    * [Composite State](#composite-state)
  * [Last Defined State - Exit machine or stay at last state](#last-defined-state---exit-machine-or-stay-at-last-state)
  * [Option to generate DotGraph of state transitions](#option-to-generate-dotgraph-of-state-transitions)
  * [Custom Event Aggregator](#custom-event-aggregator)

## Adopted Feature Requests

* [x] Generate DOT Graph

## Defining States

**Date:** 2025-12-17

### Basic State

* [ ] Simplify the registration of states to use `RegisterState<T>(...);`

### Composite State

1. Register composite to:
   1. [ ] No longer require double-registration
   2. [ ] Pass in initial sub-state

```cs
// Composite State
machine.RegisterState<State2>(
  stateId: StateId.State2,
  onSuccess: StateId.State3,
  initialState: StateId.State2_Sub1,
  subStates: (sub) =>
  {
    sub.RegisterState<State2_Sub1>(stateId: StateId.State2_Sub1);
  });

```

## Fix DOT Graph 

After switching to lazy-loading, using the fluent pattern does not recognize that AddTransition() is being called. Therefore DOTgraph adds a `doublebox` instead of a `box` to all of the states, thinking it is "the last state".

```cs
// SEE:
_transitions[outcome] = target;
```

## Last Defined State - Exit machine or stay at last state

**Date:** 2025-12-15

1. Sit at the last state and wait until told to go to the next state
  * Awaits, context.NextState(<StateId>)
  * PROs:
    * Waits for the user to inform it.
    * Idle state sits and waits for a triggering `OnMessage` without a defined `timeout`.
  * CONs:
    * Can sit without wraning
2. Auto-exit the StateMachine
  * PROs:
    * We could be done and can auto close the operation or application.
  * CONs:
    * Undesired exit of the operations/application

## Option to generate DotGraph of state transitions

1. PROs:
   * Early discoverery of errors
   * Auto-generated documentation
2. CONs:
   * N/A
3. Limitations:
   * Custom transitions may not be represented

## Custom Event Aggregator

Allow for built-in or 3rd-party event aggregator system.

Requires interfaces and API hooks.
