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
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Add new system logs.
    /// </summary>
    [DataContract]
    public class AddSystemLog : IGXRequest<AddSystemLogResponse>
    {
        /// <summary>
        /// Log item.
        /// </summary>
        [DataMember]
        [Description("Log item.")]
        [Required]
        public GXSystemLog? Item
        {
            get;
            set;
        }

        /// <summary>
        /// Log type.
        /// </summary>
        [DataMember]
        [Description("Log type.")]
        public string Type
        {
            get;
            set;
        } = default!;
    }

    /// <summary>
    /// Add system log response.
    /// </summary>
    [DataContract]
    [Description("Add system log response.")]
    public class AddSystemLogResponse
    {
    }

    /// <summary>
    /// Get system logs.
    /// </summary>
    [DataContract]
    [Description("Get system logs.")]
    public class ListSystemLogs : IGXRequest<ListSystemLogsResponse>
    {
        /// <summary>
        /// Filter can be used to filter system logs example by date.
        /// </summary>
        public GXSystemLog? Filter
        {
            get;
            set;
        }


        /// <summary>
        /// Admin user can access logs from all users.
        /// </summary>
        /// <remarks>
        /// If true, logs from all users are retreaved, not just current user. 
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
        /// Amount of the logs to retrieve.
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
        public string[]? Select
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
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public Guid[]? Included
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
    /// Get system logs response.
    /// </summary>
    [Description("Get system logs response.")]
    [DataContract]
    public class ListSystemLogsResponse
    {
        /// <summary>
        /// System logs.
        /// </summary>
        [DataMember]
        [Description("System logs.")]
        public GXSystemLog[]? Logs
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of the system logs.
        /// </summary>
        /// <remarks>
        /// With large databases reading the amount of the data can take a very long time.
        /// In those cases the count is set to -1.
        /// </remarks>
        [DataMember]
        [Description("Amount of the system logs.")]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear system logs.
    /// </summary>
    [DataContract]
    public class ClearSystemLog : IGXRequest<ClearSystemLogResponse>
    {
    }

    /// <summary>
    /// Clear system logs response.
    /// </summary>
    [Description("Clear system logs response.")]
    [DataContract]
    public class ClearSystemLogResponse
    {
    }

    /// <summary>
    /// Close system logs.
    /// </summary>
    [DataContract]
    public class CloseSystemLog : IGXRequest<CloseSystemLogResponse>
    {
        /// <summary>
        /// System logs to close.
        /// </summary>
        [DataMember]
        [Description("System logs to close.")]
        public Guid[]? Logs
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Close system logs response.
    /// </summary>
    [Description("Close system logs response.")]
    [DataContract]
    public class CloseSystemLogResponse
    {
    }
}
