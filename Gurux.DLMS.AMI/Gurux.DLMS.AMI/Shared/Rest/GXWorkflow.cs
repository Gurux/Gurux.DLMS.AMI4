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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get workflow.
    /// </summary>
    public class GetWorkflowResponse
    {
        /// <summary>
        /// Workflow information.
        /// </summary>
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeSwagger(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeSwagger(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeSwagger(typeof(GXBlockGroup), nameof(GXBlockGroup.Id))]
        [ExcludeSwagger(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language),
            nameof(GXLocalizedResource.Module), nameof(GXLocalizedResource.Block),
            nameof(GXLocalizedResource.Script))]
        [IncludeSwagger(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id),
            nameof(GXWorkflowGroup.Name))]
        [ExcludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Logs),
            nameof(GXWorkflow.Modules), nameof(GXWorkflow.Creator),
            nameof(GXWorkflow.TriggerActivity),
            nameof(GXWorkflow.TriggerMethod), nameof(GXWorkflow.User),
            nameof(GXWorkflow.UserGroup), nameof(GXWorkflow.Device), 
            nameof(GXWorkflow.DeviceGroup), nameof(GXWorkflow.ScriptMethods), 
            nameof(GXWorkflow.WorkflowGroups))]
        public GXWorkflow? Item
        {
            get;
            set;
        }
    }

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
        /// 
        [ExcludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Logs),
            nameof(GXWorkflow.Modules), nameof(GXWorkflow.Creator),
            nameof(GXWorkflow.TriggerActivity),
            nameof(GXWorkflow.TriggerMethod), nameof(GXWorkflow.User),
            nameof(GXWorkflow.UserGroup), nameof(GXWorkflow.Device),
            nameof(GXWorkflow.DeviceGroup), nameof(GXWorkflow.ScriptMethods),
            nameof(GXWorkflow.WorkflowGroups))]
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
        
        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public TargetType Select
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
        [ExcludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Logs),
            nameof(GXWorkflow.Modules), nameof(GXWorkflow.Creator),
            nameof(GXWorkflow.TriggerActivity),
            nameof(GXWorkflow.TriggerMethod), nameof(GXWorkflow.User),
            nameof(GXWorkflow.UserGroup), nameof(GXWorkflow.Device),
            nameof(GXWorkflow.DeviceGroup), nameof(GXWorkflow.ScriptMethods),
            nameof(GXWorkflow.WorkflowGroups))]
        public GXWorkflow[]? Workflows
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
        /// Workflows to update.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Logs),
            nameof(GXWorkflow.Modules), nameof(GXWorkflow.Creator),
            nameof(GXWorkflow.TriggerActivity),
            nameof(GXWorkflow.TriggerMethod), nameof(GXWorkflow.User),
            nameof(GXWorkflow.UserGroup), nameof(GXWorkflow.Device),
            nameof(GXWorkflow.DeviceGroup))]
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
        public Guid[]? WorkflowIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove workflows.
    /// </summary>
    [DataContract]
    public class RemoveWorkflow : IGXRequest<RemoveWorkflowResponse>
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

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from Remove workflow.
    /// </summary>
    [DataContract]
    public class RemoveWorkflowResponse
    {
    }
}
