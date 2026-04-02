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
    [DataContract]
    public class GXDataExchangeType : IGXRequest<GXDataExchangeTypeResponse>
    {
    }


    /// <summary>
    /// Import data response.
    /// </summary>
    [DataContract]
    public class GXDataExchangeTypeResponse
    {
        /// <summary>
        /// Available data exchange types.
        /// </summary>
        [DataMember]
        public GXTargetType[]? Types
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Import data.
    /// </summary>
    [DataContract]
    public class GXImportData : IGXRequest<GXImportDataResponse>
    {
        /// <summary>
        /// Imported data as Json.
        /// </summary>
        [DataMember]
        public string? Data
        {
            get;
            set;
        }

        /// <summary>
        /// Defines what data is done if it exists.
        /// </summary>
        [DataMember]
        public DataExchangeRule Rule
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Import data response.
    /// </summary>
    [DataContract]
    public class GXImportDataResponse
    {
        /// <summary>
        /// Imported data items and the amount of imported data items.
        /// </summary>
        [DataMember]
        public GXTargetType[]? Types
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Exported data item.
    /// </summary>
    public class GXExportDataItem
    {
        /// <summary>
        /// Exported target type.
        /// </summary>
        [DataMember]
        public string? TargetType
        {
            get;
            set;
        }

        /// <summary>
        /// Target guids.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Export data.
    /// </summary>
    [DataContract]
    public class GXExportData : IGXRequest<GXExportDataResponse>
    {
        /// <summary>
        /// Exported targets.
        /// </summary>
        [DataMember]
        public GXExportDataItem[]? Targets
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Export data response.
    /// </summary>
    [DataContract]
    public class GXExportDataResponse
    {
        /// <summary>
        /// Exported data.
        /// </summary>
        [DataMember]
        public string? Data
        {
            get;
            set;
        }
    }
}
