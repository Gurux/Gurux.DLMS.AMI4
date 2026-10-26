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
using Gurux.Service.Orm.Common;
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
        [ExcludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.Schedules),
            nameof(GXTrigger.Workflows), nameof(GXTrigger.TriggerGroups),
            nameof(GXTrigger.Module))]
        [ExcludeOpenApi(typeof(GXTriggerActivity),
             nameof(GXTriggerActivity.Trigger))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        public GXTrigger? Item { get; set; }
    }

    /// <summary>
    /// Get list from triggers.
    /// </summary>
    [DataContract]
    public class ListTriggers : IGXRequest<ListTriggersResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of triggers to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter triggers.
        /// </summary>
        [ExcludeOpenApi(typeof(GXTrigger),
            nameof(GXTrigger.Workflows),
            nameof(GXTrigger.Activities),
            nameof(GXTrigger.TriggerGroups),
            nameof(GXTrigger.Schedules),
            nameof(GXTrigger.User),
            nameof(GXTrigger.UserGroup),
            nameof(GXTrigger.Device),
            nameof(GXTrigger.DeviceGroup),
            nameof(GXTrigger.Module))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXTrigger? Filter { get; set; }
        /// <summary>
        /// Admin user can access triggers from all users.
        /// </summary>
        /// <remarks>
        /// If true, triggers for all users are retrieved, not just current user.
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
        [ExcludeOpenApi(typeof(GXTrigger),
            nameof(GXTrigger.Workflows),
            nameof(GXTrigger.Activities),
            nameof(GXTrigger.TriggerGroups),
            nameof(GXTrigger.Schedules),
            nameof(GXTrigger.User),
            nameof(GXTrigger.UserGroup),
            nameof(GXTrigger.Device),
            nameof(GXTrigger.DeviceGroup),
            nameof(GXTrigger.Module))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        public IEnumerable<GXTrigger>? Triggers { get; set; }

        /// <summary>
        /// Total number of matching triggers before paging.
        /// </summary>
        [DataMember]
        [Description("Total count of the triggers.")]
        public int Count { get; set; }
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
        [ExcludeOpenApi(typeof(GXTrigger),
            nameof(GXTrigger.Workflows),
            nameof(GXTrigger.Activities),
            nameof(GXTrigger.TriggerGroups),
            nameof(GXTrigger.Schedules),
            nameof(GXTrigger.User),
            nameof(GXTrigger.UserGroup),
            nameof(GXTrigger.Device),
            nameof(GXTrigger.DeviceGroup),
            nameof(GXTrigger.Module))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public List<GXTrigger> Triggers { get; set; }
    }

    /// <summary>
    /// Update triggers reply.
    /// </summary>
    [Description("Update triggers reply.")]
    [DataContract]
    public class UpdateTriggerResponse
    {
        /// <summary>
        /// Trigger identifiers.
        /// </summary>
        [DataMember]
        [Description("Trigger identifiers.")]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Remove triggers.
    /// </summary>
    [DataContract]
    public class RemoveTrigger : IGXRequest<RemoveTriggerResponse>
    {
        /// <summary>
        /// Trigger identifiers.
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
        public bool NewItems { get; set; }
    }
}
