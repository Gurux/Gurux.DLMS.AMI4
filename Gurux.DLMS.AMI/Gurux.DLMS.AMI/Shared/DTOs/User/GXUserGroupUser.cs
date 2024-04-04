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
using Gurux.Common.Db;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.User
{
    /// <summary>
    /// A data contract class representing user group to user binding object.
    /// </summary>
    [DataContract(Name = "GXUserGroupUser"), Serializable]
    [IndexCollection(true, nameof(UserGroupId), nameof(UserId), Clustered = true)]
    public class GXUserGroupUser : GXTableBase
    {
        /// <summary>
        /// The database ID of the user group
        /// </summary>
        [DataMember(Name = "UserGroupID")]
        [ForeignKey(typeof(GXUserGroup), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid UserGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// The database ID of the user
        /// </summary>
        [DataMember(Name = "UserID")]
        [ForeignKey(typeof(GXUser), OnDelete = ForeignKeyDelete.Cascade)]
        [StringLength(36)]
        public string UserId
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Description("Creation time.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when user was removed from the user group.
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
