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
using System.Text;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A recorded COSEM attribute value and its capture metadata.</summary>
    [Description("A recorded COSEM attribute value and its capture metadata.")]
    [DataContract]
    public class GXValue : IUnique<Guid>
    {
        /// <summary>
        /// Value identifier.
        /// </summary>
        [DataMember]
        [Description("Value identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>COSEM attribute associated with the recorded value.</summary>
        [Description("COSEM attribute associated with the recorded value.")]
        [DataMember]
        [ForeignKey(typeof(GXAttribute), OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXAttribute? Attribute { get; set; }

        /// <summary>
        /// Attribute value.
        /// </summary>
        [DataMember]
        [Description("Attribute value.")]
        [Filter(FilterType.Contains)]
        public string? Value { get; set; }

        /// <summary>
        /// Block index.
        /// </summary>
        /// <remarks>
        /// Block index is used if reply data is send in blocks
        /// and not all at the time.
        /// This can be used when a larget amount of
        /// data is read and if it can't fit to the memory at once.
        /// </remarks>
        [DataMember]
        [Description("Block index.")]
        [Filter(FilterType.Contains)]
        public UInt32? BlockIndex { get; set; }

        /// <summary>
        /// Read time.
        /// </summary>
        [DataMember]
        [Description("Read time.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? Read { get; set; }

        /// <summary>
        /// The user has entered a new value.
        /// </summary>
        /// <remarks>
        /// If value is false then the value is read from the meter.
        /// </remarks>
        [DataMember]
        [Description("The user has entered a new value.")]
        [Filter(FilterType.Exact)]
        [DefaultValue(false)]
        [IsRequired]
        public bool? User { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new();
            if (Read != null)
            {
                sb.Append(Read.ToString());
                sb.Append(' ');
            }
            sb.Append(Value);
            return sb.ToString();
        }
    }
}
