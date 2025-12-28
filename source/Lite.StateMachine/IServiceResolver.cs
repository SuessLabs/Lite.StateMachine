// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;

namespace Lite.StateMachine;

/*
/// <summary>Minimal adapter so the state machine can use any DI container.</summary>
public interface IServiceResolver
{
  /// <summary>Gets underlying container for advanced use-cases (optional).</summary>
  object? InnerContainer => null;

  /// <summary>Create an instance of T using the container's constructor injection.</summary>
  /// <typeparam name="T">Type to create.</typeparam>
  /// <returns>Instance of T.</returns>
  T CreateInstance<T>()
    where T : class;

  /// <summary>Resolve a service instance (optional; used by states via Context.Services if needed).</summary>
  /// <param name="serviceType">Type of service to resolve.</param>
  /// <returns>Service instance or null if not registered.</returns>
  object? GetService(Type serviceType);
}
*/
