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
using System.Runtime.Serialization;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Block;

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
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id),
            nameof(GXWorkflowGroup.Name))]
        [ExcludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Logs),
            nameof(GXWorkflow.Modules), nameof(GXWorkflow.Creator),
            nameof(GXWorkflow.TriggerActivity),
            nameof(GXWorkflow.TriggerMethod), nameof(GXWorkflow.User),
            nameof(GXWorkflow.UserGroup), nameof(GXWorkflow.Device),
            nameof(GXWorkflow.DeviceGroup), nameof(GXWorkflow.ScriptMethods),
            nameof(GXWorkflow.WorkflowGroups))]
        public GXWorkflow? Item { get; set; }
    }

    /// <summary>
    /// Get list from workflows.
    /// </summary>
    [DataContract]
    public class ListWorkflows : IGXRequest<ListWorkflowsResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of workflows to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter workflows.
        /// </summary>
        /// 
        [ExcludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Logs),
            nameof(GXWorkflow.Modules), nameof(GXWorkflow.Creator),
            nameof(GXWorkflow.TriggerActivity),
            nameof(GXWorkflow.TriggerMethod), nameof(GXWorkflow.User),
            nameof(GXWorkflow.UserGroup), nameof(GXWorkflow.Device),
            nameof(GXWorkflow.DeviceGroup), nameof(GXWorkflow.ScriptMethods),
            nameof(GXWorkflow.WorkflowGroups))]
        public GXWorkflow? Filter { get; set; }

        /// <summary>
        /// Admin user can access workflows from all users.
        /// </summary>
        /// <remarks>
        /// If true, workflows from all users are retrieved, not just current user.
        /// </remarks>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
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
        [ExcludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Logs),
            nameof(GXWorkflow.Modules), nameof(GXWorkflow.Creator),
            nameof(GXWorkflow.TriggerActivity),
            nameof(GXWorkflow.TriggerMethod), nameof(GXWorkflow.User),
            nameof(GXWorkflow.UserGroup), nameof(GXWorkflow.Device),
            nameof(GXWorkflow.DeviceGroup), nameof(GXWorkflow.ScriptMethods),
            nameof(GXWorkflow.WorkflowGroups))]
        public IEnumerable<GXWorkflow>? Workflows { get; set; }

        /// <summary>
        /// Total number of matching workflowrs before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
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
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Logs),
            nameof(GXWorkflow.Modules), nameof(GXWorkflow.Creator),
            nameof(GXWorkflow.TriggerActivity),
            nameof(GXWorkflow.TriggerMethod), nameof(GXWorkflow.User),
            nameof(GXWorkflow.UserGroup), nameof(GXWorkflow.Device),
            nameof(GXWorkflow.DeviceGroup))]
        public IEnumerable<GXWorkflow> Workflows { get; set; } = default!;
    }

    /// <summary>
    /// Update workflows reply.
    /// </summary>
    [Description("Update workflows reply.")]
    [DataContract]
    public class UpdateWorkflowResponse
    {
        /// <summary>
        /// Workflow identifiers.
        /// </summary>
        [DataMember]
        [Description("Workflow identifiers.")]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Remove workflows.
    /// </summary>
    [DataContract]
    public class RemoveWorkflow : IGXRequest<RemoveWorkflowResponse>
    {
        /// <summary>
        /// Workflow identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>
        /// Whether to permanently delete the items from the database.
        /// </summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>
    /// Reply from Remove workflow.
    /// </summary>
    [DataContract]
    public class RemoveWorkflowResponse
    {
    }
}
