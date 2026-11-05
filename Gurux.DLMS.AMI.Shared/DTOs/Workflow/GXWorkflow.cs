// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Workflow
{
    /// <summary>An executable workflow composed of script methods, modules, and execution conditions.</summary>
    [Description("An executable workflow composed of script methods, modules, and execution conditions.")]
    public partial class GXWorkflow : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXWorkflow.</summary>
        [Description("Initializes a new instance of GXWorkflow.")]
        public GXWorkflow()
        {
        }

        /// <summary>Initializes a new instance of GXWorkflow.</summary>
        /// <remarks>
        /// This constructor is called when a new workflow is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Workflow name.</param>
        [Description("Initializes a new instance of GXWorkflow.")]
        public GXWorkflow(string? name)
        {
            Name = name;
            ScriptMethods = [];
            WorkflowGroups = [];
            Logs = [];
            Modules = [];
            Properties = [];
            TraceLevel = System.Diagnostics.TraceLevel.Error;
        }

        /// <summary>
        /// Workflow identifier.
        /// </summary>
        [Description("Workflow identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>Additional access policies; empty retains existing permissions, otherwise any assigned policy must pass.</summary>
        [Description("Additional access policies; empty retains existing permissions, otherwise any assigned policy must pass.")]
        [DataMember, Ignore(IgnoreType.Db)]
        public List<string>? Policies { get; set; }

        /// <summary>
        /// Workflow name.
        /// </summary>
        [Description("Workflow name.")]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Workflow description.
        /// </summary>
        [Description("Workflow description.")]
        [DataMember]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? Description { get; set; }

        /// <summary>Whether the workflow is enabled.</summary>
        [Description("Whether the workflow is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// The creator of the workflow.
        /// </summary>
        [Description("The creator of the workflow.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Gets or sets who can view the schedule.
        /// </summary>
        /// <remarks>
        /// Modification, deletion and operation permissions are evaluated separately.
        /// </remarks>
        [Description("Gets or sets who can view the schedule.")]
        [DataMember]
        [DefaultValue(ResourceVisibility.Inherit)]
        [IsRequired]
        public ResourceVisibility? AccessVisibility { get; set; }

        /// <summary>
        /// Workflow properties.
        /// </summary>
        [Description("Workflow properties.")]
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXWorkflowProperty>? Properties { get; set; }


        /// <summary>
        /// Trigger that invokes the work flow.
        /// </summary>
        [Description("Trigger that invokes the work flow.")]
        [DataMember]
        [ForeignKey(typeof(GXTriggerActivity))]
        public GXTriggerActivity? TriggerActivity { get; set; }

        /// <summary>The single trigger phase to handle. Existing workflows run After.</summary>
        [DataMember, Description("Selected trigger execution phase.")]
        [DefaultValue(TriggerPhase.After), IsRequired]
        [Filter(FilterType.Exact)]
        public TriggerPhase Phase { get; set; } = TriggerPhase.After;
        /// <summary>
        /// Trigger condition script method that workflow uses.
        /// </summary>
        [Description("Trigger condition script method that workflow uses.")]
        [DefaultValue(null)]
        [ForeignKey(typeof(GXScriptMethod), OnDelete = ForeignKeyDelete.None)]
        public GXScriptMethod? TriggerMethod { get; set; }

        /// <summary>
        /// User that Workflows this event.
        /// </summary>
        [Description("User that Workflows this event.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXUser? User { get; set; }

        /// <summary>
        /// User group that Workflows this event.
        /// </summary>
        [Description("User group that Workflows this event.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXUserGroup? UserGroup { get; set; }

        /// <summary>
        /// Device that Workflows this event.
        /// </summary>
        [Description("Device that Workflows this event.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXDevice? Device { get; set; }

        /// <summary>
        /// DeviceGroup that Workflows this event.
        /// </summary>
        [Description("DeviceGroup that Workflows this event.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        public GXDeviceGroup? DeviceGroup { get; set; }


        /// <summary>Identity used by automatic executions. Manual executions use the caller.</summary>
        [DataMember, Description("Identity used by automatic workflow executions.")]
        [DefaultValue(GXWorkflowExecutionIdentity.ServiceAccount), IsRequired]
        [Filter(FilterType.Exact)]
        public GXWorkflowExecutionIdentity ExecutionIdentity { get; set; } = GXWorkflowExecutionIdentity.ServiceAccount;

        /// <summary>Explicit service account identifier; an absent account never implies the owner or administrator.</summary>
        [DataMember, Description("Explicit service account identifier for automatic workflow executions.")]
        [StringLength(64), DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public string? ExecutionUserId { get; set; }

        /// <summary>
        /// Executed script methods.
        /// </summary>
        [Description("Executed script methods.")]
        [DataMember]
        [ForeignKey(typeof(GXScriptMethod), typeof(GXWorkflowScriptMethod))]
        public List<GXScriptMethod>? ScriptMethods { get; set; }

        /// <summary>
        /// Executed modules.
        /// </summary>
        [Description("Executed modules.")]
        [DataMember]
        [ForeignKey(typeof(GXModule), typeof(GXWorkflowModule))]
        public List<GXModule>? Modules { get; set; }

        /// <summary>Workflow groups containing this workflow.</summary>
        [Description("Workflow groups containing this workflow.")]
        [DataMember]
        [ForeignKey(typeof(GXWorkflowGroup), typeof(GXWorkflowGroupWorkflow))]
        public List<GXWorkflowGroup>? WorkflowGroups { get; set; }

        /// <summary>
        /// Workflow logs.
        /// </summary>
        [Description("Workflow logs.")]
        [DataMember, Ignore(IgnoreType.Db)]
        public List<GXLog>? Logs { get; set; }

        /// <summary>Time when the record was created.</summary>
        [DataMember]
        [Description("Time when the record was created.")]
        [DefaultValue(null)]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the workflow was last updated.</summary>
        [Description("Time when the workflow was last updated.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>Trace level used for communication diagnostics.</summary>
        [DataMember]
        [DefaultValue(System.Diagnostics.TraceLevel.Error)]
        [Description("Trace level used for communication diagnostics.")]
        [IsRequired]
        public TraceLevel? TraceLevel { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time of the most recent execution.</summary>
        [DataMember]
        [Description("Time of the most recent execution.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public DateTimeOffset? ExecutionTime { get; set; }

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
