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
    /// Get a log entry.
    /// </summary>
    public class GetLogResponse
    {
        /// <summary>
        /// Log information.
        /// </summary>        
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName), nameof(GXUser.Email))]
        [IncludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id), nameof(GXLogType.Name))]
        public GXLog? Item { get; set; }
    }

    /// <summary>
    /// List log entries.
    /// </summary>
    [DataContract]
    public class ListLogs : IGXRequest<ListLogsResponse>
    {
        /// <summary>Whether an authorized administrator requests logs for all users.</summary>
        public bool AllUsers { get; set; }

        /// <summary>Optional additional fields to select.</summary>
        public IEnumerable<string>? Select { get; set; }
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        [DataMember]
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of log entries to return; zero requests all matching entries.
        /// </summary>
        [DataMember]
        public int Count { get; set; }

        /// <summary>
        /// Filter for log entries.
        /// </summary>
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id))]
        public GXLog? Filter { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Public log identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Public log identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// Log listing response.
    /// </summary>
    [DataContract]
    public class ListLogsResponse
    {
        /// <summary>
        /// Log entries.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName), nameof(GXUser.Email))]
        [IncludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id), nameof(GXLogType.Name))]
        public IEnumerable<GXLog>? Logs { get; set; }

        /// <summary>
        /// Total number of matching log entries before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Add new log.
    /// </summary>
    [DataContract]
    public class AddLog : IGXRequest<AddLogResponse>
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
        /// Added log identifiers, when returned by the endpoint.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = [];
    }

    /// <summary>
    /// Clear log entries within the scope supported by the endpoint.
    /// </summary>
    [DataContract]
    public class ClearLog : IGXRequest<ClearLogResponse>
    {
        /// <summary>Source identifiers whose logs are cleared, as defined by the endpoint.</summary>
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }

        /// <summary>Module identifiers whose logs are cleared.</summary>
        [DataMember]
        public IEnumerable<string>? Modules { get; set; }

        /// <summary>User identifiers whose logs are cleared.</summary>
        [DataMember]
        public IEnumerable<string>? Users { get; set; }

        /// <summary>Whether the endpoint permanently deletes entries.</summary>
        [DataMember]
        public bool Delete { get; set; }
        /// <summary>
        /// Cleared log groups.
        /// </summary>
        public IEnumerable<Guid>? Groups { get; set; }
    }

    /// <summary>
    /// Log clearing response.
    /// </summary>
    [DataContract]
    public class ClearLogResponse
    {
    }

    /// <summary>
    /// Close logs.
    /// </summary>
    [DataContract]
    public class CloseLog : IGXRequest<CloseLogResponse>
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
        /// Whether to permanently delete the items from the database.
        /// </summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
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
