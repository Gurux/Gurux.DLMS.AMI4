// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>A persisted authorization grant or token issued to a client.</summary>
    [System.ComponentModel.Description("A persisted authorization grant or token issued to a client.")]
    [DataContract(Name = "GXPersistedGrants"), Serializable]
    public class GXPersistedGrants : IUnique<string>
    {
        /// <summary>Unique identifier of the persisted grants.</summary>
        [System.ComponentModel.Description("Unique identifier of the persisted grants.")]
        [StringLength(200)]
        [DataMember(Name = "Key")]
        [Filter(FilterType.Exact)]
        public string Id { get; set; } = "";

        /// <summary>
        /// Gets or sets the entity type.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the entity type.")]
        [DataMember]
        [StringLength(50)]
        public string Type { get; set; } = default!;

        /// <summary>
        /// Gets or sets the unique identifier for the subject.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the unique identifier for the subject.")]
        [StringLength(200)]
        [DataMember]
        public string SubjectId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the session identifier.")]
        [DataMember]
        [StringLength(100)]
        public string SessionId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the client identifier.")]
        [DataMember]
        [StringLength(200)]
        public string ClientId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the description.")]
        [DataMember]
        [StringLength(200)]
        public string Description { get; set; } = default!;

        /// <summary>Time when the record was created.</summary>
        [System.ComponentModel.Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Expiration time.
        /// </summary>
        [System.ComponentModel.Description("Expiration time.")]
        [DataMember]
        [Index(false)]
        public DateTime Expiration { get; set; }

        /// <summary>
        /// Consumed time.
        /// </summary>
        [System.ComponentModel.Description("Consumed time.")]
        [DataMember]
        public DateTime ConsumedTime { get; set; } = default!;

        /// <summary>
        /// Serialized grant data. Personal access token records contain only permission metadata, never the token value or signature.
        /// </summary>
        [System.ComponentModel.Description("Serialized grant data. Personal access token records contain only permission metadata, never the token value or signature.")]
        [DataMember]
        public string Data { get; set; } = default!;
    }
}
