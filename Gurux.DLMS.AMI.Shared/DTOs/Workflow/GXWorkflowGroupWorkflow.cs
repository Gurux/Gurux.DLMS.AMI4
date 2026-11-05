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

using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Workflow
{
    /// <summary>Associates a workflow with a workflow group.</summary>
    [Description("Associates a workflow with a workflow group.")]
    [DataContract(Name = "GXWorkflowGroupWorkflow"), Serializable]
    [IndexCollection(true, nameof(WorkflowGroupId), nameof(WorkflowId), Clustered = true)]
    public class GXWorkflowGroupWorkflow
    {
        /// <summary>The database identifier of the workflow group.</summary>
        [Description("The database identifier of the workflow group.")]
        [DataMember(Name = "WorkflowGroupId")]
        [ForeignKey(typeof(GXWorkflowGroup), OnDelete = ForeignKeyDelete.None)]
        public Guid WorkflowGroupId { get; set; }

        /// <summary>The database identifier of the workflow.</summary>
        [Description("The database identifier of the workflow.")]
        [DataMember(Name = "WorkflowID")]
        [ForeignKey(typeof(GXWorkflow), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid WorkflowId { get; set; }

        /// <summary>Time when the record was created.</summary>
        [DataMember]
        [Description("Time when the record was created.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when agent group was removed from user group.
        /// </summary>
        [Description("Time when agent group was removed from user group.")]
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
