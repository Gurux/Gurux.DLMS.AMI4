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
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get trigger.
    /// </summary>
    public class GetTriggerResponse
    {
        /// <summary>
        /// Trigger information.
        /// </summary>
        [ExcludeSwagger(typeof(GXTrigger), nameof(GXTrigger.Schedules),
            nameof(GXTrigger.Workflows), nameof(GXTrigger.TriggerGroups),
            nameof(GXTrigger.Module))]
        [ExcludeSwagger(typeof(GXTriggerActivity),
             nameof(GXTriggerActivity.Trigger))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        public GXTrigger? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from triggers.
    /// </summary>
    [DataContract]
    public class ListTriggers : IGXRequest<ListTriggersResponse>
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
        /// Amount of the triggers to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter triggers.
        /// </summary>
        [ExcludeSwagger(typeof(GXTrigger),
            nameof(GXTrigger.Workflows),
            nameof(GXTrigger.Activities),
            nameof(GXTrigger.TriggerGroups),
            nameof(GXTrigger.Schedules),
            nameof(GXTrigger.User), 
            nameof(GXTrigger.UserGroup),
            nameof(GXTrigger.Device), 
            nameof(GXTrigger.DeviceGroup),
            nameof(GXTrigger.Module))]
        public GXTrigger? Filter
        {
            get;
            set;
        }
        /// <summary>
        /// Admin user can access triggers from all users.
        /// </summary>
        /// <remarks>
        /// If true, triggers for all users are retreaved, not just current user. 
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
    /// Trigger items reply.
    /// </summary>
    [DataContract]
    [Description("Trigger items reply.")]
    public class ListTriggersResponse
    {
        /// <summary>
        /// List of trigger items.
        /// </summary>
        [Description("List of trigger items.")]
        [DataMember]
        [ExcludeSwagger(typeof(GXTrigger),
            nameof(GXTrigger.Workflows),
            nameof(GXTrigger.Activities),
            nameof(GXTrigger.TriggerGroups),
            nameof(GXTrigger.Schedules),
            nameof(GXTrigger.User),
            nameof(GXTrigger.UserGroup),
            nameof(GXTrigger.Device),
            nameof(GXTrigger.DeviceGroup),
            nameof(GXTrigger.Module))]
        public GXTrigger[]? Triggers
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the triggers.
        /// </summary>
        [DataMember]
        [Description("Total count of the triggers.")]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update triggers.
    /// </summary>
    [DataContract]
    public class UpdateTrigger : IGXRequest<UpdateTriggerResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateTrigger()
        {
            Triggers = new List<GXTrigger>();
        }

        /// <summary>
        /// Triggers to update.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXTrigger),
            nameof(GXTrigger.Workflows),
            nameof(GXTrigger.Activities),
            nameof(GXTrigger.TriggerGroups),
            nameof(GXTrigger.Schedules),
            nameof(GXTrigger.User),
            nameof(GXTrigger.UserGroup),
            nameof(GXTrigger.Device),
            nameof(GXTrigger.DeviceGroup),
            nameof(GXTrigger.Module))]
        public List<GXTrigger> Triggers
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update triggers reply.
    /// </summary>
    [Description("Update triggers reply.")]
    [DataContract]
    public class UpdateTriggerResponse
    {
        /// <summary>
        /// New trigger identifiers.
        /// </summary>
        [DataMember]
        [Description("New trigger identifiers.")]
        public Guid[] TriggerIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove triggers.
    /// </summary>
    [DataContract]
    public class RemoveTrigger : IGXRequest<RemoveTriggerResponse>
    {
        /// <summary>
        /// Removed trigger identifiers.
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
    /// Reply from Remove trigger.
    /// </summary>
    [DataContract]
    public class RemoveTriggerResponse
    {
    }

    /// <summary>
    /// Refresh triggers.
    /// </summary>
    [DataContract]
    public class RefreshTrigger : IGXRequest<RefreshTriggerResponse>
    {
    }

    /// <summary>
    /// Reply from refresh triggers.
    /// </summary>
    [DataContract]
    public class RefreshTriggerResponse
    {
        /// <summary>
        /// True, if there are new triggers available.
        /// </summary>
        public bool NewItems
        {
            get;
            set;
        }
    }
}
