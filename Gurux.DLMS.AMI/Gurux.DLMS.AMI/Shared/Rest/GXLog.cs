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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get SQL query log.
    /// </summary>
    public class GetLogResponse
    {
        /// <summary>
        /// SQL query log information.
        /// </summary>        
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.Email))]
        [IncludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id), nameof(GXLogType.Name))]
        public GXLog? Item { get; set; }
    }

    /// <summary>
    /// Get list from the SQL query logs.
    /// </summary>
    [DataContract]
    public class ListLogs : IGXRequest<ListLogsResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        [DataMember]
        public int Index { get; set; }

        /// <summary>
        /// Maximum SQL query count to return.
        /// </summary>
        [DataMember]
        public int Count { get; set; }

        /// <summary>
        /// Filter can be used to filter SQL query logs.
        /// </summary>
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id))]
        public GXLog? Filter { get; set; }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// List SQL query log response.
    /// </summary>
    [DataContract]
    public class ListLogsResponse
    {
        /// <summary>
        /// List of SQL query logs.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.Email))]
        [IncludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id), nameof(GXLogType.Name))]
        public IEnumerable<GXLog>? Logs { get; set; }

        /// <summary>
        /// Total count of the SQL query logs.
        /// </summary>
        /// <remarks>
        /// With large databases reading the amount of the data can take a very long time.
        /// In those cases the count is set to -1.
        /// </remarks>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Add new log.
    /// </summary>
    [DataContract]
    public class AddLog
    {
        /// <summary>
        /// New log.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXLog> Logs { get; set; } = default!;

        /// <summary>
        /// Log type.
        /// </summary>
        [DataMember]
        [Description("Log type.")]
        public string Type { get; set; } = default!;
    }

    /// <summary>
    /// Add new log response.
    /// </summary>
    [DataContract]
    public class AddLogResponse
    {
        /// <summary>
        /// Added task identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Clear SQL query log. All logs are removed from the given identifiers.
    /// </summary>
    [DataContract]
    public class ClearLog : IGXRequest<ClearLogResponse>
    {
        /// <summary>
        /// Cleared log groups.
        /// </summary>
        public IEnumerable<Guid>? Groups { get; set; }
    }

    /// <summary>
    /// Clear SQL query log response.
    /// </summary>
    [DataContract]
    public class ClearLogResponse
    {
    }

    /// <summary>
    /// Close logs.
    /// </summary>
    [DataContract]
    public class CloseLog
    {
        /// <summary>
        /// Closed logs.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Close logs response.
    /// </summary>
    [DataContract]
    public class CloseLogResponse
    {
    }

    /// <summary>
    /// Remove logs.
    /// </summary>
    [DataContract]
    public class RemoveLog : IGXRequest<RemoveLogResponse>
    {
        /// <summary>
        /// Log Ids to remove.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>
    /// Remove log response.
    /// </summary>
    [DataContract]
    public class RemoveLogResponse
    {
    }
}
