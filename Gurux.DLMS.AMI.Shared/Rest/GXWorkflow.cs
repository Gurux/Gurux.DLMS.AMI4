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
    /// <summary>Response containing the requested workflow information.</summary>
    [Description("Response containing the requested workflow information.")]
    public class GetWorkflowResponse
    {
        /// <summary>
        /// Workflow information.
        /// </summary>
        [Description("Workflow information.")]
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

    /// <summary>Requests a list of workflows.</summary>
    [Description("Requests a list of workflows.")]
    [DataContract]
    public class ListWorkflows : IGXRequest<ListWorkflowsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of workflows to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of workflows to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter workflows.
        /// </summary>
        /// 
        [Description("Optional criteria used to filter workflows.")]
        [ExcludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Logs),
            nameof(GXWorkflow.Modules), nameof(GXWorkflow.Creator),
            nameof(GXWorkflow.TriggerActivity),
            nameof(GXWorkflow.TriggerMethod), nameof(GXWorkflow.User),
            nameof(GXWorkflow.UserGroup), nameof(GXWorkflow.Device),
            nameof(GXWorkflow.DeviceGroup), nameof(GXWorkflow.ScriptMethods),
            nameof(GXWorkflow.WorkflowGroups))]
        [ExcludeOpenApi(typeof(GXWorkflow),
            nameof(GXWorkflow.CreationTime), nameof(GXWorkflow.TraceLevel), nameof(GXWorkflow.ConcurrencyStamp),
            nameof(GXWorkflow.ExecutionTime))]
        public GXWorkflow? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>
        /// Admin user can access workflows from all users.
        /// </summary>
        /// <remarks>
        /// If true, workflows from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access workflows from all users.")]
        public bool AllUsers { get; set; }

        /// <summary>Additional related information to include in the response. Reserved for future use.</summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [Description("Additional related information to include in the response. Reserved for future use.")]
        public IEnumerable<string>? Select { get; set; }

        /// <summary>Name of the property to sort by. If omitted, the endpoint uses its default ordering.</summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        [Description("Name of the property to sort by. If omitted, the endpoint uses its default ordering.")]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        [Description("Whether to sort results in descending order.")]
        public bool Descending { get; set; }

        /// <summary>Identifiers to include in the results. A nonempty collection restricts results to these identifiers.</summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Identifiers to include in the results. A nonempty collection restricts results to these identifiers.")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested workflows.</summary>
    [Description("Response containing the requested workflows.")]
    [DataContract]
    public class ListWorkflowsResponse
    {
        /// <summary>
        /// List of workflow items.
        /// </summary>
        [Description("List of workflow items.")]
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
        [Description("Total number of matching workflowrs before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified workflows.</summary>
    [Description("Requests changes to the specified workflows.")]
    [DataContract]
    public class UpdateWorkflow : IGXRequest<UpdateWorkflowResponse>
    {
        /// <summary>
        /// Workflows to update.
        /// </summary>
        [Description("Workflows to update.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Logs),
            nameof(GXWorkflow.Modules), nameof(GXWorkflow.Creator),
            nameof(GXWorkflow.TriggerActivity),
            nameof(GXWorkflow.TriggerMethod), nameof(GXWorkflow.User),
            nameof(GXWorkflow.UserGroup), nameof(GXWorkflow.Device),
            nameof(GXWorkflow.DeviceGroup))]
        [ExcludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.CreationTime), nameof(GXWorkflow.Updated))]
        public IEnumerable<GXWorkflow> Workflows { get; set; } = default!;
    }

    /// <summary>Response to updating the specified workflows.</summary>
    [Description("Response to updating the specified workflows.")]
    [DataContract]
    public class UpdateWorkflowResponse
    {
        /// <summary>Identifiers of the added or updated workflow records.</summary>
        [DataMember]
        [Description("Identifiers of the added or updated workflow records.")]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected workflows.</summary>
    [Description("Requests deletion of the selected workflows.")]
    [DataContract]
    public class RemoveWorkflow : IGXRequest<RemoveWorkflowResponse>
    {
        /// <summary>Identifiers of the workflow records to delete.</summary>
        [Description("Identifiers of the workflow records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected workflows.</summary>
    [Description("Response to deleting the selected workflows.")]
    [DataContract]
    public class RemoveWorkflowResponse
    {
    }
}
