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
        public GXDeviceTemplateGroup? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get device template group list.
    /// </summary>
    [DataContract]
    public class ListDeviceTemplateGroups : IGXRequest<ListDeviceTemplateGroupsResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the device template  groups to retrieve.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter device template groups.
        /// </summary>
        [ExcludeOpenApi(typeof(GXDeviceTemplateGroup), 
            nameof(GXDeviceTemplateGroup.UserGroups),
            nameof(GXDeviceTemplateGroup.DeviceTemplates))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXDeviceTemplateGroup? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access groups from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public string[]? Select
        {
            get;
            set;
        }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending
        {
            get;
            set;
        }

        /// <summary>
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public Guid[]? Included
        {
            get;
            set;
        }

        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
        /// </remarks>
        public Guid[]? Exclude
        {
            get;
            set;
        }
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
        public GXDeviceTemplateGroup[]? DeviceTemplateGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the device template groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
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
        public GXDeviceTemplateGroup[]? DeviceTemplateGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new device template group response.
    /// </summary>
    [DataContract]
    public class AddDeviceTemplateGroupResponse
    {
        /// <summary>
        /// New device template groups.
        /// </summary>
        public GXDeviceTemplateGroup[]? DeviceTemplateGroups
        {
            get;
            set;
        }
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
        public Guid[]? Ids
        {
            get;
            set;
        }

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove device template group response.
    /// </summary>
    [DataContract]
    public class RemoveDeviceTemplateGroupResponse
    {
    }
}
