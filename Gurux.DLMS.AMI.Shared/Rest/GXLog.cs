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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested log information.</summary>
    [Description("Response containing the requested log information.")]
    public class GetLogResponse
    {
        /// <summary>
        /// Log information.
        /// </summary>        
        [Description("Log information.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName), nameof(GXUser.Email))]
        [IncludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id), nameof(GXLogType.Name))]
        public GXLog? Item { get; set; }
    }

    /// <summary>Requests a list of logs.</summary>
    [Description("Requests a list of logs.")]
    [DataContract]
    public class ListLogs : IGXRequest<ListLogsResponse>
    {
        /// <summary>Whether an authorized administrator requests logs for all users.</summary>
        [Description("Whether an authorized administrator requests logs for all users.")]
        public bool AllUsers { get; set; }

        /// <summary>Optional additional fields to select.</summary>
        [Description("Optional additional fields to select.")]
        public IEnumerable<string>? Select { get; set; }
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DataMember]
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>
        /// Maximum number of log entries to return; zero requests all matching entries.
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        [Description("Maximum number of log entries to return; zero requests all matching entries.")]
        public long Count { get; set; }

        /// <summary>
        /// Filter for log entries.
        /// </summary>
        [Description("Filter for log entries.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id))]
        [ExcludeOpenApi(typeof(GXLog),
            nameof(GXLog.Id), nameof(GXLog.StackTrace), nameof(GXLog.Group),
            nameof(GXLog.Closable), nameof(GXLog.Reply))]
        public GXLog? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>Name of the property to sort by. If omitted, the endpoint uses its default ordering.</summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        [Description("Name of the property to sort by. If omitted, the endpoint uses its default ordering.")]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        [Description("Whether to sort results in descending order.")]
        public bool Descending { get; set; }

        /// <summary>
        /// Public log identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Public log identifiers to include in the results.")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Public log identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Public log identifiers to exclude from the results.")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested logs.</summary>
    [Description("Response containing the requested logs.")]
    [DataContract]
    public class ListLogsResponse
    {
        /// <summary>
        /// Log entries.
        /// </summary>
        [Description("Log entries.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName), nameof(GXUser.Email))]
        [IncludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id), nameof(GXLogType.Name))]
        public IEnumerable<GXLog>? Logs { get; set; }

        /// <summary>
        /// Total number of matching log entries before paging.
        /// </summary>
        [Description("Total number of matching log entries before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests the addition of logs.</summary>
    [Description("Requests the addition of logs.")]
    [DataContract]
    public class AddLog : IGXRequest<AddLogResponse>
    {
        /// <summary>
        /// New log.
        /// </summary>
        [Description("New log.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXLog), nameof(GXLog.CreationTime))]
        public IEnumerable<GXLog> Logs { get; set; } = default!;

        /// <summary>
        /// Log type.
        /// </summary>
        [DataMember]
        [Description("Log type.")]
        public string Type { get; set; } = default!;
    }

    /// <summary>Response to adding logs.</summary>
    [Description("Response to adding logs.")]
    [DataContract]
    public class AddLogResponse
    {
        /// <summary>Identifiers of the newly added log records.</summary>
        [Description("Identifiers of the newly added log records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = [];
    }

    /// <summary>
    /// Clear log entries within the scope supported by the endpoint.
    /// </summary>
    [Description("Clear log entries within the scope supported by the endpoint.")]
    [DataContract]
    public class ClearLog : IGXRequest<ClearLogResponse>
    {
        /// <summary>Source identifiers whose logs are cleared, as defined by the endpoint.</summary>
        [Description("Source identifiers whose logs are cleared, as defined by the endpoint.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }

        /// <summary>Module identifiers whose logs are cleared.</summary>
        [Description("Module identifiers whose logs are cleared.")]
        [DataMember]
        public IEnumerable<string>? Modules { get; set; }

        /// <summary>User identifiers whose logs are cleared.</summary>
        [Description("User identifiers whose logs are cleared.")]
        [DataMember]
        public IEnumerable<string>? Users { get; set; }

        /// <summary>Whether the endpoint permanently deletes entries.</summary>
        [Description("Whether the endpoint permanently deletes entries.")]
        [DataMember]
        public bool Delete { get; set; }
        /// <summary>
        /// Cleared log groups.
        /// </summary>
        [Description("Cleared log groups.")]
        public IEnumerable<Guid>? Groups { get; set; }
    }

    /// <summary>
    /// Log clearing response.
    /// </summary>
    [Description("Log clearing response.")]
    [DataContract]
    public class ClearLogResponse
    {
    }

    /// <summary>
    /// Close logs.
    /// </summary>
    [Description("Close logs.")]
    [DataContract]
    public class CloseLog : IGXRequest<CloseLogResponse>
    {
        /// <summary>
        /// Closed logs.
        /// </summary>
        [Description("Closed logs.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Close logs response.
    /// </summary>
    [Description("Close logs response.")]
    [DataContract]
    public class CloseLogResponse
    {
    }

    /// <summary>Requests deletion of the selected logs.</summary>
    [Description("Requests deletion of the selected logs.")]
    [DataContract]
    public class RemoveLog : IGXRequest<RemoveLogResponse>
    {
        /// <summary>Identifiers of the log records to delete.</summary>
        [Description("Identifiers of the log records to delete.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected logs.</summary>
    [Description("Response to deleting the selected logs.")]
    [DataContract]
    public class RemoveLogResponse
    {
    }
}
