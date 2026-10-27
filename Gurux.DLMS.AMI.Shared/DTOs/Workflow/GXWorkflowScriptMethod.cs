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
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Workflow
{
    /// <summary>Associates a workflow with a script method to invoke.</summary>
    [Description("Associates a workflow with a script method to invoke.")]
    [DataContract(Name = "GXWorkflowScriptMethod"), Serializable]
    [IndexCollection(true, nameof(WorkflowId), nameof(MethodId), Clustered = true)]
    public class GXWorkflowScriptMethod : GXTableBase
    {
        /// <summary>The database identifier of the workflow.</summary>
        [Description("The database identifier of the workflow.")]
        [DataMember(Name = "WorkflowID")]
        [ForeignKey(typeof(GXWorkflow), OnDelete = ForeignKeyDelete.None)]
        public Guid WorkflowId { get; set; }

        /// <summary>The database identifier of the script method.</summary>
        [Description("The database identifier of the script method.")]
        [DataMember(Name = "MethodId")]
        [ForeignKey(typeof(GXScriptMethod), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid MethodId { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when script method was removed from workflow.
        /// </summary>
        [Description("Time when script method was removed from workflow.")]
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

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
    }
}
