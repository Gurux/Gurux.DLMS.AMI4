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
// This file is a part of Gurux DeviceTemplate Framework.
//
// Gurux DeviceTemplate Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux DeviceTemplate Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested device template group information.</summary>
    [Description("Response containing the requested device template group information.")]
    public class GetDeviceTemplateGroupResponse
    {
        /// <summary>
        /// Device template group information.
        /// </summary>
        [Description("Device template group information.")]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id),
                nameof(GXDeviceTemplate.Name))]
        public GXDeviceTemplateGroup? Item { get; set; }
    }

    /// <summary>Requests a list of device template groups.</summary>
    [Description("Requests a list of device template groups.")]
    [DataContract]
    public class ListDeviceTemplateGroups : IGXRequest<ListDeviceTemplateGroupsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of device template groups to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of device template groups to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter device template groups.
        /// </summary>
        [Description("Optional criteria used to filter device template groups.")]
        [ExcludeOpenApi(typeof(GXDeviceTemplateGroup),
            nameof(GXDeviceTemplateGroup.UserGroups),
            nameof(GXDeviceTemplateGroup.DeviceTemplates))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Description), nameof(GXDeviceTemplateGroup.ConcurrencyStamp))]
        public GXDeviceTemplateGroup? Filter { get; set; }

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

    /// <summary>Response containing the requested device template groups.</summary>
    [Description("Response containing the requested device template groups.")]
    [DataContract]
    public class ListDeviceTemplateGroupsResponse
    {
        /// <summary>
        /// List of device template groups.
        /// </summary>
        [Description("List of device template groups.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXDeviceTemplateGroup),
            nameof(GXDeviceTemplateGroup.UserGroups),
            nameof(GXDeviceTemplateGroup.DeviceTemplates))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXDeviceTemplateGroup>? DeviceTemplateGroups { get; set; }

        /// <summary>
        /// Total number of matching device template groups before paging.
        /// </summary>
        [Description("Total number of matching device template groups before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests the addition of device template groups.</summary>
    [Description("Requests the addition of device template groups.")]
    [DataContract]
    public class AddDeviceTemplateGroup : IGXRequest<AddDeviceTemplateGroupResponse>
    {
        /// <summary>New device template groups.</summary>
        [Description("New device template groups.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.CreationTime), nameof(GXDeviceTemplateGroup.Updated))]
        public IEnumerable<GXDeviceTemplateGroup> DeviceTemplateGroups { get; set; } = default!;
    }

    /// <summary>Response to adding device template groups.</summary>
    [Description("Response to adding device template groups.")]
    [DataContract]
    public class AddDeviceTemplateGroupResponse
    {
        /// <summary>Identifiers of the newly added device template group records.</summary>
        [Description("Identifiers of the newly added device template group records.")]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected device template groups.</summary>
    [Description("Requests deletion of the selected device template groups.")]
    [DataContract]
    public class RemoveDeviceTemplateGroup : IGXRequest<RemoveDeviceTemplateGroupResponse>
    {
        /// <summary>Identifiers of the device template group records to delete.</summary>
        [Description("Identifiers of the device template group records to delete.")]
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

    /// <summary>Response to deleting the selected device template groups.</summary>
    [Description("Response to deleting the selected device template groups.")]
    [DataContract]
    public class RemoveDeviceTemplateGroupResponse
    {
    }
}
