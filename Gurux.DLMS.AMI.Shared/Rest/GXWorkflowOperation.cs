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
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested workflow operation information.</summary>
    [Description("Response containing the requested workflow operation information.")]
    public class GetWorkflowOperationResponse
    {
        /// <summary>
        /// Workflow operation information.
        /// </summary>
        [Description("Workflow operation information.")]
        public GXWorkflowOperation? Item { get; set; }
    }

    /// <summary>Requests a list of workflow operations.</summary>
    [Description("Requests a list of workflow operations.")]
    [DataContract]
    public class ListWorkflowOperations : IGXRequest<ListWorkflowOperationsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of workflow operations to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of workflow operations to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter workflow operations.
        /// </summary>
        [Description("Optional criteria used to filter workflow operations.")]
        [ExcludeOpenApi(typeof(GXWorkflowOperation), nameof(GXWorkflowOperation.Data),
            nameof(GXWorkflowOperation.LeaseId), nameof(GXWorkflowOperation.LeaseExpires),
            nameof(GXWorkflowOperation.StartedPublished), nameof(GXWorkflowOperation.OutcomePublished),
            nameof(GXWorkflowOperation.FinishedPublished))]
        public GXWorkflowOperation? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>
        /// Whether an administrator requests operations from all users.
        /// </summary>
        /// <remarks>
        /// Non-administrators remain restricted to their own operations regardless of this value.
        /// </remarks>
        [Description("Whether an administrator requests operations from all users.")]
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

        /// <summary>
        /// Included operation identifiers (GXWorkflowOperation.Id).
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Included operation identifiers (GXWorkflowOperation.Id).")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Excluded operation identifiers (GXWorkflowOperation.Id).
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Excluded operation identifiers (GXWorkflowOperation.Id).")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested workflow operations.</summary>
    [Description("Response containing the requested workflow operations.")]
    [DataContract]
    public class ListWorkflowOperationsResponse
    {
        /// <summary>
        /// List of workflow operations.
        /// </summary>
        [Description("List of workflow operations.")]
        [DataMember]
        public IEnumerable<GXWorkflowOperation>? WorkflowOperations { get; set; }

        /// <summary>
        /// Total number of matching workflow operations before paging.
        /// </summary>
        [Description("Total number of matching workflow operations before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests the addition of workflow operations.</summary>
    [Description("Requests the addition of workflow operations.")]
    [DataContract]
    public class AddWorkflowOperation : IGXRequest<AddWorkflowOperationResponse>
    {
        /// <summary>New workflow operations.</summary>
        [Description("New workflow operations.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXLog), nameof(GXLog.PublicId))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeOpenApi(typeof(GXWorkflowOperation), nameof(GXWorkflowOperation.Started), nameof(GXWorkflowOperation.Finished))]
        public IEnumerable<GXWorkflowOperation> WorkflowOperations { get; set; } = default!;
    }

    /// <summary>Response to adding workflow operations.</summary>
    [Description("Response to adding workflow operations.")]
    [DataContract]
    public class AddWorkflowOperationResponse
    {
        /// <summary>Identifiers of the newly added workflow operation records.</summary>
        [Description("Identifiers of the newly added workflow operation records.")]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected workflow operations.</summary>
    [Description("Requests deletion of the selected workflow operations.")]
    [DataContract]
    public class RemoveWorkflowOperation : IGXRequest<RemoveWorkflowOperationResponse>
    {
        /// <summary>Identifiers of the workflow operation records to delete.</summary>
        [Description("Identifiers of the workflow operation records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Response to deleting the selected workflow operations.</summary>
    [Description("Response to deleting the selected workflow operations.")]
    [DataContract]
    public class RemoveWorkflowOperationResponse
    {
    }

    /// <summary>
    /// Regenerate workflow operation.
    /// </summary>
    [Description("Regenerate workflow operation.")]
    [DataContract]
    public class RegenerateWorkflowOperation : IGXRequest<RegenerateWorkflowOperationResponse>
    {
        /// <summary>Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.</summary>
        [Description("Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate workflow operations response.
    /// </summary>
    [Description("Regenerate workflow operations response.")]
    [DataContract]
    public class RegenerateWorkflowOperationResponse
    {
    }
}
