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
using System.Text.Json.Nodes;

namespace Gurux.DLMS.AMI.Module;

public sealed partial class AmiModuleBuilder
{
    /// <summary>Opt-in typed settings schema, retained only for this module generation.</summary>
    public AmiModuleSettingsSchema? SettingsSchema { get; private set; }

    /// <summary>Registers typed settings validation and sequential version migrations.</summary>
    /// <typeparam name="T">Settings model supporting DataAnnotations and IValidatableObject.</typeparam>
    /// <param name="version">Current nonnegative version; unversioned JSON is legacy version zero.</param>
    /// <param name="migrations">Pure migration callbacks keyed by their source version, advancing by one.</param>
    /// <param name="validator">Optional additional validation callback.</param>
    public void AddSettings<T>(int version, IReadOnlyDictionary<int, Func<JsonObject, JsonObject>>? migrations = null, Action<T>? validator = null) where T : class, new()
    {
        if (SettingsSchema != null)
        {
            throw new InvalidOperationException("A module can register only one settings schema.");
        }
        SettingsSchema = new AmiModuleSettingsSchema<T>(version, migrations, validator);
    }
}
