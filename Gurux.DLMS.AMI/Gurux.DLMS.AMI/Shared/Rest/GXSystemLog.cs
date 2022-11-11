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
    /// Add new system error.
    /// </summary>
    [DataContract]
    public class AddSystemLog : IGXRequest<AddSystemLogResponse>
    {
        /// <summary>
        /// Occurred error.
        /// </summary>
        [DataMember]
        [Description("Occurred error.")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXSystemLog Error
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add system error response.
    /// </summary>
    [DataContract]
    [Description("Add system error response.")]
    public class AddSystemLogResponse
    {
    }

    /// <summary>
    /// Get system errors.
    /// </summary>
    [DataContract]
    [Description("Get system errors.")]
    public class ListSystemLogs : IGXRequest<ListSystemLogsResponse>
    {
        /// <summary>
        /// Filter can be used to filter system error example by date.
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
        /// Amount of the errors to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get system errors response.
    /// </summary>
    [Description("Get system errors response.")]
    [DataContract]
    public class ListSystemLogsResponse
    {
        /// <summary>
        /// System errors.
        /// </summary>
        [DataMember]
        [Description("System errors.")]
        public GXSystemLog[] Errors
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of the system errors.
        /// </summary>
        [DataMember]
        [Description("Amount of the system errors.")]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear system errors.
    /// </summary>
    [DataContract]
    public class ClearSystemLog : IGXRequest<ClearSystemLogResponse>
    {
    }

    /// <summary>
    /// Clear system errors response.
    /// </summary>
    [Description("Clear system errors response.")]
    [DataContract]
    public class ClearSystemLogResponse
    {
    }

    /// <summary>
    /// Close system errors.
    /// </summary>
    [DataContract]
    public class CloseSystemLog : IGXRequest<CloseSystemLogResponse>
    {
        /// <summary>
        /// System errors to close.
        /// </summary>
        [DataMember]
        [Description("System errors to close.")]
        public Guid[] Errors
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Close system errors response.
    /// </summary>
    [Description("Close system errors response.")]
    [DataContract]
    public class CloseSystemLogResponse
    {
    }
}
