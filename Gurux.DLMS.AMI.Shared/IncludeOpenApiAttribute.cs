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

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Limits a nested type in the OpenAPI schema for this property to the named properties.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    /// <param name="type">Nested type whose schema is restricted.</param>
    /// <param name="include">Names of properties to include.</param>
    [System.ComponentModel.Description("Limits a nested type in the OpenAPI schema for this property to the named properties.")]
    [AttributeUsage(AttributeTargets.Property,
      AllowMultiple = true)]
    public sealed class IncludeOpenApiAttribute(Type type, params string[] include) : Attribute
    {
        /// <summary>
        /// Nested type whose schema is restricted.
        /// </summary>
        [System.ComponentModel.Description("Nested type whose schema is restricted.")]
        public Type Type { get; private set; } = type;

        /// <summary>
        /// Property names to include in the nested type schema.
        /// </summary>
        [System.ComponentModel.Description("Property names to include in the nested type schema.")]
        public string[] Include { get; private set; } = include;
    }
}
