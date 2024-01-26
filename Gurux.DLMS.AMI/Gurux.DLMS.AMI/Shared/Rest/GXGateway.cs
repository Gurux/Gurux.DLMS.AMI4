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
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get gateway.
    /// </summary>
    public class GetGatewayResponse
    {
        /// <summary>
        /// Gateway information.
        /// </summary>
        [ExcludeSwagger(typeof(GXGateway),
                nameof(GXGateway.Logs))]
        [IncludeSwagger(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id)
            , nameof(GXGatewayGroup.Name)
            , nameof(GXGatewayGroup.Description))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        public GXGateway? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update gateway.
    /// </summary>
    [DataContract]
    public class UpdateGateway : IGXRequest<UpdateGatewayResponse>
    {
        /// <summary>
        /// Gateway to add.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXGateway), nameof(GXGateway.Creator),
            nameof(GXGateway.Logs), nameof(GXGateway.Detected)
            , nameof(GXGateway.CreationTime), nameof(GXGateway.Updated))]
        [IncludeSwagger(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        [Required]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXGateway[] Gateways
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update reader gateway.
    /// </summary>
    [DataContract]
    public class UpdateGatewayResponse
    {
        /// <summary>
        /// New gateway identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? GatewayIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from gateways.
    /// </summary>
    [DataContract]
    public class ListGateways : IGXRequest<ListGatewaysResponse>
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
        /// Amount of the gateways to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter gateways.
        /// </summary>
        [ExcludeSwagger(typeof(GXGateway), nameof(GXGateway.Creator),
            nameof(GXGateway.GatewayGroups), nameof(GXGateway.Logs))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        public GXGateway? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access gateways from all users.
        /// </summary>
        /// <remarks>
        /// If true, gateways from all users are retreaved, not just current user. 
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
    /// List gateways response.
    /// </summary>
    [DataContract]
    public class ListGatewaysResponse
    {
        /// <summary>
        /// List of gateways.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXGateway), nameof(GXGateway.Creator),
            nameof(GXGateway.GatewayGroups), nameof(GXGateway.Logs)
            , nameof(GXGateway.DeviceGroups), nameof(GXGateway.Devices))]
        public GXGateway[]? Gateways
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the gateways.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }    

    /// <summary>
    /// Remove gateways.
    /// </summary>
    [DataContract]
    public class RemoveGateway : IGXRequest<RemoveGatewayResponse>
    {
        /// <summary>
        /// Gateway identifiers to remove.
        /// </summary>
        [DataMember]
        [Required]
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
    /// Remove gateway response.
    /// </summary>
    [DataContract]
    public class RemoveGatewayResponse
    {
    }

    /// <summary>
    /// Update gateway state.
    /// </summary>
    [DataContract]
    public class UpdateGatewayStatus
    {
        /// <summary>
        /// Gateway ID.
        /// </summary>
        [DataMember]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gateway Status.
        /// </summary>
        [DataMember]
        public DTOs.Enums.GatewayStatus Status
        {
            get;
            set;
        }
    }
}
