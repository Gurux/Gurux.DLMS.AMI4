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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get agent log.
    /// </summary>
    public class GetAgentLogResponse
    {
        /// <summary>
        /// Agent log information.
        /// </summary>
        [IncludeSwagger(typeof(GXAgent),
                nameof(GXAgent.Id),
                nameof(GXAgent.Name))]
        public GXAgentLog? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from agent logs.
    /// </summary>
    [DataContract]
    public class ListAgentLogs
    {
        /// <summary>
        /// Filter can be used to filter log example by date.
        /// </summary>
        [IncludeSwagger(typeof(GXAgent),
                nameof(GXAgent.Id),
                nameof(GXAgent.Name))]
        public GXAgentLog? Filter
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
    }

    /// <summary>
    /// Get agent logs response.
    /// </summary>
    [DataContract]
    public class ListAgentLogsResponse
    {
        /// <summary>
        /// List of Agent logs.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXAgent),
                nameof(GXAgent.Id),
                nameof(GXAgent.Name))]
        public GXAgentLog[]? Logs
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
    /// Add new agent log.
    /// </summary>
    [DataContract]
    public class AddAgentLog
    {
        /// <summary>
        /// New agent log.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXAgent),
                nameof(GXAgent.Id))]
        public GXAgentLog[] Logs
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new agent log response.
    /// </summary>
    [DataContract]
    public class AddAgentLogResponse
    {
    }

    /// <summary>
    /// Clear logs. All logs are removed from the given user.
    /// </summary>
    [DataContract]
    public class ClearAgentLogs
    {
        /// <summary>
        /// Agent identifiers where logs are removed.
        /// </summary>
        [DataMember]
        public Guid[]? Agents
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear logs response.
    /// </summary>
    [DataContract]
    public class ClearAgentLogsResponse
    {
    }

    /// <summary>
    /// Close logs.
    /// </summary>
    [DataContract]
    public class CloseAgentLog
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
    public class CloseAgentLogResponse
    {
    }
}
