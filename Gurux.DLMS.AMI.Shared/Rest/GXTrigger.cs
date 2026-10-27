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
    /// <summary>Response containing the requested trigger information.</summary>
    [Description("Response containing the requested trigger information.")]
    public class GetTriggerResponse
    {
        /// <summary>
        /// Trigger information.
        /// </summary>
        [Description("Trigger information.")]
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

    /// <summary>Requests a list of triggers.</summary>
    [Description("Requests a list of triggers.")]
    [DataContract]
    public class ListTriggers : IGXRequest<ListTriggersResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of triggers to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of triggers to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter triggers.
        /// </summary>
        [Description("Optional criteria used to filter triggers.")]
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
        [ExcludeOpenApi(typeof(GXTrigger),
            nameof(GXTrigger.ClassName), nameof(GXTrigger.Icon), nameof(GXTrigger.ConfigurationUI),
            nameof(GXTrigger.Settings), nameof(GXTrigger.ConcurrencyStamp), nameof(GXTrigger.ExecutionTime))]
        public GXTrigger? Filter { get; set; }
        /// <summary>
        /// Admin user can access triggers from all users.
        /// </summary>
        /// <remarks>
        /// If true, triggers for all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access triggers from all users.")]
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

    /// <summary>Response containing the requested triggers.</summary>
    [DataContract]
    [Description("Response containing the requested triggers.")]
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
        [Description("Total number of matching triggers before paging.")]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified triggers.</summary>
    [Description("Requests changes to the specified triggers.")]
    [DataContract]
    public class UpdateTrigger : IGXRequest<UpdateTriggerResponse>
    {
        /// <summary>Initializes a new instance of UpdateTrigger.</summary>
        [Description("Initializes a new instance of UpdateTrigger.")]
        public UpdateTrigger()
        {
            Triggers = [];
        }

        /// <summary>
        /// Triggers to update.
        /// </summary>
        [Description("Triggers to update.")]
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
        [ExcludeOpenApi(typeof(GXTrigger), nameof(GXTrigger.CreationTime), nameof(GXTrigger.Updated))]
        public List<GXTrigger> Triggers { get; set; }
    }

    /// <summary>Response to updating the specified triggers.</summary>
    [Description("Response to updating the specified triggers.")]
    [DataContract]
    public class UpdateTriggerResponse
    {
        /// <summary>Identifiers of the added or updated trigger records.</summary>
        [DataMember]
        [Description("Identifiers of the added or updated trigger records.")]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected triggers.</summary>
    [Description("Requests deletion of the selected triggers.")]
    [DataContract]
    public class RemoveTrigger : IGXRequest<RemoveTriggerResponse>
    {
        /// <summary>Identifiers of the trigger records to delete.</summary>
        [Description("Identifiers of the trigger records to delete.")]
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

    /// <summary>Response to deleting the selected triggers.</summary>
    [Description("Response to deleting the selected triggers.")]
    [DataContract]
    public class RemoveTriggerResponse
    {
    }

    /// <summary>
    /// Refresh triggers.
    /// </summary>
    [Description("Refresh triggers.")]
    [DataContract]
    public class RefreshTrigger : IGXRequest<RefreshTriggerResponse>
    {
    }

    /// <summary>
    /// Reply from refresh triggers.
    /// </summary>
    [Description("Reply from refresh triggers.")]
    [DataContract]
    public class RefreshTriggerResponse
    {
        /// <summary>
        /// True, if there are new triggers available.
        /// </summary>
        [Description("True, if there are new triggers available.")]
        public bool NewItems { get; set; }
    }
}
