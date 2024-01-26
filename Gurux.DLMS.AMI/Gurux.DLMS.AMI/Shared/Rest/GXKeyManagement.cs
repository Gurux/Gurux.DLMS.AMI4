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
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get key management.
    /// </summary>
    public class GetKeyManagementResponse
    {
        /// <summary>
        /// Key management information.
        /// </summary>
        [IncludeSwagger(typeof(GXDevice),
        nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeSwagger(typeof(GXUser),
        nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeSwagger(typeof(GXKeyManagementLog),
        nameof(GXKeyManagementLog.KeyManagement))]
        [ExcludeSwagger(typeof(GXKeyManagementKey),
        nameof(GXKeyManagementKey.KeyManagement))]
        [IncludeSwagger(typeof(GXKeyManagementGroup),
        nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        public GXKeyManagement? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from key managements.
    /// </summary>
    [DataContract]
    public class ListKeyManagements : IGXRequest<ListKeyManagementsResponse>
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
        /// Amount of the key managements to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter key managements.
        /// </summary>
        [IncludeSwagger(typeof(GXKeyManagement),
        nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name),
            nameof(GXKeyManagement.SystemTitle), nameof(GXKeyManagement.Modified)
            , nameof(GXKeyManagement.CreationTime)
            )]
        [IncludeSwagger(typeof(GXDevice),
        nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeSwagger(typeof(GXUser),
        nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeSwagger(typeof(GXKeyManagementLog),
        nameof(GXKeyManagementLog.KeyManagement))]
        [ExcludeSwagger(typeof(GXKeyManagementKey),
        nameof(GXKeyManagementKey.KeyManagement))]
        [IncludeSwagger(typeof(GXKeyManagementGroup),
        nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        public GXKeyManagement? Filter
        {
            get;
            set;
        }


        /// <summary>
        /// Admin user can access key managements from all users.
        /// </summary>
        /// <remarks>
        /// If true, key managements from all users are retreaved, not just current user. 
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
    /// KeyManagement items reply.
    /// </summary>
    [DataContract]
    public class ListKeyManagementsResponse
    {
        /// <summary>
        /// List of key management items.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXKeyManagement),
        nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name),
            nameof(GXKeyManagement.SystemTitle), nameof(GXKeyManagement.Modified)
            , nameof(GXKeyManagement.CreationTime)
            )]
        /*
        [IncludeSwagger(typeof(GXDevice),
        nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeSwagger(typeof(GXUser),
        nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeSwagger(typeof(GXKeyManagementLog),
        nameof(GXKeyManagementLog.KeyManagement))]
        [ExcludeSwagger(typeof(GXKeyManagementKey),
        nameof(GXKeyManagementKey.KeyManagement))]
        [IncludeSwagger(typeof(GXKeyManagementGroup),
        nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        */
        public GXKeyManagement[]? KeyManagements
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the key managementrs.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update key managements.
    /// </summary>
    [DataContract]
    public class UpdateKeyManagement : IGXRequest<UpdateKeyManagementResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateKeyManagement()
        {
            KeyManagements = new List<GXKeyManagement>();
        }

        /// <summary>
        /// KeyManagements to update.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXDevice),
        nameof(GXDevice.Id))]
        [IncludeSwagger(typeof(GXUser),
        nameof(GXUser.Id))]
        [ExcludeSwagger(typeof(GXKeyManagementLog),
        nameof(GXKeyManagementLog.KeyManagement))]
        [ExcludeSwagger(typeof(GXKeyManagementKey),
        nameof(GXKeyManagementKey.KeyManagement))]
        [IncludeSwagger(typeof(GXKeyManagementGroup),
        nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]

        public List<GXKeyManagement> KeyManagements
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update key managements reply.
    /// </summary>
    [DataContract]
    public class UpdateKeyManagementResponse
    {
        /// <summary>
        /// New key management identifiers.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete key managements.
    /// </summary>
    [DataContract]
    public class RemoveKeyManagement : IGXRequest<RemoveKeyManagementResponse>
    {
        /// <summary>
        /// Removed key management identifiers.
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
    /// Reply from Delete key management.
    /// </summary>
    [DataContract]
    public class RemoveKeyManagementResponse
    {
    }

    /// <summary>
    /// Update new keys for the meter.
    /// </summary>
    [DataContract]
    public class UpdateKeys : IGXRequest<UpdateKeysResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateKeys()
        {
            Keys = new List<GXKeyManagement>();
        }

        /// <summary>
        /// Keys to update.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXKeyManagement),
        nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Keys))]
        [ExcludeSwagger(typeof(GXKeyManagementKey), 
            nameof(GXKeyManagementKey.KeyManagement))]
        /*
        [IncludeSwagger(typeof(GXDevice),
        nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeSwagger(typeof(GXUser),
        nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeSwagger(typeof(GXKeyManagementLog),
        nameof(GXKeyManagementLog.KeyManagement))]
        [IncludeSwagger(typeof(GXKeyManagementGroup),
        nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        */
        public List<GXKeyManagement> Keys
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update keys reply.
    /// </summary>
    [DataContract]
    public class UpdateKeysResponse
    {

    }
}
