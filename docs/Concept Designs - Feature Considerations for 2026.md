# Lite.State - Feature Considerations

**Table of Contents:**

* [Lite.State - Feature Considerations](#litestate---feature-considerations)
  * [How the system treats the last defined state transition](#how-the-system-treats-the-last-defined-state-transition)
  * [Option to generate DotGraph of state transitions](#option-to-generate-dotgraph-of-state-transitions)
  * [Custom Event Aggregator](#custom-event-aggregator)

## How the system treats the last defined state transition

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
