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
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.View;

namespace Gurux.DLMS.AMI.Shared.DTOs.User
{
    /// <summary>A group of user accounts with access to associated resource groups.</summary>
    [Description("A group of user accounts with access to associated resource groups.")]
    [DataContract(Name = "GXUserGroup"), Serializable]
    public class GXUserGroup : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXUserGroup.</summary>
        [Description("Initializes a new instance of GXUserGroup.")]
        public GXUserGroup()
        {
        }

        /// <summary>Initializes a new instance of GXUserGroup.</summary>
        /// <remarks>
        /// This constructor is called when a new user group is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">User group name.</param>
        [Description("Initializes a new instance of GXUserGroup.")]
        public GXUserGroup(string? name)
        {
            Name = name;
            Description = "";
            Users = [];
            DeviceGroups = [];
            ScheduleGroups = [];
            DeviceTemplateGroups = [];
            AgentGroups = [];
            ModuleGroups = [];
            WorkflowGroups = [];
            TriggerGroups = [];
            BlockGroups = [];
            MenuGroups = [];
            ComponentViewGroups = [];
            ScriptGroups = [];
            ManufacturerGroups = [];
            KeyManagementGroups = [];
        }

        /// <summary>User group identifier.</summary>
        [Description("User group identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The creator of the user group.
        /// </summary>
        [Description("The creator of the user group.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Name of the user group.
        /// </summary>
		[Description("Name of the user group.")]
		[DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [StringLength(64)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// User group description.
        /// </summary>
        [Description("User group description.")]
        [DataMember]
        [DefaultValue(null)]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? Description { get; set; }

        /// <summary>Time when the record was created.</summary>
        [DataMember]
        [Description("Time when the record was created.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when user group was removed.
        /// </summary>
        [Description("Time when user group was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the user group was last updated.</summary>
        [Description("Time when the user group was last updated.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>User accounts belonging to this group.</summary>
        [Description("User accounts belonging to this group.")]
        [DataMember]
        [ForeignKey(typeof(GXUser), typeof(GXUserGroupUser))]
        [DefaultValue(null)]
        public List<GXUser>? Users { get; set; }

        /// <summary>
        /// List of device groups that this user group can access.
        /// </summary>
        [Description("List of device groups that this user group can access.")]
        [DataMember, ForeignKey(typeof(GXDeviceGroup), typeof(GXUserGroupDeviceGroup))]
        [DefaultValue(null)]
        public List<GXDeviceGroup>? DeviceGroups { get; set; }

        /// <summary>
        /// List of schedule groups that this user group can access.
        /// </summary>
        [Description("List of schedule groups that this user group can access.")]
        [DataMember, ForeignKey(typeof(GXScheduleGroup), typeof(GXUserGroupScheduleGroup))]
        [DefaultValue(null)]
        public List<GXScheduleGroup>? ScheduleGroups { get; set; }

        /// <summary>Device template groups accessible to this user group.</summary>
        [Description("Device template groups accessible to this user group.")]
        [DataMember, ForeignKey(typeof(GXDeviceTemplateGroup),
            typeof(GXUserGroupDeviceTemplateGroup))]
        [DefaultValue(null)]
        public List<GXDeviceTemplateGroup>? DeviceTemplateGroups { get; set; }

        /// <summary>
        ///List of agent groups that this user group can access.
        /// </summary>
        [Description("List of agent groups that this user group can access.")]
        [DataMember, ForeignKey(typeof(GXAgentGroup), typeof(GXUserGroupAgentGroup))]
        [DefaultValue(null)]
        public List<GXAgentGroup>? AgentGroups { get; set; }

        /// <summary>
        ///List of gateway groups that this user group can access.
        /// </summary>
        [Description("List of gateway groups that this user group can access.")]
        [DataMember, ForeignKey(typeof(GXGatewayGroup), typeof(GXUserGroupGatewayGroup))]
        [DefaultValue(null)]
        public List<GXGatewayGroup>? GatewayGroups { get; set; }

        /// <summary>Module groups accessible to this user group.</summary>
        [Description("Module groups accessible to this user group.")]
        [DataMember, ForeignKey(typeof(GXModuleGroup), typeof(GXUserGroupModuleGroup))]
        [DefaultValue(null)]
        public List<GXModuleGroup>? ModuleGroups { get; set; }

        /// <summary>Workflow groups accessible to this user group.</summary>
        [Description("Workflow groups accessible to this user group.")]
        [DataMember, ForeignKey(typeof(GXWorkflowGroup), typeof(GXUserGroupWorkflowGroup))]
        [DefaultValue(null)]
        public List<GXWorkflowGroup>? WorkflowGroups { get; set; }

        /// <summary>Trigger groups accessible to this user group.</summary>
        [Description("Trigger groups accessible to this user group.")]
        [DataMember, ForeignKey(typeof(GXTriggerGroup), typeof(GXUserGroupTriggerGroup))]
        [DefaultValue(null)]
        public List<GXTriggerGroup>? TriggerGroups { get; set; }

        /// <summary>
        /// List of block groups that this user group can access.
        /// </summary>
        [Description("List of block groups that this user group can access.")]
        [DataMember, ForeignKey(typeof(GXBlockGroup), typeof(GXUserGroupBlockGroup))]
        [DefaultValue(null)]
        public List<GXBlockGroup>? BlockGroups { get; set; }

        /// <summary>Menu groups accessible to this user group.</summary>


        /// <summary>
        /// List of content type groups that this user group can access.
        /// </summary>


        /// <summary>
        /// List of menu groups that this user group can access.
        /// </summary>
        [Description("List of menu groups that this user group can access.")]
        [DataMember, ForeignKey(typeof(GXMenuGroup), typeof(GXUserGroupMenuGroup))]
        [DefaultValue(null)]
        public List<GXMenuGroup>? MenuGroups { get; set; }

        /// <summary>
        /// List of view groups that this user group can access.
        /// </summary>
        [Description("List of view groups that this user group can access.")]
        [DataMember, ForeignKey(typeof(GXViewGroup), typeof(GXUserGroupViewGroup))]
        [DefaultValue(null)]
        public List<GXViewGroup>? ViewGroups { get; set; }

        /// <summary>
        /// List of component view groups that this user group can access.
        /// </summary>
        [Description("List of component view groups that this user group can access.")]
        [DataMember, ForeignKey(typeof(GXComponentViewGroup), typeof(GXUserGroupComponentViewGroup))]
        [DefaultValue(null)]
        public List<GXComponentViewGroup>? ComponentViewGroups { get; set; }

        /// <summary>
        /// List of script groups that this user group can access.
        /// </summary>
        [Description("List of script groups that this user group can access.")]
        [DataMember, ForeignKey(typeof(GXScriptGroup), typeof(GXUserGroupScriptGroup))]
        [DefaultValue(null)]
        public List<GXScriptGroup>? ScriptGroups { get; set; }

        /// <summary>
        /// List of manufacturer groups that this user group can access.
        /// </summary>
        [Description("List of manufacturer groups that this user group can access.")]
        [DataMember, ForeignKey(typeof(GXManufacturerGroup), typeof(GXUserGroupManufacturerGroup))]
        [DefaultValue(null)]
        public List<GXManufacturerGroup>? ManufacturerGroups { get; set; }

        /// <summary>
        /// List of key management groups that this user group can access.
        /// </summary>
        [Description("List of key management groups that this user group can access.")]
        [DataMember, ForeignKey(typeof(GXKeyManagementGroup), typeof(GXUserGroupKeyManagementGroup))]
        [DefaultValue(null)]
        public List<GXKeyManagementGroup>? KeyManagementGroups { get; set; }

        /// <summary>
        /// This is default user group where new users are added automatically when user creates them.
        /// </summary>
        [Description("This is default user group where new users are added automatically when user creates them.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default { get; set; }

        /// <summary>Initializes the creation timestamp before insertion.</summary>
        [Description("Initializes the creation timestamp before insertion.")]
        public override void BeforeAdd()
        {
            if (CreationTime == null)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Updates the last-modified timestamp before persisting changes.
        /// </summary>
        [Description("Updates the last-modified timestamp before persisting changes.")]
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXUserGroup);
        }
    }
}
