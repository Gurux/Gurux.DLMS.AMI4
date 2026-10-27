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
using Gurux.DLMS.AMI.Shared.DTOs.Block;

namespace Gurux.DLMS.AMI.Shared.DTOs.User
{
    /// <summary>A user's saved configuration for a user interface block.</summary>
    [Description("A user's saved configuration for a user interface block.")]
    [DataContract(Name = "GXUserBlockSettings"), Serializable]
    [IndexCollection(true, nameof(UserId), nameof(BlockId), Clustered = true)]
    public class GXUserBlockSettings
    {
        /// <summary>The database identifier of the user.</summary>
        [Description("The database identifier of the user.")]
        [DataMember(Name = "UserID")]
        [ForeignKey(typeof(GXUser), OnDelete = ForeignKeyDelete.None)]
        [StringLength(36)]
        //ForeignKeyDelete is None because creator of the block is causing multiple cascade paths error in MSSQL.
        public string? UserId { get; set; }

        /// <summary>Block identifier.</summary>
        [Description("Block identifier.")]
        [DataMember(Name = "BlockID"), ForeignKey(typeof(GXBlock),
            OnDelete = ForeignKeyDelete.Cascade)]
        public Guid BlockId { get; set; }

        /// <summary>
        /// Time when user closed the block.
        /// </summary>
        /// <remarks>
        /// If block is closed it's not shown for the user.
        /// </remarks>
        [Description("Time when user closed the block.")]
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Closed { get; set; }

        /// <summary>
        /// User-dependent block settings.
        /// </summary>
        [Description("User-dependent block settings.")]
        [DataMember]
        public string? Settings { get; set; }
    }
}
