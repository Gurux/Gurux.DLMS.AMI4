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
using System.ComponentModel;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using Gurux.Common.Db;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// User roles.
    /// </summary>
    [DataContract(Name = "GXRole"), Serializable]
    public partial class GXRole : IUnique<string>
    {
        /// <summary>
        /// Role Identifier.
        /// </summary>
        [StringLength(36)]
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public string Id{get; set; } = "";

        /// <summary>
        /// Name of the role.
        /// </summary>
        [DataMember]
        [StringLength(256)]
        [Filter(FilterType.Equals)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Normalized name.
        /// </summary>
        [DataMember]
        [Index]
        [StringLength(256)]
        [IsRequired]
        public string? NormalizedName
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
        /// If true, the role is added for the new user as a default role.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default
        {
            get;
            set;
        }

        /// <summary>
        /// Time when role was removed.
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

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                if (Default.HasValue && Default.Value)
                {
                    return Name + " Default";
                }
                return Name;
            }
            return nameof(GXRole);
        }
    }
}
