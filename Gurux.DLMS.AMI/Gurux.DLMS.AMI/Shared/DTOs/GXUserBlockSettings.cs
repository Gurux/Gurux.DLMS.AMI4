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
using Gurux.Common.Db;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// User block is used to save user depending block settings.
    /// </summary>
    [DataContract(Name = "GXUserBlockSettings"), Serializable]
    [IndexCollection(true, nameof(UserId), nameof(BlockId), Clustered = true)]
    public class GXUserBlockSettings
    {
        /// <summary>
        /// The database ID of the user
        /// </summary>
        [DataMember(Name = "UserID")]
        [ForeignKey(typeof(GXUser), OnDelete = ForeignKeyDelete.None)]
        [StringLength(36)]
        public string UserId
        {
            get;
            set;
        }

        /// <summary>
        /// Block ID.
        /// </summary>
        [DataMember(Name = "BlockID"), ForeignKey(typeof(GXBlock), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid BlockId
        {
            get;
            set;
        }

        /// <summary>
        /// Time when user closed the block.
        /// </summary>
        /// <remarks>
        /// If block is closed it's not shown for the user.
        /// </remarks>
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Closed
        {
            get;
            set;
        }

        /// <summary>
        /// User-dependent block settings.
        /// </summary>
        [DataMember]
        public string? Settings
        {
            get;
            set;
        }
    }
}
