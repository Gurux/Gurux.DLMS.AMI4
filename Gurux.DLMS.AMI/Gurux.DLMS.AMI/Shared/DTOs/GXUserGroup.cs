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
using Gurux.Common.Db;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    [DataContract(Name = "GXUserGroup"), Serializable]
    public class GXUserGroup : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXUserGroup()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new user group is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">User group name.</param>
        public GXUserGroup(string? name)
        {
            Name = name;
            Description = "";
            Users = new List<GXUser>();
            DeviceGroups = new List<GXDeviceGroup>();
            ScheduleGroups = new List<GXScheduleGroup>();
            DeviceTemplateGroups = new List<GXDeviceTemplateGroup>();
            AgentGroups = new List<GXAgentGroup>();
            ModuleGroups = new List<GXModuleGroup>();
            WorkflowGroups = new List<GXWorkflowGroup>();
            TriggerGroups = new List<GXTriggerGroup>();
            BlockGroups = new List<GXBlockGroup>();
            ComponentViewGroups = new List<GXComponentViewGroup>();
            ScriptGroups = new List<GXScriptGroup>();
            ManufacturerGroups = new List<GXManufacturerGroup>();
            KeyManagementGroups = new List<GXKeyManagementGroup>();
        }

        /// <summary>
        /// User group ID.
        /// </summary>
        [DataMember(Name = "ID"), Index(Unique = true)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the user group.
        /// </summary>
		[DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [StringLength(64)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// User group description.
        /// </summary>
		[DataMember]
        [DefaultValue(null)]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Description("Creation time.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when user group was removed.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed
        {
            get;
            set;
        }

        /// <summary>
        /// When was the user group last updated.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }
        /// <summary>
        /// User has modified the item.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        [JsonIgnore]
        public bool Modified
        {
            get;
            set;
        }

        /// <summary>
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        public string? ConcurrencyStamp
        {
            get;
            set;
        }

        /// <summary>
        /// List of users who belong to this device group.
        /// </summary>       
        [DataMember]
        [ForeignKey(typeof(GXUser), typeof(GXUserGroupUser))]
        [DefaultValue(null)]
        public List<GXUser>? Users
        {
            get;
            set;
        }

        /// <summary>
        /// List of device groups that this user group can access.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXDeviceGroup), typeof(GXUserGroupDeviceGroup))]
        [DefaultValue(null)]
        public List<GXDeviceGroup>? DeviceGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of schedule groups that this user group can access.
        /// </summary>        
        [DataMember, ForeignKey(typeof(GXScheduleGroup), typeof(GXUserGroupScheduleGroup))]
        [DefaultValue(null)]
        public List<GXScheduleGroup>? ScheduleGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of device templates that this user group can access.
        /// </summary>      
        [DataMember, ForeignKey(typeof(GXDeviceTemplateGroup), typeof(GXUserGroupDeviceTemplateGroup))]
        [DefaultValue(null)]
        public List<GXDeviceTemplateGroup>? DeviceTemplateGroups
        {
            get;
            set;
        }

        /// <summary>
        ///List of readers that this user group can access.
        /// </summary>     
        [DataMember, ForeignKey(typeof(GXAgentGroup), typeof(GXUserGroupAgentGroup))]
        [DefaultValue(null)]
        public List<GXAgentGroup>? AgentGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of modules that this user group can access.
        /// </summary>      
        [DataMember, ForeignKey(typeof(GXModuleGroup), typeof(GXUserGroupModuleGroup))]
        [DefaultValue(null)]
        public List<GXModuleGroup>? ModuleGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of workflows that this user group can access.
        /// </summary>       
        [DataMember, ForeignKey(typeof(GXWorkflowGroup), typeof(GXUserGroupWorkflowGroup))]
        [DefaultValue(null)]
        public List<GXWorkflowGroup>? WorkflowGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of workflows that this user group can access.
        /// </summary>       
        [DataMember, ForeignKey(typeof(GXTriggerGroup), typeof(GXUserGroupTriggerGroup))]
        [DefaultValue(null)]
        public List<GXTriggerGroup>? TriggerGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of schedule groups that this user group can access.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXBlockGroup), typeof(GXUserGroupBlockGroup))]
        [DefaultValue(null)]
        public List<GXBlockGroup>? BlockGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of component view groups that this user group can access.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXComponentViewGroup), typeof(GXUserGroupComponentViewGroup))]
        [DefaultValue(null)]
        public List<GXComponentViewGroup>? ComponentViewGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of script groups that this user group can access.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXScriptGroup), typeof(GXUserGroupScriptGroup))]
        [DefaultValue(null)]
        public List<GXScriptGroup>? ScriptGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of manufacturer groups that this user group can access.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXManufacturerGroup), typeof(GXUserGroupManufacturerGroup))]
        [DefaultValue(null)]
        public List<GXManufacturerGroup>? ManufacturerGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of key management groups that this user group can access.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXKeyManagementGroup), typeof(GXUserGroupKeyManagementGroup))]
        [DefaultValue(null)]
        public List<GXKeyManagementGroup>? KeyManagementGroups
        {
            get;
            set;
        }

        /// <summary>
        /// This is default user group where new users are added automatically when user creates them.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default
        {
            get;
            set;
        }

        /// <summary>
        /// Update Creation time.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Update concurrency stamp.
        /// </summary>
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
