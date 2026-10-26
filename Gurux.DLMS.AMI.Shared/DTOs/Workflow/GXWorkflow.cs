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

using Gurux.DLMS.AMI.Shared.DTOs.Log;
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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Workflow
{
    /// <summary>An executable workflow composed of script methods, modules, and execution conditions.</summary>
    public class GXWorkflow : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXWorkflow()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new workflow is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Workflow name.</param>
        public GXWorkflow(string? name)
        {
            Name = name;
            ScriptMethods = new List<GXScriptMethod>();
            WorkflowGroups = new List<GXWorkflowGroup>();
            Logs = new List<GXLog>();
            Modules = new List<GXModule>();
            TraceLevel = System.Diagnostics.TraceLevel.Error;
        }

        /// <summary>
        /// Workflow identifier.
        /// </summary>
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Workflow name.
        /// </summary>
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Workflow description.
        /// </summary>
        [DataMember]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? Description { get; set; }

        /// <summary>
        /// Is Workflow enabled.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// The creator of the workflow.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator { get; set; }


        /// <summary>
        /// Trigger that invokes the work flow.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXTriggerActivity))]
        public GXTriggerActivity? TriggerActivity { get; set; }

        /// <summary>
        /// Trigger condition script method that workflow uses.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXScriptMethod), OnDelete = ForeignKeyDelete.None)]
        public GXScriptMethod? TriggerMethod { get; set; }

        /// <summary>
        /// User that Workflows this event.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXUser? User { get; set; }

        /// <summary>
        /// User group that Workflows this event.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXUserGroup? UserGroup { get; set; }

        /// <summary>
        /// Device that Workflows this event.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXDevice? Device { get; set; }

        /// <summary>
        /// DeviceGroup that Workflows this event.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXDeviceGroup? DeviceGroup { get; set; }

        /// <summary>
        /// Executed script methods.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScriptMethod), typeof(GXWorkflowScriptMethod))]
        public List<GXScriptMethod>? ScriptMethods { get; set; }

        /// <summary>
        /// Executed modules.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXModule), typeof(GXWorkflowModule))]
        public List<GXModule>? Modules { get; set; }

        /// <summary>Workflow groups containing this workflow.</summary>
        [DataMember]
        [ForeignKey(typeof(GXWorkflowGroup), typeof(GXWorkflowGroupWorkflow))]
        public List<GXWorkflowGroup>? WorkflowGroups { get; set; }

        /// <summary>
        /// Workflow logs.
        /// </summary>
        [DataMember, Ignore(IgnoreType.Db)]
        public List<GXLog>? Logs { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Description("Creation time.")]
        [DefaultValue(null)]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// When the Workflow is updated for the last time.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>
        /// Used trace level.
        /// </summary>
        [DataMember]
        [DefaultValue(System.Diagnostics.TraceLevel.Error)]
        [Description("Used trace level.")]
        [IsRequired]
        public TraceLevel? TraceLevel { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// Remove time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// Last execution time
        /// </summary>
        [DataMember]
        [Description("Last execution time.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public DateTimeOffset? ExecutionTime { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
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
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXWorkflow);
        }
    }
}
