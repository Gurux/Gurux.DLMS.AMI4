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

using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Describes a component that can be displayed inside an AMI UI block.
    /// </summary>
    public interface IGXAmiComponent
    {
        /// <summary>
        /// Gets the display name of the component view.
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// Gets the configuration component type, or <see langword="null"/> when no configuration UI is provided.
        /// </summary>
        [JsonIgnore]
        Type? ConfigurationUI
        {
            get;
        }

        /// <summary>
        /// Gets a CSS icon name or Base64 image, or <see langword="null"/> when no icon is provided.
        /// </summary>
        [JsonIgnore]
        string? Icon
        {
            get;
        }
    }
}
