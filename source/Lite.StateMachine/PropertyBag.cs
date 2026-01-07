// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Lite.StateMachine;

/// <summary>Context parameter stack properties for passing data between states.</summary>
/// <typeparam name="TKey">TKey is Key.</typeparam>
/// <remarks>
///   1) Use thread-safe ConcurrentDictionary
///   2) In a future release, make the keys only enums for fast execution (2025-12-16 DS).
///   <![CDATA[
///     public class PropertyBag<TKey, TValue> : Dictionary<TKey, TValue> where TKey : notnull
///   ]]>
/// </remarks>
public class PropertyBag : Dictionary<object, object?>
{
  public void SafeAdd(object key, object? value)
  {
    this[key] = value;
  }
}
