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
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested log type information.</summary>
    [Description("Response containing the requested log type information.")]
    public class GetLogTypeResponse
    {
        /// <summary>
        /// Log type information.
        /// </summary>
        [Description("Log type information.")]
        [IncludeOpenApi(typeof(GXLog), nameof(GXLog.PublicId),
                nameof(GXLog.Message))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXLogType? Item { get; set; }
    }

    /// <summary>Requests a list of log types.</summary>
    [Description("Requests a list of log types.")]
    [DataContract]
    public class ListLogTypes : IGXRequest<ListLogTypesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of log types to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of log types to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter log types.
        /// </summary>
        [Description("Optional criteria used to filter log types.")]
        [ExcludeOpenApi(typeof(GXLogType), nameof(GXLogType.Logs))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXLogType),
            nameof(GXLogType.Id), nameof(GXLogType.Description), nameof(GXLogType.ConcurrencyStamp),
            nameof(GXLogType.Policies))]
        public GXLogType? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>
        /// Admin user can access types from all users.
        /// </summary>
        /// <remarks>
        /// If true, types from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access types from all users.")]
        public bool AllUsers { get; set; }

        /// <summary>Additional related information to include in the response. Reserved for future use.</summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [Description("Additional related information to include in the response. Reserved for future use.")]
        public IEnumerable<string>? Select { get; set; }

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
        /// Included public GUID identifiers (GXLogType.PublicId).
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Included public GUID identifiers (GXLogType.PublicId).")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Excluded public GUID identifiers (GXLogType.PublicId).
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Excluded public GUID identifiers (GXLogType.PublicId).")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested log types.</summary>
    [Description("Response containing the requested log types.")]
    [DataContract]
    public class ListLogTypesResponse
    {
        /// <summary>
        /// List of log types.
        /// </summary>
        [Description("List of log types.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXLogType), nameof(GXLogType.Logs))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXLogType>? LogTypes { get; set; }

        /// <summary>
        /// Total number of matching log types before paging.
        /// </summary>
        [Description("Total number of matching log types before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests the addition of log types.</summary>
    [Description("Requests the addition of log types.")]
    [DataContract]
    public class AddLogType : IGXRequest<AddLogTypeResponse>
    {
        /// <summary>New log types.</summary>
        [Description("New log types.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXLog), nameof(GXLog.PublicId))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id), nameof(GXLogType.CreationTime), nameof(GXLogType.Updated))]
        public IEnumerable<GXLogType> LogTypes { get; set; } = default!;
    }

    /// <summary>Response to adding log types.</summary>
    [Description("Response to adding log types.")]
    [DataContract]
    public class AddLogTypeResponse
    {
        /// <summary>Identifiers of the newly added log type records.</summary>
        [Description("Identifiers of the newly added log type records.")]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected log types.</summary>
    [Description("Requests deletion of the selected log types.")]
    [DataContract]
    public class RemoveLogType : IGXRequest<RemoveLogTypeResponse>
    {
        /// <summary>Identifiers of the log type records to delete.</summary>
        [Description("Identifiers of the log type records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected log types.</summary>
    [Description("Response to deleting the selected log types.")]
    [DataContract]
    public class RemoveLogTypeResponse
    {
    }

    /// <summary>
    /// Regenerate log type.
    /// </summary>
    [Description("Regenerate log type.")]
    [DataContract]
    public class RegenerateLogType : IGXRequest<RegenerateLogTypeResponse>
    {
        /// <summary>Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.</summary>
        [Description("Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate log types response.
    /// </summary>
    [Description("Regenerate log types response.")]
    [DataContract]
    public class RegenerateLogTypeResponse
    {
    }
}
