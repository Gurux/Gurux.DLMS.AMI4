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
    /// Get device error.
    /// </summary>
    public class GetDeviceErrorResponse
    {
        /// <summary>
        /// Device error information.
        /// </summary>
        [IncludeSwagger(typeof(GXDevice),
                nameof(GXDevice.Id),
                nameof(GXDevice.Name))]
        public GXDeviceError? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from device errors.
    /// </summary>
    [DataContract]
    public class ListDeviceErrors : IGXRequest<ListDeviceErrorsResponse>
    {
        /// <summary>
        /// Filter can be used to filter error example by date.
        /// </summary>
        public GXDeviceError? Filter
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
        /// Amount of the errors to retreave.
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
    /// Get device errors response.
    /// </summary>
    [DataContract]
    public class ListDeviceErrorsResponse
    {
        /// <summary>
        /// List of Device errors.
        /// </summary>
        [DataMember]
        public GXDeviceError[]? Errors
        {
            get;
            set;
        }

        /// <summary>
        /// Total amount of the errors.
        /// </summary>
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new device error.
    /// </summary>
    [DataContract]
    public class AddDeviceError : IGXRequest<AddDeviceErrorResponse>
    {
        /// <summary>
        /// New device error.
        /// </summary>
        [DataMember]
        public GXDeviceError[] Errors
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new device error response.
    /// </summary>
    [DataContract]
    public class AddDeviceErrorResponse
    {
    }

    /// <summary>
    /// Clear errors. All errors are removed from the given user.
    /// </summary>
    [DataContract]
    public class ClearDeviceErrors : IGXRequest<ClearDeviceErrorsResponse>
    {
        /// <summary>
        /// Device identifiers where errors are removed.
        /// </summary>
        [DataMember]
        public Guid[]? Devices
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear errors response.
    /// </summary>
    [DataContract]
    public class ClearDeviceErrorsResponse
    {
    }

    /// <summary>
    /// Close errors.
    /// </summary>
    [DataContract]
    public class CloseDeviceError : IGXRequest<CloseDeviceErrorResponse>
    {
        /// <summary>
        /// Closed errors.
        /// </summary>
        [DataMember]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Guid[] Errors
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Close errors response.
    /// </summary>
    [DataContract]
    public class CloseDeviceErrorResponse
    {
    }
}
