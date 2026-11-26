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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.Enums;
using Task = System.Threading.Tasks.Task;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Determines whether a catalog attribute can produce a read request during a bulk meter or object read.</summary>
internal static class GXObjectReadPolicy
{
    /// <summary>Excludes static attributes and interprets the owner's legacy modes or version 3 flags.</summary>
    /// <param name="attribute">The attribute whose read-once sentinel and rights are checked.</param>
    /// <param name="owner">The object template identifying the association rights encoding.</param>
    /// <returns>True when the non-static attribute grants read access.</returns>
    internal static bool CanRead(GXAttributeTemplate attribute, GXObjectTemplate? owner)
    {
        if (attribute.ExpirationTime?.Year == DateTimeOffset.MaxValue.Year)
        {
            return false;
        }
        if (owner?.ActionAccessLevels?.StartsWith("0x", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ((AccessMode3)attribute.AccessLevel & AccessMode3.Read) != 0;
        }
        return (AccessMode)attribute.AccessLevel is AccessMode.Read or AccessMode.ReadWrite or
            AccessMode.AuthenticatedRead or AccessMode.AuthenticatedReadWrite;
    }
    /// <summary>Resolves a profile metadata dependency without reading static or inaccessible attributes.</summary>
    /// <param name="model">The object containing metadata templates and cached values.</param>
    /// <param name="index">The metadata attribute's one-based index.</param>
    /// <param name="read">Reads permitted metadata from the meter.</param>
    /// <returns>The fresh metadata, its stored or catalog value, or null when no permitted value is available.</returns>
    internal static async Task<object?> ReadMetadataAsync(GXObject model, int index, Func<int, Task<object>> read)
    {
        var attribute = model.Attributes?.FirstOrDefault(attribute => attribute.Template?.Index == index);
        if (attribute?.Template != null && CanRead(attribute.Template, model.Template))
        {
            return await read(index);
        }
        return string.IsNullOrWhiteSpace(attribute?.Value) ? attribute?.Template?.DefaultValue : attribute.Value;
    }
}