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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Appearance is used to customice appearance theme and icons.
    /// </summary>
    public class GXAppearance : GXTableBase, IUnique<string>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAppearance()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAppearance(byte type, string id, string description)
        {
            ResourceType = type;
            Id = id;
            Description = description;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAppearance(byte type, string id, string description, string value)
        {
            ResourceType = type;
            Id = id;
            Description = description;
            Value = value;
        }

        /// <summary>
        /// Appearance ID.
        /// </summary>
        [Key]
        [DataMember(Name = "ID")]
        [StringLength(64)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string Id
        {
            get;
            set;
        } = "";

        /// <summary>
        /// The Appearance creator.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        [IsRequired]
        public GXUser? Creator
        {
            get;
            set;
        }

        /// <summary>
        /// Is appearance active.
        /// </summary>
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Category.
        /// </summary>
        [DataMember]
        [Filter(FilterType.Equals)]
        [DefaultValue(null)]
        [StringLength(64)]
        [Index(Unique = false)]
        public string? Category
        {
            get;
            set;
        }

        /// <summary>
        /// Appearance description.
        /// </summary>
        [DataMember]
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Appearance value.
        /// </summary>
        [DataMember]
        [IsRequired]
        public string? Value
        {
            get;
            set;
        }

        /// <summary>
        /// Resource type.
        /// </summary>
        [IsRequired]
        [DataMember]
        [Filter(FilterType.Exact)]
        [Index(false)]
        public byte? ResourceType
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
        public DateTimeOffset? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when Appearance was removed.
        /// </summary>
        /// <remarks>
        /// In filter if the removed time is set it will return values that are not null.
        /// </remarks>
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
        /// When the Appearance is updated for the last time.
        /// </summary>
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
        [JsonIgnore]
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
            if (CreationTime == null)
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
            if (!string.IsNullOrEmpty(Id))
            {
                return Id;
            }
            return nameof(GXAppearance);
        }
    }
}
