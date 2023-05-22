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

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get workflow log.
    /// </summary>
    public class GetWorkflowLogResponse
    {
        /// <summary>
        /// Workflow log information.
        /// </summary>
        [IncludeSwagger(typeof(GXWorkflow), nameof(GXWorkflowLog.Id))]
        public GXWorkflowLog? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from workflow logs.
    /// </summary>
    [DataContract]
    public class ListWorkflowLogs : IGXRequest<ListWorkflowLogsResponse>
    {
        /// <summary>
        /// Filter can be used to filter log example by date.
        /// </summary>
        [ExcludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Creator),
             nameof(GXWorkflow.TriggerActivity), nameof(GXWorkflow.TriggerMethod),
             nameof(GXWorkflow.User),
             nameof(GXWorkflow.UserGroup),
             nameof(GXWorkflow.Device),
             nameof(GXWorkflow.DeviceGroup),
             nameof(GXWorkflow.ScriptMethods),
             nameof(GXWorkflow.Modules),
             nameof(GXWorkflow.WorkflowGroups),
             nameof(GXWorkflow.Logs))]
        [IncludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Id))]
        public GXWorkflowLog? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access errors from all users.
        /// </summary>
        /// <remarks>
        /// If true, errors from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }


        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of the logs to retreave.
        /// </summary>
        public int Count
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

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending
        {
            get;
            set;
        }

        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
        /// </remarks>
        public Guid[]? Exclude
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get workflow logs response.
    /// </summary>
    [DataContract]
    public class ListWorkflowLogsResponse
    {
        /// <summary>
        /// List of Workflow logs.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Creator),
             nameof(GXWorkflow.TriggerActivity), nameof(GXWorkflow.TriggerMethod),
             nameof(GXWorkflow.User),
             nameof(GXWorkflow.UserGroup),
             nameof(GXWorkflow.Device),
             nameof(GXWorkflow.DeviceGroup),
             nameof(GXWorkflow.ScriptMethods),
             nameof(GXWorkflow.Modules),
             nameof(GXWorkflow.WorkflowGroups),
             nameof(GXWorkflow.Logs))]
        public GXWorkflowLog[]? Logs
        {
            get;
            set;
        }

        /// <summary>
        /// Total amount of the logs.
        /// </summary>
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new workflow log.
    /// </summary>
    [DataContract]
    public class AddWorkflowLog : IGXRequest<AddWorkflowLogResponse>
    {
        /// <summary>
        /// New workflow log(s).
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Id))]
        [ExcludeSwagger(typeof(GXWorkflowLog), nameof(GXWorkflowLog.CreationTime), nameof(GXWorkflowLog.Closed))]
        public GXWorkflowLog[] Logs
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new workflow log response.
    /// </summary>
    [DataContract]
    public class AddWorkflowLogResponse
    {
    }

    /// <summary>
    /// Clear logs. All logs are removed from the given user.
    /// </summary>
    [DataContract]
    public class ClearWorkflowLogs : IGXRequest<ClearWorkflowLogsResponse>
    {
        /// <summary>
        /// Workflow identifiers where logs are removed.
        /// </summary>
        [DataMember]
        public Guid[]? Workflows
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear logs response.
    /// </summary>
    [DataContract]
    public class ClearWorkflowLogsResponse
    {
    }

    /// <summary>
    /// Close logs.
    /// </summary>
    [DataContract]
    public class CloseWorkflowLog : IGXRequest<CloseWorkflowLogResponse>
    {
        /// <summary>
        /// Closed logs.
        /// </summary>
        [DataMember]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Guid[] Logs
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Close logs response.
    /// </summary>
    [DataContract]
    public class CloseWorkflowLogResponse
    {
    }
}
