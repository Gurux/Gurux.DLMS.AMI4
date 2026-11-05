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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.User
{
    /// <summary>A user's saved value for a named application setting.</summary>
    [Description("A user's saved value for a named application setting.")]
    [DataContract(Name = nameof(GXUserProperty)), Serializable]
    [IndexCollection(true, nameof(User), nameof(Module), nameof(Name))]
    public class GXUserProperty : IUnique<Guid>, IGXProperty
    {
        /// <summary>
        /// User settings identifier.
        /// </summary>
        [Description("User settings identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// User settings name.
        /// </summary>
        [Description("User settings name.")]
        [DataMember]
        [StringLength(64)]
        [IsRequired]
        public string? Name { get; set; }


        /// <summary>
        /// Settings owner.
        /// </summary>
        [Description("Settings owner.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? User { get; set; }

        /// <summary>
        /// Settings module.
        /// </summary>
        [Description("Settings module.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        // Module properties are deleted explicitly when a module is removed to avoid multiple
        // SQL Server cascade paths through the owning user and the module creator.
        [Filter(FilterType.Exact)]
        public GXModule? Module { get; set; }

        /// <summary>
        /// User settings value.
        /// </summary>
        [Description("User settings value.")]
        [DataMember]
        public string? Value { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the user property was last updated.</summary>
        [Description("Time when the user property was last updated.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }
    }
}
