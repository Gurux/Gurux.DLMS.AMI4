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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Notification
{
    /// <summary>
    /// A data contract class representing notification group to notification binding object.
    /// </summary>
    [DataContract(Name = nameof(GXNotificationGroupNotification)), Serializable]
    [IndexCollection(true, nameof(NotificationGroupId), nameof(NotificationId), Clustered = true)]
    public class GXNotificationGroupNotification : GXTableBase
    {
        /// <summary>
        /// The database ID of the notification group.
        /// </summary>
        [DataMember(Name = "NotificationGroupID")]
        [ForeignKey(typeof(GXNotificationGroup), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        public Guid NotificationGroupId
        {
            //ForeignKeyDelete is None because creator of the notification is causing multiple cascade paths error in MSSQL.
            get;
            set;
        }

        /// <summary>
        /// The database ID of the notification.
        /// </summary>
        [DataMember(Name = "NotificationID")]
        [ForeignKey(typeof(GXNotification), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid NotificationId
        {
            get;
            set;
        }

        /// <summary>
		/// Creation time.
		/// The time when the notification was added to the notification group.
		/// </summary>
		[DataMember]
        [Description("Creation time.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when notification was removed from notification group.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed
        {
            get;
            set;
        }

        /// <summary>
        /// Update Creation time.
        /// </summary>
        public override void BeforeAdd()
        {
            CreationTime = DateTime.Now;
        }
    }
}
