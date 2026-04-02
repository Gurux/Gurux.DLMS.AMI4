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
// This file is a part of Gurux Module Framework.
//
// Gurux Module Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Module Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Notification
{
    /// <summary>
    /// A data contract class representing notification to module group binding object.
    /// </summary>
    [IndexCollection(true, nameof(ModuleGroupId), nameof(NotificationId), Clustered = true)]
    public class GXNotificationModuleGroup
    {
        /// <summary>
        /// Notification ID.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXNotification), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        public Guid NotificationId
        {
            //ForeignKeyDelete is None because creator of the module group is causing multiple cascade paths error in MSSQL.
            get;
            set;
        }

        /// <summary>
        /// Module group ID.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXModuleGroup), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid ModuleGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// The time when the module group was added to the notification notification.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when module group was removed from notification group.
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
    }
}
