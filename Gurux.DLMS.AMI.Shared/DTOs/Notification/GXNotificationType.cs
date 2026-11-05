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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Notification
{
    /// <summary>A notification delivery channel, such as email or a module-provided channel.</summary>

    [Description("A notification delivery channel, such as email or a module-provided channel.")]
    [DataContract(Name = "GXNotificationType"), Serializable]
    public class GXNotificationType : IUnique<Guid>
    {
        /// <summary>Notification identifier.</summary>
        [Description("Notification identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>Whether the notification type is enabled.</summary>
        [Description("Whether the notification type is enabled.")]
        [DataMember]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Notification type name.
        /// </summary>
        [Description("Notification type name.")]
        [DataMember]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Code is a unique normalized type name used in the API and database. 
        /// It is derived from the Name property and is 
        /// typically converted to kebab-case (lowercase with hyphens).
        /// </summary>
        /// <remarks>
        /// Name is converted to kebab-case.
        /// </remarks>
        [Description("Code is a unique normalized type name used in the API and database. ")]
        [DataMember]
        [StringLength(128)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [Index(true)]
        [IsRequired]
        public string? Code { get; set; }

        /// <summary>
        /// Creation mode defines how notifications are created for a user. 
        /// It can be Single, Automatic, or Multiple.
        /// </summary>
        [Description("Creation mode defines how notifications are created for a user.")]
        [DataMember]
        [DefaultValue(NotificationCreationMode.Single)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public NotificationCreationMode? CreationMode { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the notification type was last updated.</summary>
        [DataMember]
        [Description("Time when the notification type was last updated.")]
        [DefaultValue(null)]
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

        /// <summary>
        /// The creator of the notification type.
        /// </summary>
        [Description("The creator of the notification type.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Description of the notification.
        /// </summary>
        [DataMember]
        [StringLength(256)]
        [Description("Description of the notification.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description { get; set; }

        /// <summary>
        /// Named visibility policy; empty means public within the surrounding resource.
        /// </summary>
        [Description("Named visibility policy; empty means public within the surrounding resource.")]
        [DataMember, Ignore(IgnoreType.Db)]
        public List<string>? Policies { get; set; }

        /// <summary>Time when the notification type was marked as removed.</summary>
        [Description("Time when the notification type was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
