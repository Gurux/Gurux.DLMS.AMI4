// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Filters shared tab parameters using the target component's ordinary parameters.</summary>
public static class GXTabParameters
{
    private static readonly ConcurrentDictionary<Type, HashSet<string>> ParameterNames = new();

    /// <summary>Copies supported common values and applies explicit per-link overrides.</summary>
    /// <param name="componentType">Component whose declared and inherited parameters are accepted.</param>
    /// <param name="common">Shared values; unknown and cascading parameters are ignored.</param>
    /// <param name="overrides">Explicit values from the per-link callback; these take precedence.</param>
    /// <returns>A new parameter dictionary, or null if there are no values to pass.</returns>
    public static IDictionary<string, object>? Filter(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type componentType,
        IDictionary<string, object>? common,
        IDictionary<string, object>? overrides = null)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (common?.Count > 0)
        {
            if (!ParameterNames.TryGetValue(componentType, out var names))
            {
                var declared = componentType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.SetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0 && Attribute.IsDefined(p, typeof(ParameterAttribute), inherit: true) && !Attribute.IsDefined(p, typeof(CascadingParameterAttribute), inherit: true)).Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                names = ParameterNames.GetOrAdd(componentType, declared);
            }
        
            foreach (var parameter in common)
            {
                if (names.Contains(parameter.Key))
                {
                    result[parameter.Key] = parameter.Value;
                }
            }
        }
        if (overrides != null)
        {
            foreach (var parameter in overrides)
            {
                result[parameter.Key] = parameter.Value;
            }
        }
        return result.Count == 0 ? null : result;
    }
}
