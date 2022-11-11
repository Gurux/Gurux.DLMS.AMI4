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

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// A data contract class representing User Group to User binding object.
    /// </summary>
    [DataContract(Name = "GXModuleGroupModule"), Serializable]
    [IndexCollection(true, nameof(ModuleGroupId), nameof(ModuleId), Clustered = true)]
    public class GXModuleGroupModule : GXTableBase
    {
        /// <summary>
        /// The database ID of the Module group.
        /// </summary>
        [DataMember(Name = "ModuleGroupId")]
        [ForeignKey(typeof(GXModuleGroup), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid ModuleGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// The database ID of the schedule.
        /// </summary>
        [DataMember(Name = "ModuleID")]
        [ForeignKey(typeof(GXModule), OnDelete = ForeignKeyDelete.Cascade)]
        [StringLength(64)]
        public string ModuleId
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// The time when the module was added to the module group.
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
        /// Time when module was removed from module group.
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
