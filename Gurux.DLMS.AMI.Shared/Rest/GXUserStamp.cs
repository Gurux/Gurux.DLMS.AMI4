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
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get list from user stamps.
    /// </summary>
    [DataContract]
    public class ListUserStamps
    {
        /// <summary>
        /// Optional criteria used to filter user stamps, for example by date.
        /// </summary>
        [IncludeOpenApi(typeof(GXUser),
                nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXUserStamp? Filter { get; set; }

        /// <summary>
        /// Admin user can access user stamps from all users.
        /// </summary>
        /// <remarks>
        /// If true, user stamps from all users are retrieved, not just current user.
        /// </remarks>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of user stamps to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// Get user stamps response.
    /// </summary>
    [DataContract]
    public class ListUserStampsResponse
    {
        /// <summary>
        /// User stamp records returned by the request.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXUser),
                nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public IEnumerable<GXUserStamp>? Stamps { get; set; }

        /// <summary>
        /// Total number of matching user stamp records.
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// Update user stamp.
    /// </summary>
    [DataContract]
    public class UpdateUserStamp
    {
        /// <summary>
        /// New user stamp.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXUserStamp),
                nameof(GXUserStamp.Creator))]
        public IEnumerable<GXUserStamp>? Stamps { get; set; }
    }

    /// <summary>
    /// Update user stamp response.
    /// </summary>
    [DataContract]
    public class UpdateUserStampResponse
    {
        /// <summary>
        /// New user stamp.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXUserStamp),
                nameof(GXUserStamp.Creator))]
        public IEnumerable<GXUserStamp>? Stamps { get; set; }
    }

    /// <summary>
    /// Delete user stamps.
    /// </summary>
    [DataContract]
    public class DeleteUserStamps
    {
        /// <summary>
        /// Identifiers of the user stamp records to delete.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Response to deleting user stamp records.
    /// </summary>
    [DataContract]
    public class DeleteUserStampsResponse
    {
    }
}
