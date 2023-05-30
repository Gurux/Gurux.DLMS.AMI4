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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get key management log.
    /// </summary>
    public class GetKeyManagementLog
    {
        /// <summary>
        /// Key management log
        /// </summary>
        [IncludeSwagger(typeof(GXKeyManagement), nameof(GXKeyManagement.Id)
            , nameof(GXKeyManagement.Name))]
        public GXKeyManagementLog? Item
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Get list from key management logs.
    /// </summary>
    [DataContract]
    public class ListKeyManagementLogs : IGXRequest<ListKeyManagementLogsResponse>
    {
        /// <summary>
        /// Filter can be used to filter log example by date.
        /// </summary>
        [IncludeSwagger(typeof(GXKeyManagement), nameof(GXKeyManagement.Id)
           , nameof(GXKeyManagement.Name))]
        public GXKeyManagementLog? Filter
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
    /// Get key management logs response.
    /// </summary>
    [DataContract]
    public class ListKeyManagementLogsResponse
    {
        /// <summary>
        /// List of KeyManagement logs.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXKeyManagement), nameof(GXKeyManagement.Id)
           , nameof(GXKeyManagement.Name))]
        public GXKeyManagementLog[]? Logs
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
    /// Add new key management log.
    /// </summary>
    [DataContract]
    public class AddKeyManagementLog : IGXRequest<AddKeyManagementLogResponse>
    {
        /// <summary>
        /// New key management log.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXKeyManagement), nameof(GXKeyManagement.Id)
           , nameof(GXKeyManagement.Name))]
        public GXKeyManagementLog[]? Logs
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new key management log response.
    /// </summary>
    [DataContract]
    public class AddKeyManagementLogResponse
    {
    }

    /// <summary>
    /// Clear logs. All logs are removed from the given user.
    /// </summary>
    [DataContract]
    public class ClearKeyManagementLogs : IGXRequest<ClearKeyManagementLogsResponse>
    {
        /// <summary>
        /// KeyManagement identifiers where logs are removed.
        /// </summary>
        [DataMember]
        public Guid[]? KeyManagements
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear logs response.
    /// </summary>
    [DataContract]
    public class ClearKeyManagementLogsResponse
    {
    }

    /// <summary>
    /// Close logs.
    /// </summary>
    [DataContract]
    public class CloseKeyManagementLog : IGXRequest<CloseKeyManagementLogResponse>
    {
        /// <summary>
        /// Closed logs.
        /// </summary>
        [DataMember]
        public Guid[]? Logs
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Close logs response.
    /// </summary>
    [DataContract]
    public class CloseKeyManagementLogResponse
    {
    }
}
