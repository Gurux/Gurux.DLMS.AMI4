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
    /// <summary>
    /// Get device template group.
    /// </summary>
    public class GetDeviceTemplateGroupResponse
    {
        /// <summary>
        /// Device template group information.
        /// </summary>
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id),
                nameof(GXDeviceTemplate.Name))]
        public GXDeviceTemplateGroup? Item { get; set; }
    }

    /// <summary>
    /// Get device template group list.
    /// </summary>
    [DataContract]
    public class ListDeviceTemplateGroups : IGXRequest<ListDeviceTemplateGroupsResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of device template  groups to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter device template groups.
        /// </summary>
        [ExcludeOpenApi(typeof(GXDeviceTemplateGroup),
            nameof(GXDeviceTemplateGroup.UserGroups),
            nameof(GXDeviceTemplateGroup.DeviceTemplates))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXDeviceTemplateGroup? Filter { get; set; }

        /// <summary>
        /// Admin user can access groups from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all users are retrieved, not just current user.
        /// </remarks>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

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
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// Get device template  groups response.
    /// </summary>
    [DataContract]
    public class ListDeviceTemplateGroupsResponse
    {
        /// <summary>
        /// List of device template groups.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXDeviceTemplateGroup),
            nameof(GXDeviceTemplateGroup.UserGroups),
            nameof(GXDeviceTemplateGroup.DeviceTemplates))]
        public IEnumerable<GXDeviceTemplateGroup>? DeviceTemplateGroups { get; set; }

        /// <summary>
        /// Total number of matching device template groups before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Add new device template group.
    /// </summary>
    [DataContract]
    public class AddDeviceTemplateGroup : IGXRequest<AddDeviceTemplateGroupResponse>
    {
        /// <summary>
        /// New device template group(s).
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXDeviceTemplateGroup> DeviceTemplateGroups { get; set; } = default!;
    }

    /// <summary>
    /// Add new device template group response.
    /// </summary>
    [DataContract]
    public class AddDeviceTemplateGroupResponse
    {
        /// <summary>
        /// New device template group IDs.
        /// </summary>
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Remove device template group.
    /// </summary>
    [DataContract]
    public class RemoveDeviceTemplateGroup : IGXRequest<RemoveDeviceTemplateGroupResponse>
    {
        /// <summary>
        /// Device template group Id(s) to remove.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

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
    /// Remove device template group response.
    /// </summary>
    [DataContract]
    public class RemoveDeviceTemplateGroupResponse
    {
    }
}
