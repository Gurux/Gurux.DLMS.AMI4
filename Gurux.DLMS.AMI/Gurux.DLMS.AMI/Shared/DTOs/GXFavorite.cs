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

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Favorite is used to save favorite targets..
    /// </summary>
    public class GXFavorite : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Favorite ID.
        /// </summary>
        [DataMember(Name = "ID")]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Favorite Name.
        /// </summary>
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Favorite type.
        /// </summary>
        [StringLength(64)]
        [Index(false)]
        public string? Type
        {
            get; set;
        }

        /// <summary>
        /// Favorite path.
        /// </summary>
        [StringLength(256)]
        public string? Path
        {
            get;
            set;
        }

        /// <summary>
        /// The Favorite user.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? User
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
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When the block is updated for the last time.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// Favorite target is removed.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Null)]
        [IsRequired(true)]
        public bool? Removed
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
            string str = "";
            if (!string.IsNullOrEmpty(Name))
            {
                str = Name;
            }
            if (!string.IsNullOrEmpty(Path))
            {
                if (str != "")
                {
                    str += " ";
                }
                str += Path;
            }
            if (str != "")
            {
                return str;
            }
            return typeof(GXFavorite).Name;
        }
    }
}
