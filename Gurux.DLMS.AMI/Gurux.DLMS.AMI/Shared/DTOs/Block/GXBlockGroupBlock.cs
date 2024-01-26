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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Block
{
    /// <summary>
    /// A data contract class representing User Group to User binding object.
    /// </summary>
    [DataContract(Name = "GXBlockGroupBlock"), Serializable]
    [IndexCollection(true, nameof(BlockGroupId), nameof(BlockId), Clustered = true)]
    public class GXBlockGroupBlock : GXTableBase
    {
        /// <summary>
        /// The database ID of the block group.
        /// </summary>
        [DataMember(Name = "BlockGroupId")]
        [ForeignKey(typeof(GXBlockGroup), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid BlockGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// The database ID of the block.
        /// </summary>
        [DataMember(Name = "BlockID")]
        [ForeignKey(typeof(GXBlock), OnDelete = ForeignKeyDelete.Cascade)]
        [StringLength(36)]
        [IsRequired]
        public Guid BlockId
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// The time when the block was added to the block group.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when block was removed from block group.
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
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }
    }
}
