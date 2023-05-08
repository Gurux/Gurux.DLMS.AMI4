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
using Gurux.Common;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get manufacturer group.
    /// </summary>
    public class GetManufacturerGroupResponse
    {
        /// <summary>
        /// Manufacturer group information.
        /// </summary>
        [IncludeSwagger(typeof(GXManufacturer), nameof(GXManufacturer.Id),
                nameof(GXManufacturer.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        public GXManufacturerGroup? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get manufacturer group list.
    /// </summary>
    [DataContract]
    public class ListManufacturerGroups : IGXRequest<ListManufacturerGroupsResponse>
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
        /// Amount of the manufacturer groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter manufacturer groups.
        /// </summary>
        [ExcludeSwagger(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Manufacturers),
            nameof(GXManufacturerGroup.UserGroups))]
        public GXManufacturerGroup? Filter
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
        public TargetType Select
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
    }

    /// <summary>
    /// Get manufacturer groups response.
    /// </summary>
    [DataContract]
    public class ListManufacturerGroupsResponse
    {
        /// <summary>
        /// List of manufacturer groups.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Manufacturers),
            nameof(GXManufacturerGroup.UserGroups))]
        public GXManufacturerGroup[]? ManufacturerGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the manufacturer groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new manufacturer group.
    /// </summary>
    [DataContract]
    public class AddManufacturerGroup : IGXRequest<AddManufacturerGroupResponse>
    {
        /// <summary>
        /// New manufacturer group(s).
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXManufacturer), nameof(GXManufacturer.Id))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeSwagger(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.CreationTime), nameof(GXManufacturerGroup.Updated))]
        public GXManufacturerGroup[] ManufacturerGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new manufacturer group response.
    /// </summary>
    [DataContract]
    public class AddManufacturerGroupResponse
    {
        /// <summary>
        /// New manufacturer group IDs.
        /// </summary>
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove manufacturer group.
    /// </summary>
    [DataContract]
    public class RemoveManufacturerGroup : IGXRequest<RemoveManufacturerGroupResponse>
    {
        /// <summary>
        /// Manufacturer group Ids to remove.
        /// </summary>
        [DataMember]
        public Guid[] Ids
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
    /// Remove manufacturer group response.
    /// </summary>
    [DataContract]
    public class RemoveManufacturerGroupResponse
    {
    }
}
