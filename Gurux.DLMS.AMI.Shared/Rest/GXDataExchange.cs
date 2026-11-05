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
using Gurux.DLMS.AMI.Shared.DIs.Enums;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Client can ask available data exchange types.
    /// </summary>
    [System.ComponentModel.Description("Client can ask available data exchange types.")]
    [DataContract]
    public class GXDataExchangeType : IGXRequest<GXDataExchangeTypeResponse>
    {
    }


    /// <summary>
    /// Available import and export target types.
    /// </summary>
    [System.ComponentModel.Description("Available import and export target types.")]
    [DataContract]
    public class GXDataExchangeTypeResponse
    {
        /// <summary>
        /// Available data exchange types.
        /// </summary>
        [System.ComponentModel.Description("Available data exchange types.")]
        [DataMember]
        public IEnumerable<GXTargetType>? Types { get; set; }
    }

    /// <summary>
    /// Import data.
    /// </summary>
    [System.ComponentModel.Description("Import data.")]
    [DataContract]
    public class GXImportData : IGXRequest<GXImportDataResponse>
    {
        /// <summary>
        /// JSON data to import.
        /// </summary>
        [System.ComponentModel.Description("JSON data to import.")]
        [DataMember]
        public string? Data { get; set; }

        /// <summary>
        /// Rule to apply when an imported item already exists.
        /// </summary>
        [System.ComponentModel.Description("Rule to apply when an imported item already exists.")]
        [DataMember]
        public DataExchangeRule Rule { get; set; }
    }

    /// <summary>
    /// Import data response.
    /// </summary>
    [System.ComponentModel.Description("Import data response.")]
    [DataContract]
    public class GXImportDataResponse
    {
        /// <summary>
        /// Imported data items and the amount of imported data items.
        /// </summary>
        [System.ComponentModel.Description("Imported data items and the amount of imported data items.")]
        [DataMember]
        public IEnumerable<GXTargetType>? Types { get; set; }
    }

    /// <summary>
    /// Exported data item.
    /// </summary>
    [System.ComponentModel.Description("Exported data item.")]
    public class GXExportDataItem
    {
        /// <summary>
        /// Exported target type.
        /// </summary>
        [System.ComponentModel.Description("Exported target type.")]
        [DataMember]
        public string? TargetType { get; set; }

        /// <summary>
        /// Identifiers of the targets to export.
        /// </summary>
        [System.ComponentModel.Description("Identifiers of the targets to export.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Export data.
    /// </summary>
    [System.ComponentModel.Description("Export data.")]
    [DataContract]
    public class GXExportData : IGXRequest<GXExportDataResponse>
    {
        /// <summary>
        /// Target types and identifiers to export.
        /// </summary>
        [System.ComponentModel.Description("Target types and identifiers to export.")]
        [DataMember]
        public IEnumerable<GXExportDataItem>? Targets { get; set; }
    }

    /// <summary>
    /// Export data response.
    /// </summary>
    [System.ComponentModel.Description("Export data response.")]
    [DataContract]
    public class GXExportDataResponse
    {
        /// <summary>
        /// Exported data.
        /// </summary>
        [System.ComponentModel.Description("Exported data.")]
        [DataMember]
        public string? Data { get; set; }
    }
}
