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
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested view group information.</summary>
    [Description("Response containing the requested view group information.")]
    public class GetViewGroupResponse
    {
        /// <summary>
        /// View group information.
        /// </summary>
        [Description("View group information.")]
        [IncludeOpenApi(typeof(GXView), nameof(GXView.Id),
                nameof(GXView.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [ExcludeOpenApi(typeof(GXViewGroup))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXViewGroup? Item { get; set; }
    }

    /// <summary>Requests a list of view groups.</summary>
    [Description("Requests a list of view groups.")]
    [DataContract]
    public class ListViewGroups : IGXRequest<ListViewGroupsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of view groups to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of view groups to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter view groups.
        /// </summary>
        [Description("Optional criteria used to filter view groups.")]
        [ExcludeOpenApi(typeof(GXViewGroup), nameof(GXViewGroup.Views),
            nameof(GXViewGroup.UserGroups))]
        [IncludeOpenApi(typeof(GXViewGroup), nameof(GXViewGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXViewGroup), nameof(GXViewGroup.Description), nameof(GXViewGroup.ConcurrencyStamp))]
        public GXViewGroup? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>Whether administrators can access groups owned by other users.</summary>
        /// <remarks>
        /// If true, groups from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Whether administrators can access groups owned by other users.")]
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

        /// <summary>Identifiers to include in the results. A nonempty collection restricts results to these identifiers.</summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Identifiers to include in the results. A nonempty collection restricts results to these identifiers.")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested view groups.</summary>
    [Description("Response containing the requested view groups.")]
    [DataContract]
    public class ListViewGroupsResponse
    {
        /// <summary>
        /// List of view groups.
        /// </summary>
        [Description("List of view groups.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXViewGroup), nameof(GXViewGroup.Views),
            nameof(GXViewGroup.UserGroups))]
        [ExcludeOpenApi(typeof(GXViewGroup))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXViewGroup>? ViewGroups { get; set; }

        /// <summary>
        /// Total number of matching view groups before paging.
        /// </summary>
        [Description("Total number of matching view groups before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests the addition of view groups.</summary>
    [Description("Requests the addition of view groups.")]
    [DataContract]
    public class AddViewGroup : IGXRequest<AddViewGroupResponse>
    {
        /// <summary>New view groups.</summary>
        [Description("New view groups.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXView), nameof(GXView.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeOpenApi(typeof(GXViewGroup), nameof(GXViewGroup.CreationTime), nameof(GXViewGroup.Updated))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXViewGroup> ViewGroups { get; set; } = default!;
    }

    /// <summary>Response to adding view groups.</summary>
    [Description("Response to adding view groups.")]
    [DataContract]
    public class AddViewGroupResponse
    {
        /// <summary>Identifiers of the newly added view group records.</summary>
        [Description("Identifiers of the newly added view group records.")]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected view groups.</summary>
    [Description("Requests deletion of the selected view groups.")]
    [DataContract]
    public class RemoveViewGroup : IGXRequest<RemoveViewGroupResponse>
    {
        /// <summary>Identifiers of the view group records to delete.</summary>
        [Description("Identifiers of the view group records to delete.")]
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

    /// <summary>Response to deleting the selected view groups.</summary>
    [Description("Response to deleting the selected view groups.")]
    [DataContract]
    public class RemoveViewGroupResponse
    {
    }
}
