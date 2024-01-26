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
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using Gurux.Common.Db;
using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// User roles.
    /// </summary>
    [DataContract(Name = "GXScope"), Serializable]
    public partial class GXScope : IUnique<Guid>
    {
        /// <summary>
        /// Scope identifier.
        /// </summary>
        [StringLength(36)]
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public Guid Id { get; set; }

        /// <summary>
        /// Parent role.
        /// </summary>
        [StringLength(36)]
        [DataMember(Name = "RoleID"), Index(Unique = false)]
        [ForeignKey(typeof(GXRole), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public string? RoleId { get; set; }

        /// <summary>
        /// If true, the scope is added for the new user as a default role.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [IsRequired]
        public bool? Default
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the scope.
        /// </summary>
        [DataMember]
        [StringLength(128)]
        [Filter(FilterType.Equals)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXScope()
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the scope.</param>
        public GXScope(string? name)
        {
            Name = name;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXScope);
        }
    }
}
