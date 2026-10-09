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
using System.ComponentModel;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// User permissions.
    /// </summary>
    [DataContract(Name = "GXPermission"), Serializable]
    public partial class GXPermission : IUnique<Guid>
    {
        /// <summary>
        /// Permission identifier.
        /// </summary>
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public Guid Id { get; set; }

        /// <summary>
        /// If true, the permission is granted to new users by default.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [IsRequired]
        public bool? Default { get; set; }

        /// <summary>
        /// Notify users when content associated with this permission changes.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [IsRequired]
        public bool? NotifyOnContentChange { get; set; }

        /// <summary>
        /// Name of the permission.
        /// </summary>
        [DataMember]
        [StringLength(128)]
        [Index(Unique = true)]
        [Filter(FilterType.Equals)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Permission description.
        /// </summary>
        [DataMember]
        public string? Description { get; set; }

        /// <summary>
        /// Indicates whether the entity is defined and managed by the system.
        /// </summary>
        /// <remarks>
        /// When set to true, the entity is protected from user modifications,
        /// including editing and deletion. System-defined entities are controlled
        /// by the application and are required for core functionality.
        /// </remarks>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? SystemDefined { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXPermission()
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the permission.</param>
        public GXPermission(string? name)
        {
            Name = name;
        }

        /// <summary>
        /// Make permission clone.
        /// </summary>
        /// <returns></returns>
        public GXPermission Clone()
        {
            GXPermission item = new GXPermission()
            {
                Id = Id,
                Default = Default,
                SystemDefined = SystemDefined,
                Name = Name,
                Description = Description,
                NotifyOnContentChange = NotifyOnContentChange,
            };
            return item;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXPermission);
        }
    }
}


