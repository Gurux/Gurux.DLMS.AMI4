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
    public class GXAttributeTemplate : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAttributeTemplate()
        {
            ListItems = new List<GXAttributeListItem>();
        }

        /// <summary>
        /// Attribute template Id.
        /// </summary>
        [DataMember]
        [Description("Attribute template Id.")]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Object template.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [IsRequired]
        public GXObjectTemplate? ObjectTemplate
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute index.
        /// </summary>
        [DataMember]
        public int Index
        {
            get;
            set;
        }

        /// <summary>
        /// Object attribute name.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute enumerated values.
        /// </summary>
        [DataMember]
        [ForeignKey]
        public List<GXAttributeListItem> ListItems
        {
            get;
            set;
        }

        /// <summary>
        /// Expiration time tells how often value needs to read from the meter. If it's null it will read every read. If it's DateTime.Max it's read only once.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        public DateTimeOffset? ExpirationTime
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
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When value is last updated.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the schedule group.
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
        /// Remove time.
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
        /// Access level.
        /// </summary>
        [DataMember]
        [Description("Access level.")]
        [DefaultValue(0)]
        public int AccessLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Data type.
        /// </summary>
        [DataMember]
        [Description("Data type.")]
        [DefaultValue(0)]
        public int DataType
        {
            get;
            set;
        }

        /// <summary>
        /// UI Data type.
        /// </summary>
        [DataMember]
        [Description("UI Data type.")]
        [DefaultValue(0)]
        public int UIDataType
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
    }
}
