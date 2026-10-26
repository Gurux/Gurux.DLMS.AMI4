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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Notification
{
    /// <summary>A notification subscription defining its event sources, delivery channel, and destination.</summary>
    public class GXNotification : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXNotification()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new notification is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Notification name.</param>
        public GXNotification(string? name)
        {
            Enabled = true;
            Name = name;
        }

        /// <summary>
        /// Notification ID.
        /// </summary>
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Notification Name.
        /// </summary>
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Description.
        /// </summary>
        [DataMember]
        [StringLength(256)]
        [Description("Description.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description { get; set; }

        /// <summary>
        /// Script method that this notification uses.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXScriptMethod), OnDelete = ForeignKeyDelete.None)]
        //ForeignKeyDelete is None because creator of the script is causing multiple cascade paths error in MSSQL.
        public GXScriptMethod? ScriptMethod { get; set; }

        /// <summary>
        /// Is notification enabled.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Notification target type. E.g. Mail, SMS, Toaster, etc.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public GXNotificationType? Type { get; set; }

        /// <summary>
        /// Notification sources. E.g. agent, user, system log, etc.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Sources { get; set; }


        /// <summary>Delivery destination, such as email addresses or a network address, interpreted by the selected channel.</summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Destination { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// The creator of the notification.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// When the notification is sent for the last time.
        /// </summary>
        [Description("When the notification is sent for the last time.")]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Detected { get; set; }

        /// <summary>Minimum interval between notification deliveries, in seconds.</summary>
        [DataMember]
        public int MinimumDeliveryTime { get; set; }

        /// <summary>
        /// Time when notification was removed.
        /// </summary>
        /// <remarks>
        /// In filter if the removed time is set it will return values that are not null.
        /// </remarks>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// When the notification is updated for the last time.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>
        /// Used trace level.
        /// </summary>
        [DataMember]
        [DefaultValue(System.Diagnostics.TraceLevel.Error)]
        [Description("Used trace level.")]
        [IsRequired]
        public TraceLevel? TraceLevel { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == null)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Updates the last-modified timestamp before persisting changes.
        /// </summary>
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string? str = Name;
            if (Enabled.HasValue && Enabled.Value)
            {
                str += ", Enabled";
            }
            if (string.IsNullOrEmpty(str))
            {
                str = nameof(GXNotification);
            }
            return str;
        }
    }
}
