// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.StateMachine;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

/// <summary>State identifier is already registered exception.</summary>
public class DuplicateStateException(string message) : InvalidOperationException(message);

/// <summary>State transition not allowed by pre-defined rule.</summary>
/// <remarks>Happens when a custom-override provided is not in the allowed list.</remarks>
public class InvalidStateTransitionException(string message) : InvalidOperationException(message);

/// <summary>Top-level state is missing an initial state.</summary>
/// <param name="message">Message.</param>
public class MissingInitialStateException(string message) : InvalidOperationException(message);

/// <summary>Composite state is missing an initial child state.</summary>
/// <param name="message">Message.</param>
public class MissingInitialSubStateException(string message) : InvalidOperationException(message);

//// <summary>Missing state transition exception.</summary>
//// public class MissingStateTransitionException(string message) : InvalidOperationException(message);

/// <summary>The child state was not registered the specified composite parent state.</summary>
/// <param name="message">Message.</param>
public class OrphanSubStateException(string message) : InvalidOperationException(message);

/// <summary>Substate's parent state must be registered as a composite state.</summary>
/// <param name="message">Message.</param>
public class ParentStateMustBeCompositeException(string message) : InvalidOperationException(message);

/// <summary>Thrown if the specified state identifier has not been registered.</summary>
/// <param name="message">Message.</param>
public class UnregisteredStateTransitionException(string message) : InvalidOperationException(message);

/// <summary>Substates do not share the same parent state.</summary>
/// <param name="message">Message.</param>
public class DisjointedNextSubStateException(string message) : InvalidOperationException(message);

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
