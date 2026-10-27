//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
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
