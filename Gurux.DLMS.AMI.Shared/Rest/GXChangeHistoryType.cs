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

using Gurux.DLMS.AMI.Shared.DTOs.History;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested history type information.</summary>
    [Description("Response containing the requested history type information.")]
    [DataContract]
    public class GetChangeHistoryTypeResponse
    {
        /// <summary>
        /// The requested history type.
        /// </summary>        
        [Description("The requested history type.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXChangeHistoryType), nameof(GXChangeHistoryType.Id), nameof(GXChangeHistoryType.Name), nameof(GXChangeHistoryType.RecordHistory))]
        public GXChangeHistoryType? Item { get; set; }
    }

    /// <summary>Requests a list of history types.</summary>
    [Description("Requests a list of history types.")]
    [DataContract]
    public class ListChangeHistoryTypes : IGXRequest<ListChangeHistoryTypesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        [DataMember]
        public long Index { get; set; }

        /// <summary>Maximum number of history types to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of history types to retrieve. Use 0 to return all matching items without paging.")]
        [DataMember]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter history types by identifier or name.
        /// </summary>
        [Description("Optional criteria used to filter history types by identifier or name.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXChangeHistoryType), nameof(GXChangeHistoryType.Id), nameof(GXChangeHistoryType.Name), nameof(GXChangeHistoryType.RecordHistory))]
        public GXChangeHistoryType? Filter { get; set; }

        /// <summary>History type property to sort by: Id or Name. If omitted, the endpoint uses its default ordering.</summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        [Description("History type property to sort by: Id or Name. If omitted, the endpoint uses its default ordering.")]
        [DataMember]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        [Description("Whether to sort results in descending order.")]
        [DataMember]
        public bool Descending { get; set; }

        /// <summary>Identifiers to include in the results. A nonempty collection restricts results to these identifiers.</summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Identifiers to include in the results. A nonempty collection restricts results to these identifiers.")]
        [DataMember]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        [DataMember]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested history types.</summary>
    [Description("Response containing the requested history types.")]
    [DataContract]
    public class ListChangeHistoryTypesResponse
    {
        /// <summary>
        /// List of history types.
        /// </summary>
        [Description("List of history types.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXChangeHistoryType), nameof(GXChangeHistoryType.Id), nameof(GXChangeHistoryType.Name), nameof(GXChangeHistoryType.RecordHistory))]
        public IEnumerable<GXChangeHistoryType>? Items { get; set; }

        /// <summary>
        /// Total number of matching history types before paging.
        /// </summary>
        [Description("Total number of matching history types before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests insertion or updating of change history type definitions.</summary>
    [Description("Requests insertion or updating of change history type definitions.")]
    [DataContract]
    public class UpdateChangeHistoryType : IGXRequest<UpdateChangeHistoryTypeResponse>
    {

        /// <summary>
        /// Change history type definitions to insert or update, preserving the GUIDs of existing definitions.
        /// </summary>
        [Description("Change history type definitions to insert or update, preserving the GUIDs of existing definitions.")]
        [DataMember]
        public IEnumerable<GXChangeHistoryType> ChangeHistoryTypes { get; set; } = default!;
    }

    /// <summary>Response containing the identifiers of inserted or updated change history type definitions.</summary>
    [Description("Response containing the identifiers of inserted or updated change history type definitions.")]
    [DataContract]
    public class UpdateChangeHistoryTypeResponse
    {
        /// <summary>Stable GUIDs of the inserted or updated change history type definitions.</summary>
        [Description("Stable GUIDs of the inserted or updated change history type definitions.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests permanent deletion of selected change history type definitions and their associated history items and revision records.</summary>
    [Description("Requests permanent deletion of selected change history type definitions and their associated history items and revision records.")]
    [DataContract]
    public class RemoveChangeHistoryType : IGXRequest<RemoveChangeHistoryTypeResponse>
    {
        /// <summary>Stable GUIDs of the change history type definitions to delete.</summary>
        [Description("Stable GUIDs of the change history type definitions to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Response to deleting selected change history type definitions and their associated history items and revision records.</summary>
    [Description("Response to deleting selected change history type definitions and their associated history items and revision records.")]
    [DataContract]
    public class RemoveChangeHistoryTypeResponse
    {
    }

    /// <summary>Adds missing default history types or resets selected existing default definitions.</summary>
    /// <remarks>Requires administrator access. The defaults are Schedule, Device, User, Gateway, Script and Agent.</remarks>
    [Description("Adds missing default history types or resets selected existing defaults, preserving history and revisions.")]
    [DataContract]
    public class RegenerateChangeHistoryType : IGXRequest<RegenerateChangeHistoryTypeResponse>
    {
        /// <summary>Gets or sets the stable GUIDs of existing default history types to reset.</summary>
        /// <remarks>Null or an empty collection adds only missing defaults and preserves existing settings. A nonempty selection restores built-in names and sets RecordHistory to false without changing identifiers, history items or revision records.</remarks>
        [Description("Existing default type GUIDs to reset. Null or empty adds only missing defaults without changing existing settings.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>Confirms completion of the addition or reset of default history types.</summary>
    [Description("Response to adding missing default history types or resetting selected default definitions.")]
    [DataContract]
    public class RegenerateChangeHistoryTypeResponse
    {
    }
}
