// Copyright Xeno Innovations, Inc. 2025
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace LiteState;

public class PropertyBag : Dictionary<string, object>
{
}

public interface IPropertyBag : IDictionary<string, object>
{
}
