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

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle options.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle options.")]
    public interface IOptionsRepository
    {
        /// <summary>
        /// Get server settings.
        /// </summary>
        /// <typeparam name="T">Type of the settings object.</typeparam>
        /// <param name="configuration">Configuration key.</param>
        /// <returns>Settings object of type T.</returns>
        [System.ComponentModel.Description("Get server settings.")]
        T GetServerSettings<T>(string configuration);

        /// <summary>
        /// Get settings from the database as JSON string.
        /// </summary>
        /// <param name="configuration">Configuration key.</param>
        /// <returns>A task whose result is the stored configuration serialized as JSON.</returns>
        [System.ComponentModel.Description("Get settings from the database as JSON string.")]
        Task<string> GetSettingsAsJsonAsync(string configuration);

        /// <summary>
        /// Save settings to the database.
        /// </summary>
        /// <param name="configuration">Configuration key.</param>
        /// <param name="value">Configuration value to persist.</param>
        /// <returns>A task that completes when the configuration settings have been saved.</returns>
        [System.ComponentModel.Description("Save settings to the database.")]
        Task SaveSettingsAsync(string configuration, object value);
    }
}
