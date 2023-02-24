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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get workflow group.
    /// </summary>
    public class GetWorkflowGroupResponse
    {
        /// <summary>
        /// Workflow group information.
        /// </summary>
        [IncludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Id),
            nameof(GXWorkflow.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id),
            nameof(GXUserGroup.Name))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXWorkflowGroup Item
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get workflow group list.
    /// </summary>
    [DataContract]
    public class ListWorkflowGroups : IGXRequest<ListWorkflowGroupsResponse>
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
        /// Amount of the workflow groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter workflow groups.
        /// </summary>
        [ExcludeSwagger(typeof(GXWorkflowGroup),
             nameof(GXWorkflowGroup.Workflows),
             nameof(GXWorkflowGroup.UserGroups))]
        public GXWorkflowGroup? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access groups from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all users are retreaved, not just current user. 
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
    /// Get workflow groups response.
    /// </summary>
    [DataContract]
    public class ListWorkflowGroupsResponse
    {
        /// <summary>
        /// List of workflow groups.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXWorkflowGroup),
             nameof(GXWorkflowGroup.Workflows),
             nameof(GXWorkflowGroup.UserGroups))]
        public GXWorkflowGroup[] WorkflowGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the workflow groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new workflow group.
    /// </summary>
    [DataContract]
    public class AddWorkflowGroup : IGXRequest<AddWorkflowGroupResponse>
    {
        /// <summary>
        /// Add new workflow group(s).
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Id))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        public GXWorkflowGroup[] WorkflowGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new workflow group response.
    /// </summary>
    [DataContract]
    public class AddWorkflowGroupResponse
    {
        /// <summary>
        /// New workflow group IDs.
        /// </summary>
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove workflow group.
    /// </summary>
    [DataContract]
    public class RemoveWorkflowGroup : IGXRequest<RemoveWorkflowGroupResponse>
    {
        /// <summary>
        /// Workflow group Ids to remove.
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
    /// Remove workflow group response.
    /// </summary>
    [DataContract]
    public class RemoveWorkflowGroupResponse
    {
    }
}
