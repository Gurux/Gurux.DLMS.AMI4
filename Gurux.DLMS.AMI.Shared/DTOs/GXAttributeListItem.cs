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
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>An item in the list of possible values for an attribute.</summary>
    [Description("An item in the list of possible values for an attribute.")]
    public class GXAttributeListItem : IUnique<Guid>
    {
        /// <summary>
        /// Attribute value identifier.
        /// </summary>
        [Description("Attribute value identifier.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Parent attribute template.
        /// </summary>
        [Description("Parent attribute template.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [JsonIgnore]
        public GXAttributeTemplate? Template { get; set; }

        /// <summary>Display label corresponding to the numeric attribute value.</summary>
        [Description("Display label corresponding to the numeric attribute value.")]
        [DataMember]
        public string? UIValue { get; set; }

        /// <summary>Numeric attribute value represented by the display label.</summary>
        [Description("Numeric attribute value represented by the display label.")]
        [DataMember]
        public int Value { get; set; }

        /// <summary>
        /// Mask size.
        /// </summary>
        [Description("Mask size.")]
        [DataMember]
        public int MaskSize { get; set; }

        /// <summary>
        /// Shift.
        /// </summary>
        [Description("Shift.")]
        [DataMember]
        public int Shift { get; set; }

    }
}
