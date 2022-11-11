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
using Gurux.Common;
using System.Runtime.Serialization;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get list from workflows.
    /// </summary>
    [DataContract]
    public class ListWorkflows : IGXRequest<ListWorkflowsResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the workflows to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter workflows.
        /// </summary>
        public GXWorkflow? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access workflows from all users.
        /// </summary>
        /// <remarks>
        /// If true, workflows from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }

    }

    /// <summary>
    /// Workflow items reply.
    /// </summary>
    [DataContract]
    public class ListWorkflowsResponse
    {
        /// <summary>
        /// List of workflow items.
        /// </summary>
        [DataMember]
        public GXWorkflow[] Workflows
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the workflowrs.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update workflows.
    /// </summary>
    [DataContract]
    public class UpdateWorkflow : IGXRequest<UpdateWorkflowResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateWorkflow()
        {
        }

        /// <summary>
        /// Workflows to update.
        /// </summary>
        [DataMember]
        [Description("Workflows to update.")]
        public GXWorkflow[] Workflows
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update workflows reply.
    /// </summary>
    [Description("Update workflows reply.")]
    [DataContract]
    public class UpdateWorkflowResponse
    {
        /// <summary>
        /// New workflow identifiers.
        /// </summary>
        [DataMember]
        [Description("New workflow identifiers.")]
        public Guid[] WorkflowIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete workflows.
    /// </summary>
    [DataContract]
    public class DeleteWorkflow : IGXRequest<DeleteWorkflowResponse>
    {
        /// <summary>
        /// Removed workflow identifiers.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from Delete workflow.
    /// </summary>
    [DataContract]
    [Description("Reply from Delete workflow.")]
    public class DeleteWorkflowResponse
    {
    }   
}
