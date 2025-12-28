// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Lite.StateMachine;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "No need to waste a file.")]
public interface IPropertyBag : IDictionary<string, object>;

/// <summary>Context parameter stack properties for passing data between states.</summary>
/// <remarks>
///   In a future release, make the keys flexible so we can use enums, strings, etc (2025-12-16 DS).
///   <![CDATA[
///     public class PropertyBag<TKey, TValue> : Dictionary<TKey, TValue> where TKey : notnull
///   ]]>
/// </remarks>
public class PropertyBag : Dictionary<string, object>;
