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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get user properties.
    /// </summary>
    public class GetUserProperties
    {
        /// <summary>
        /// User properties information.
        /// </summary>
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id)
            , nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        public GXUserProperty? Item { get; set; }
    }

    /// <summary>
    /// Update user properties.
    /// </summary>
    [DataContract]
    public class UpdateUserProperties : IGXRequest<UpdateUserPropertiesResponse>
    {
        /// <summary>
        /// User properties to add.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        public IEnumerable<GXUserProperty> Properties { get; set; } = default!;
    }

    /// <summary>
    /// Update user properties.
    /// </summary>
    [DataContract]
    public class UpdateUserPropertiesResponse
    {
        /// <summary>
        /// User properties identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Get user properties from the DB.
    /// </summary>
    [DataContract]
    public class ListUserProperties
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of user properties to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter user properties.
        /// </summary>
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        public GXUserProperty? Filter { get; set; }

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
    /// Get user properties reply.
    /// </summary>
    [DataContract]
    public class ListUserPropertiesResponse
    {
        /// <summary>
        /// List of user properties items.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXUserProperty> Properties { get; set; } = default!;

        /// <summary>
        /// Total number of matching user properties items before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }
}
