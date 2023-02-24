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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get user errors.
    /// </summary>
    public class GetUserError
    {
        /// <summary>
        /// User error.
        /// </summary>
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id)
            , nameof(GXUser.UserName))]
        public GXUserError? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from user errors.
    /// </summary>
    [DataContract]
    public class ListUserErrors : IGXRequest<ListUserErrorsResponse>
    {
        /// <summary>
        /// Filter can be used to filter error example by date.
        /// </summary>
        public GXUserError? Filter
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
    }

    /// <summary>
    /// Get user errors response.
    /// </summary>
    [DataContract]
    public class ListUserErrorsResponse
    {
        /// <summary>
        /// List of User errors.
        /// </summary>
        [DataMember]
        public GXUserError[]? Errors
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
    /// Add new user error.
    /// </summary>
    [DataContract]
    public class AddUserError : IGXRequest<AddUserErrorResponse>
    {
        /// <summary>
        /// New user error.
        /// </summary>
        [DataMember]
        public GXUserError[] Errors
        {
            get;
            set;
        }       
    }

    /// <summary>
    /// Add new user error response.
    /// </summary>
    [DataContract]
    public class AddUserErrorResponse
    {
    }

    /// <summary>
    /// Clear user errors. All errors are cleared from the given user(s).
    /// </summary>
    [DataContract]
    public class ClearUserErrors : IGXRequest<ClearUserErrorsResponse>
    {
        /// <summary>
        /// A collection of users whose errors will be cleared.
        /// </summary>
        [DataMember]
        public string[]? Users
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear errors response.
    /// </summary>
    [DataContract]
    public class ClearUserErrorsResponse
    {
    }

    /// <summary>
    /// Close errors.
    /// </summary>
    [DataContract]
    public class CloseUserError : IGXRequest<CloseUserErrorResponse>
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
    public class CloseUserErrorResponse
    {
    }
}
