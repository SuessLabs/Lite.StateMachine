// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Lite.State;

/// <summary>Simple event aggregator for delivering messages to the current command state.</summary>
public interface IEventAggregator
{
  void Publish(object message);

  IDisposable Subscribe(Func<object, bool> handler);
}
