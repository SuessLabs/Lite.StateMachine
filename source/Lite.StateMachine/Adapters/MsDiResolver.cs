// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

/*
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.StateMachine.Adapters;

public sealed class MsDiResolver : IServiceResolver
{
  private readonly IServiceProvider _provider;

  public MsDiResolver(IServiceProvider provider) =>
    _provider = provider;

  public object? InnerContainer => _provider;

  public T CreateInstance<T>()
    where T : class =>
    ActivatorUtilities.CreateInstance<T>(_provider);

  public object? GetService(Type serviceType) => _provider.GetService(serviceType);
}
*/
