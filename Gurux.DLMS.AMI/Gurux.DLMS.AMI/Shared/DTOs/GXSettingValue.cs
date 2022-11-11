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
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Configuration settings is saved for this table.
    /// </summary>
    [IndexCollection(true, nameof(Id), nameof(Name))]
    public class GXSettingValue : GXTableBase, IUnique<Guid> 
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSettingValue()
        {
            //User can modify the configuration value by default.
            AccessType = AccessType.Edit;
            Name = "";
        }

        /// <summary>
        /// System setting identifier.
        /// </summary>
        [DataMember(Name = "ID")]
        [Description("Setting identifier.")]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the setting.
        /// </summary>
        [DataMember]
        [StringLength(128)]
        [Index(false)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Setting value.
        /// </summary>
        [DataMember]
        [Description("Setting value")]
        public string? Value
        {
            get;
            set;
        }       

        /// <summary>
        /// Setting value type.
        /// </summary>
        [DataMember]
        [StringLength(32)]
        public string? ValueType
        {
            get;
            set;
        }

        /// <summary>
        /// Access type
        /// </summary>
        [DataMember]
        public AccessType AccessType
        {
            get;
            set;
        }
        /// <summary>
        /// Settings group.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        public GXSettingGroup? Group
        {
            get;
            set;
        }

        /// <summary>
        /// Setting value description.
        /// </summary>
        [DataMember]
        [StringLength(256)]
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When value is last updated.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the item.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        public bool Modified
        {
            get;
            set;
        }

        /// <summary>
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        public string? ConcurrencyStamp
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

        /// <summary>
        /// Update concurrency stamp.
        /// </summary>
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name + ": " + Value;
        }
    }
}
