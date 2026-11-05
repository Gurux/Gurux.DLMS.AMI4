// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>A named permission that can be assigned to users or roles.</summary>
    [Description("A named permission that can be assigned to users or roles.")]
    [DataContract(Name = "GXPermission"), Serializable]
    public partial class GXPermission : IUnique<Guid>
    {
        /// <summary>
        /// Permission identifier.
        /// </summary>
        [Description("Permission identifier.")]
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public Guid Id { get; set; }

        /// <summary>
        /// If true, the permission is granted to new users by default.
        /// </summary>
        [Description("If true, the permission is granted to new users by default.")]
        [DataMember]
        [DefaultValue(false)]
        [IsRequired]
        public bool? Default { get; set; }

        /// <summary>
        /// Name of the permission.
        /// </summary>
        [Description("Name of the permission.")]
        [DataMember]
        [StringLength(128)]
        [Index(Unique = true)]
        [Filter(FilterType.Equals)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Permission description.
        /// </summary>
        [Description("Permission description.")]
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
        [Description("Indicates whether the entity is defined and managed by the system.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? SystemDefined { get; set; }

        /// <summary>Initializes a new instance of GXPermission.</summary>
        [Description("Initializes a new instance of GXPermission.")]
        public GXPermission()
        {

        }

        /// <summary>Initializes a new instance of GXPermission.</summary>
        /// <param name="name">Name of the permission.</param>
        [Description("Initializes a new instance of GXPermission.")]
        public GXPermission(string? name)
        {
            Name = name;
        }

        /// <summary>
        /// Make permission clone.
        /// </summary>
        /// <returns></returns>
        [Description("Make permission clone.")]
        public GXPermission Clone()
        {
            GXPermission item = new()
            {
                Id = Id,
                Default = Default,
                SystemDefined = SystemDefined,
                Name = Name,
                Description = Description,
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


