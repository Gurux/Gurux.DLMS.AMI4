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

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Owns form state in the client, shared by editors of the same model instance.</summary>
public static class GXEditContexts
{
    /// <summary>Weakly keyed form contexts shared by editors of the same model instance.</summary>
    private static readonly ConditionalWeakTable<object, EditContext> _contexts = [];

    /// <summary>Returns the model's context without retaining models after their editors are released.</summary>
    /// <param name="model">Model instance whose editor state or command policy is needed.</param>
    /// <returns>The existing context for this model instance, or a newly created context.</returns>
    /// <exception cref="ArgumentNullException">The model is null.</exception>
    public static EditContext For(object model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return _contexts.GetValue(model, static value => new EditContext(value));
    }
}
