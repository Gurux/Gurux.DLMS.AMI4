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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.User
{
    /// <summary>Associates a user account with a user group.</summary>
    [Description("Associates a user account with a user group.")]
    [DataContract(Name = nameof(GXUserGroupUser)), Serializable]
    [IndexCollection(true, nameof(UserGroupId), nameof(UserId), Clustered = true)]
    public class GXUserGroupUser : GXTableBase
    {
        /// <summary>The database identifier of the user group.</summary>
        [Description("The database identifier of the user group.")]
        [DataMember]
        [ForeignKey(typeof(GXUserGroup), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid UserGroupId { get; set; }

        /// <summary>The database identifier of the user.</summary>
        [Description("The database identifier of the user.")]
        [DataMember]
        [ForeignKey(typeof(GXUser), OnDelete = ForeignKeyDelete.None)]
        [StringLength(36)]
        //ForeignKeyDelete is None because creator of the user group is causing multiple cascade paths error in MSSQL.
        public string? UserId { get; set; }

        /// <summary>Time when the record was created.</summary>
        [DataMember]
        [Description("Time when the record was created.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when user was removed from the user group.
        /// </summary>
        [Description("Time when user was removed from the user group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Initializes the creation timestamp before insertion.</summary>
        [Description("Initializes the creation timestamp before insertion.")]
        public override void BeforeAdd()
        {
            if (CreationTime == null)
            {
                CreationTime = DateTime.Now;
            }
        }
    }
}
