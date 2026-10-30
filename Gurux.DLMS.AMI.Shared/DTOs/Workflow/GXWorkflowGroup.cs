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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Workflow
{
    /// <summary>A named group of workflows with shared access settings.</summary>
    [Description("A named group of workflows with shared access settings.")]
    [DataContract(Name = "GXWorkflowGroup"), Serializable]
    public class GXWorkflowGroup : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXWorkflowGroup.</summary>
        [Description("Initializes a new instance of GXWorkflowGroup.")]
        public GXWorkflowGroup()
        {
        }

        /// <summary>Initializes a new instance of GXWorkflowGroup.</summary>
        /// <remarks>
        /// This constructor is called when a new workflow group is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Workflow group name.</param>
        [Description("Initializes a new instance of GXWorkflowGroup.")]
        public GXWorkflowGroup(string? name)
        {
            Name = name;
            UserGroups = [];
            Workflows = [];
        }

        /// <summary>Workflow group identifier.</summary>
        [Description("Workflow group identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The creator of the workflow group.
        /// </summary>
        [Description("The creator of the workflow group.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Gets or sets who can view the schedule.
        /// </summary>
        /// <remarks>
        /// Modification, deletion and operation permissions are evaluated separately.
        /// </remarks>
        [Description("Gets or sets who can view the schedule.")]
        [DataMember]
        [DefaultValue(ResourceVisibility.Private)]
        [IsRequired]
        public ResourceVisibility? AccessVisibility { get; set; }

        /// <summary>
        /// Name of the schedule group.
        /// </summary>
        [Description("Name of the schedule group.")]
        [DataMember]
        [StringLength(64)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Workflow group description.
        /// </summary>
        [Description("Workflow group description.")]
        [DataMember]
        public string? Description { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when schedule group was removed.
        /// </summary>
        [Description("Time when schedule group was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the workflow group was last updated.</summary>
        [Description("Time when the workflow group was last updated.")]
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

        /// <summary>User groups associated with this workflow group.</summary>
        [Description("User groups associated with this workflow group.")]
        [DataMember, ForeignKey(typeof(GXUserGroup), typeof(GXUserGroupWorkflowGroup))]
        public List<GXUserGroup>? UserGroups { get; set; }

        /// <summary>Workflows belonging to this group.</summary>
        [Description("Workflows belonging to this group.")]
        [DataMember, ForeignKey(typeof(GXWorkflow), typeof(GXWorkflowGroupWorkflow))]
        public List<GXWorkflow>? Workflows { get; set; }

        /// <summary>
        /// This is default workflow group where new workflows are added automatically when user creates them.
        /// </summary>
        [Description("This is default workflow group where new workflows are added automatically when user creates them.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
        [Description("Initializes the creation timestamp before insertion if it has not been set.")]
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
            return nameof(GXWorkflowGroup);
        }
    }
}
