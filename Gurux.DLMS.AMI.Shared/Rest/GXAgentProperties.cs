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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm.Common;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get agent properties.
    /// </summary>
    public class GetAgentProperties
    {
        /// <summary>
        /// Agent properties information.
        /// </summary>
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id)
            , nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        public GXAgentProperty? Item { get; set; }
    }

    /// <summary>
    /// Update user properties.
    /// </summary>
    [DataContract]
    public class UpdateAgentProperties : IGXRequest<UpdateAgentPropertiesResponse>
    {
        /// <summary>
        /// Agent properties to add.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        public IEnumerable<GXAgentProperty> Properties { get; set; } = default!;
    }

    /// <summary>
    /// Update agent properties.
    /// </summary>
    [DataContract]
    public class UpdateAgentPropertiesResponse
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
    public class ListAgentProperties
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
        /// Optional criteria used to filter agent properties.
        /// </summary>
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        public GXAgentProperty? Filter { get; set; }

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
    /// Get agent properties reply.
    /// </summary>
    [DataContract]
    public class ListAgentPropertiesResponse
    {
        /// <summary>
        /// List of agent properties items.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        public IEnumerable<GXAgentProperty> Properties { get; set; } = default!;

        /// <summary>
        /// Total number of matching user properties items before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }
}
