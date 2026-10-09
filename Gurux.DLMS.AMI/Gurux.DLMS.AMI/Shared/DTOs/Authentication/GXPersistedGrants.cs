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
using Gurux.Service.Orm.Common.Enums;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// Persisted access tokes are saved for this table.
    /// </summary>
    [DataContract(Name = "GXPersistedGrants"), Serializable]
    public class GXPersistedGrants : IUnique<string>
    {
        /// <summary>
        /// Identifier.
        /// </summary>
        [StringLength(200)]
        [DataMember(Name = "Key")]
        [Filter(FilterType.Exact)]
        public string Id { get; set; } = default!;

        /// <summary>
        /// Gets or sets the entity type.
        /// </summary>
        [DataMember]
        [StringLength(50)]
        public string Type { get; set; } = default!;

        /// <summary>
        /// Gets or sets the unique identifier for the subject.
        /// </summary>
        [StringLength(200)]
        [DataMember]
        public string SubjectId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        [DataMember]
        [StringLength(100)]
        public string SessionId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        [DataMember]
        [StringLength(200)]
        public string ClientId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [DataMember]
        [StringLength(200)]
        public string Description { get; set; } = default!;

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Expiration time.
        /// </summary>
        [DataMember]
        [Index(false)]
        public DateTime Expiration { get; set; }

        /// <summary>
        /// Consumed time.
        /// </summary>
        [DataMember]
        public DateTime ConsumedTime { get; set; } = default!;

        /// <summary>
        /// Data. This is the actual data of the token, which can be a JSON string or any other format depending on the implementation. It contains the information needed to validate and use the token, such as claims, scopes, and other relevant details.
        /// </summary>
        [DataMember]
        public string Data { get; set; } = default!;
    }
}
