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

using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>A persisted authentication key.</summary>
    [System.ComponentModel.Description("A persisted authentication key.")]
    [DataContract(Name = "GXKey"), Serializable]
    public partial class GXKey : IUnique<string>
    {
        /// <summary>Unique identifier of the key.</summary>
        [System.ComponentModel.Description("Unique identifier of the key.")]
        [StringLength(450)]
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        public string Id { get; set; } = "";

        /// <summary>
        /// Key version.
        /// </summary>
        [System.ComponentModel.Description("Key version.")]
        [DataMember]
        public int Version { get; set; }

        /// <summary>
        /// When key is created.
        /// </summary>
        [System.ComponentModel.Description("When key is created.")]
        [DataMember]
        public DateTime Created { get; set; }

        /// <summary>
        /// Key use.
        /// </summary>
        [System.ComponentModel.Description("Key use.")]
        [Index(Unique = false)]
        [DataMember]
        [StringLength(450)]
        public string? Use { get; set; }

        /// <summary>
        /// Key algorithm.
        /// </summary>
        [System.ComponentModel.Description("Key algorithm.")]
        [DataMember]
        [StringLength(100)]
        public string? Algorithm { get; set; }

        /// <summary>
        /// Is Key X509 certificate.
        /// </summary>
        [System.ComponentModel.Description("Is Key X509 certificate.")]
        [DataMember]
        [IsRequired]
        public bool IsX509Certificate { get; set; }

        /// <summary>
        /// Is key data protected.
        /// </summary>
        [System.ComponentModel.Description("Is key data protected.")]
        [DataMember]
        [IsRequired]
        public bool DataProtected { get; set; }

        /// <summary>
        /// Key data.
        /// </summary>
        [System.ComponentModel.Description("Key data.")]
        [DataMember]
        public string? Data { get; set; }
    }
}
