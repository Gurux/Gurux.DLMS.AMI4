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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.History
{
    /// <summary>Defines an entity type with a stable GUID and a name and controls whether its changes are recorded in change history.</summary>
    /// <remarks>The GUID is the persistent identity of the type. Renaming the type must not change its GUID.</remarks>
    [Description("Defines an entity type with a stable GUID and a name and controls whether its changes are recorded in change history.")]
    public sealed class GXChangeHistoryType : IUnique<Guid>
    {
        /// <summary>Gets or sets the stable GUID referenced by change history items to identify the entity type.</summary>
        [Description("Gets or sets the stable GUID referenced by change history items to identify the entity type.")]
        [Key]
        public Guid Id { get; set; }

        /// <summary>Gets or sets whether changes to entities of this type create change history items.</summary>
        /// <remarks>
        /// Defaults to false. Disabling recording does not prevent entity changes from being saved
        /// or the current entity revision from advancing. Existing history items are preserved.
        /// Enabling recording applies to subsequent changes; it does not record earlier changes retroactively.
        /// </remarks>
        [DefaultValue(false)]
        [Description("Whether changes to entities of this type create change history items. Defaults to false; entity updates and revision tracking remain active.")]
        public bool RecordHistory { get; set; }

        /// <summary>Gets or sets the name of the entity type, such as Device or User.</summary>
        [Description("Gets or sets the name of the entity type, such as Device or User.")]
        [IsRequired]
        [StringLength(128), Index(false)]
        public string? Name { get; set; }

        /// <summary>Gets or sets the change history items that reference this entity type.</summary>
        [Description("Gets or sets the change history items that reference this entity type.")]
        [DataMember, ForeignKey(typeof(GXChangeHistoryItem))]
        public List<GXChangeHistoryItem>? Items { get; set; }
    }
}
