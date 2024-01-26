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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Workflow
{
    /// <summary>
    /// A data contract class representing User Group to User binding object.
    /// </summary>
    [DataContract(Name = "GXWorkflowGroupWorkflow"), Serializable]
    [IndexCollection(true, nameof(WorkflowGroupId), nameof(WorkflowId), Clustered = true)]
    public class GXWorkflowGroupWorkflow : GXTableBase
    {
        /// <summary>
        /// The database ID of the Schedule group.
        /// </summary>
        [DataMember(Name = "WorkflowGroupId")]
        [ForeignKey(typeof(GXWorkflowGroup), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid WorkflowGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// The database ID of the workflow.
        /// </summary>
        [DataMember(Name = "WorkflowID")]
        [ForeignKey(typeof(GXWorkflow), OnDelete = ForeignKeyDelete.Cascade)]
        [StringLength(36)]
        public Guid WorkflowId
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
        /// Time when agent group was removed from user group.
        /// </summary>
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed
        {
            get;
            set;
        }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }
    }
}
