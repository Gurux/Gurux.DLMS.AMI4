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
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A named value in a persisted enumeration category.</summary>
    [Description("A named value in a persisted enumeration category.")]
    [DataContract]
    public class GXEnumType : GXTableBase, IUnique<int>
    {
        /// <summary>Database identifier of this enumeration entry.</summary>
        [Description("Database identifier of this enumeration entry.")]
        [DataMember]
        [DefaultValue(null)]
        [AutoIncrement]
        public int Id { get; set; }

        /// <summary>Category containing the entry, such as a resource type or log severity category.</summary>
        [Description("Category containing the entry, such as a resource type or log severity category.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        [Index(false)]
        [StringLength(64)]
        [IsRequired]
        public string? Type { get; set; }

        /// <summary>Name of the entry within its category.</summary>
        [Description("Name of the entry within its category.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [Index(false)]
        [StringLength(64)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>Optional integer value represented by this entry, distinct from its database identifier.</summary>
        [Description("Optional integer value represented by this entry, distinct from its database identifier.")]
        [DataMember]
        public int? Value { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Id + " " + Name;
        }
    }
}
