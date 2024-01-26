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
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Subtotal
{
    /// <summary>
    /// A data contract class representing subtotal to script group binding object.
    /// </summary>
    [DataContract(Name = "GXSubtotalScriptGroup"), Serializable]
    [IndexCollection(true, nameof(SubtotalId), nameof(ScriptGroupId), Clustered = true)]
    public class GXSubtotalScriptGroup : GXTableBase
    {
        /// <summary>
        /// The database ID of the subtotal.
        /// </summary>
        [DataMember(Name = "SubtotalID")]
        [ForeignKey(typeof(GXSubtotal), OnDelete = ForeignKeyDelete.Cascade)]
        [StringLength(36)]
        [IsRequired]
        public Guid SubtotalId
        {
            get;
            set;
        }

        /// <summary>
        /// The database ID of the script group.
        /// </summary>
        [DataMember(Name = "ScriptGroupId")]
        [ForeignKey(typeof(GXScriptGroup), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid ScriptGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// The time when the subtotal was joined to the device.
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
        /// Time when device was removed from subtotal.
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
