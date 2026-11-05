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

using System.ComponentModel;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>Persisted device authorization codes used during device authentication.</summary>
    [Description("Persisted device authorization codes used during device authentication.")]
    [DataContract(Name = "GXDeviceCodes"), Serializable]
    public class GXDeviceCodes : IUnique<string>
    {
        /// <summary>Unique identifier of the device codes.</summary>
        [Description("Unique identifier of the device codes.")]
        [StringLength(200)]
        [DataMember(Name = "UserCode")]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public string Id
        {
            get
            {
                return DeviceCode;
            }
            set
            {
                DeviceCode = value;
            }
        }

        /// <summary>
        /// Device code.
        /// </summary>
        [Description("Device code.")]
        [DataMember]
        [Index]
        [StringLength(200)]
        [IsRequired]
        public string DeviceCode { get; set; } = default!;

        /// <summary>
        /// Gets or sets the unique identifier for the subject.
        /// </summary>
        [Description("Gets or sets the unique identifier for the subject.")]
        [DataMember]
        [StringLength(200)]
        public string? SubjectId { get; set; }
        /// <summary>Session identifier.</summary>
        [Description("Session identifier.")]
        [DataMember]
        [StringLength(100)]
        public string? SessionId { get; set; }

        /// <summary>Client identifier.</summary>
        [Description("Client identifier.")]
        [DataMember]
        [StringLength(200)]
        [IsRequired]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string ClientId { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>Description of the device codes.</summary>
        [Description("Description of the device codes.")]
        [DataMember]
        [StringLength(200)]
        public string? Description { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Expiration time.
        /// </summary>
        [Description("Expiration time.")]
        [DataMember]
        [IsRequired]
        [Index(false)]
        public DateTime Expiration { get; set; }

        /// <summary>
        /// Data.
        /// </summary>
        [Description("Data.")]
        [DataMember]
        [IsRequired]
        public string Data { get; set; } = default!;
    }
}
