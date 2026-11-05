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
using Gurux.DLMS.AMI.Shared.DTOs;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Requests a list of performance records.</summary>
    [Description("Requests a list of performance records.")]
    [DataContract]
    public class ListPerformances : IGXRequest<ListPerformancesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of performance records to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of performance records to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>Optional criteria used to filter performance records.</summary>
        [Description("Optional criteria used to filter performance records.")]
        [ExcludeOpenApi(typeof(GXPerformance),
            nameof(GXPerformance.ListCount), nameof(GXPerformance.ListTime), nameof(GXPerformance.ReadCount),
            nameof(GXPerformance.ReadTime), nameof(GXPerformance.AddCount), nameof(GXPerformance.AddTime),
            nameof(GXPerformance.AddNotificationTime), nameof(GXPerformance.UpdateCount), nameof(GXPerformance.UpdateTime),
            nameof(GXPerformance.UpdateNotificationTime), nameof(GXPerformance.DeleteCount), nameof(GXPerformance.DeleteTime),
            nameof(GXPerformance.DeleteNotificationTime), nameof(GXPerformance.ClearCount), nameof(GXPerformance.ClearTime),
            nameof(GXPerformance.ClearNotificationTime), nameof(GXPerformance.Start), nameof(GXPerformance.End),
            nameof(GXPerformance.TotalCount), nameof(GXPerformance.TotalTime), nameof(GXPerformance.NotificationTime))]
        public GXPerformance? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>Admin user can access performance records from all users.</summary>
        /// <remarks>
        /// If true, performances from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access performance records from all users.")]
        public bool AllUsers { get; set; }

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
    }

    /// <summary>Response containing the requested performance records.</summary>
    [Description("Response containing the requested performance records.")]
    [DataContract]
    public class ListPerformancesResponse
    {
        /// <summary>Performance records.</summary>
        [Description("Performance records.")]
        public IEnumerable<GXPerformance>? Performances { get; set; }

        /// <summary>
        /// Total number of performance records matching the request.
        /// </summary>
        [Description("Total number of performance records matching the request.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests the addition of performance records.</summary>
    [Description("Requests the addition of performance records.")]
    [DataContract]
    public class AddPerformance : IGXRequest<AddPerformanceResponse>
    {
        /// <summary>Added performance records.</summary>
        [Description("Added performance records.")]
        public List<GXPerformance>? Performances { get; set; }
    }

    /// <summary>Response to adding performance records.</summary>
    [Description("Response to adding performance records.")]
    [DataContract]
    public class AddPerformanceResponse
    {
        /// <summary>Identifiers of the newly added performance records.</summary>
        [Description("Identifiers of the newly added performance records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected performance records.</summary>
    [Description("Requests deletion of the selected performance records.")]
    [DataContract]
    public class RemovePerformance : IGXRequest<RemovePerformanceResponse>
    {
        /// <summary>Identifiers of the performance records to delete.</summary>
        [Description("Identifiers of the performance records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Response to deleting the selected performance records.</summary>
    [Description("Response to deleting the selected performance records.")]
    [DataContract]
    public class RemovePerformanceResponse
    {
    }

    /// <summary>
    /// Clear performance.
    /// </summary>
    [Description("Clear performance.")]
    [DataContract]
    public class ClearPerformance : IGXRequest<ClearPerformanceResponse>
    {
    }

    /// <summary>
    /// Clear performance response.
    /// </summary>
    [Description("Clear performance response.")]
    [DataContract]
    public class ClearPerformanceResponse
    {
    }
}
