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

        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
