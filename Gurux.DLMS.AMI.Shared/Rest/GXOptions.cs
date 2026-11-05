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

using Gurux.Service.Orm.Common;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Requests a list of options.</summary>
    [System.ComponentModel.Description("Requests a list of options.")]
    [DataContract]
    public class ListOptions : IGXRequest<ListConfigurationResponse>
    {
        /// <summary>
        /// Configuration key identifying the settings to read or update.
        /// </summary>
        [System.ComponentModel.Description("Configuration key identifying the settings to read or update.")]
        public string? Configuration { get; set; }
    }

    /// <summary>Response containing the requested options.</summary>
    [System.ComponentModel.Description("Response containing the requested options.")]
    [DataContract]
    public class ListOptionsResponse
    {
        /// <summary>
        /// Configuration settings as JSON.
        /// </summary>
        [System.ComponentModel.Description("Configuration settings as JSON.")]
        [DataMember]
        public string? Settings { get; set; }
    }

    /// <summary>Updates the configuration settings stored under the specified key.</summary>
    [System.ComponentModel.Description("Updates the configuration settings stored under the specified key.")]
    [DataContract]
    public class UpdateOptions : IGXRequest<UpdateOptionsResponse>
    {
        /// <summary>
        /// Configuration key identifying the settings to read or update.
        /// </summary>
        [System.ComponentModel.Description("Configuration key identifying the settings to read or update.")]
        public string? Configuration { get; set; }

        /// <summary>
        /// Settings data type.
        /// </summary>
        [System.ComponentModel.Description("Settings data type.")]
        [DataMember]
        public string? SettingsType { get; set; }

        /// <summary>
        /// Configuration settings as JSON.
        /// </summary>
        [System.ComponentModel.Description("Configuration settings as JSON.")]
        [DataMember]
        public string? Settings { get; set; }
    }

    /// <summary>Response to updating configuration settings.</summary>
    [System.ComponentModel.Description("Response to updating configuration settings.")]
    [DataContract]
    public class UpdateOptionsResponse
    {
    }
}
