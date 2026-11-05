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

using System.ComponentModel;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.Data;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>A named authorization role with its claims and permissions.</summary>
    [Description("A named authorization role with its claims and permissions.")]
    [DataContract(Name = "GXRole"), Serializable]
    public class GXRole : IUnique<string>
    {
        /// <summary>
        /// Role Identifier.
        /// </summary>
        [Description("Role Identifier.")]
        [StringLength(36)]
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public string Id { get; set; } = "";

        /// <summary>
        /// Name of the role.
        /// </summary>
        [Description("Name of the role.")]
        [DataMember]
        [Index]
        [StringLength(256)]
        [Filter(FilterType.Equals)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Normalized name.
        /// </summary>
        [Description("Normalized name.")]
        [DataMember]
        [Index(Unique = true)]
        [StringLength(256)]
        [IsRequired]
        public string? NormalizedName { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>Whether this is the default role.</summary>
        [Description("Whether this is the default role.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default { get; set; }

        /// <summary>
        /// Role description.
        /// </summary>
        [Description("Role description.")]
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

        /// <summary>
        /// Time when role was removed.
        /// </summary>
        /// <remarks>
        /// In filter if the removed time is set it will return values that are not null.
        /// </remarks>
        [Description("Time when role was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// Role permissions.
        /// </summary>
        [Description("Role permissions.")]
        [DataMember]
        [ForeignKey(typeof(GXPermission), typeof(GXRolePermission))]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXPermission>? Permissions { get; set; }

        /// <summary>Initializes a new instance of GXRole.</summary>
        [Description("Initializes a new instance of GXRole.")]
        public GXRole()
        {

        }

        /// <summary>Initializes a new instance of GXRole.</summary>
        /// <param name="name">Role name.</param>
        [Description("Initializes a new instance of GXRole.")]
        public GXRole(string? name)
        {
            Name = name;
            NormalizedName = name?.ToUpperInvariant();
            Permissions = [];
        }

        /// <summary>
        /// Make role clone.
        /// </summary>
        /// <returns></returns>
        [Description("Make role clone.")]
        public GXRole Clone()
        {
            GXRole item = new()
            {
                Id = Id,
                Name = Name,
                NormalizedName = NormalizedName,
                ConcurrencyStamp = ConcurrencyStamp,
                Removed = Removed,
                Permissions = Permissions
            };
            if (Permissions != null)
            {
                item.Permissions = [];
                foreach (var it in Permissions)
                {
                    GXPermission s = it.Clone();
                    item.Permissions.Add(s);
                }
            }
            return item;
        }

        /// <summary>
        /// Get list of role permissions.
        /// </summary>
        /// <returns></returns>
        [Description("Get list of role permissions.")]
        public IEnumerable<string> GetPermissions()
        {
            return [.. (Permissions ?? []).Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .Select(p => p.Name!).Distinct(StringComparer.OrdinalIgnoreCase)];
        }

        /// <summary>
        /// Returns selected roles.
        /// </summary>
        /// <param name="roles">List of roles where values are search for.</param>
        /// <param name="names">Role names.</param>
        /// <param name="onlyDefault">Only default roles are returned.</param>
        /// <param name="unknownException">Throw exception if name is unknown.</param>
        /// <returns></returns>
        [Description("Returns selected roles.")]
        public static IEnumerable<GXRole> GetRoles(IEnumerable<GXRole> roles,
            IEnumerable<string>? names,
            bool onlyDefault,
            bool unknownException)
        {
            List<GXRole> list = [];
            if (names != null)
            {

                foreach (var name in names)
                {
                    bool found = false;
                    foreach (var role in roles)
                    {
                        if ((!onlyDefault || role.Default == true) && string.Compare(role.Name, name, true) == 0)
                        {
                            found = true;
                            if (!list.Where(w => w.Name?.ToLower() == role.Name?.ToLower()).Any())
                            {
                                list.Add(role);
                            }
                            break;
                        }
                    }
                    if (unknownException && !found)
                    {
                        throw new ArgumentException(string.Format("Unknown role '{0}'.", name));
                    }
                }
            }
            return [.. list];
        }

        /// <summary>
        /// Returns permissions from the roles.
        /// </summary>
        /// <param name="roles">List of roles where values are search for.</param>
        /// <param name="names">Permission names.</param>
        /// <param name="unknownException">Throw exception if name is unknown.</param>
        /// <returns></returns>
        [Description("Returns permissions from the roles.")]
        public static IEnumerable<GXPermission> GetPermissions(IEnumerable<GXRole> roles,
            IEnumerable<string>? names,
            bool unknownException)
        {
            List<GXPermission> list = [];
            if (names != null)
            {
                foreach (var name in names)
                {
                    bool found = false;
                    foreach (var role in roles)
                    {
                        foreach (var permission in role.Permissions ?? Enumerable.Empty<GXPermission>())
                        {
                            if (string.Equals(permission.Name, name, StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                if (!list.Any(p => string.Equals(p.Name, permission.Name, StringComparison.OrdinalIgnoreCase)))
                                {
                                    list.Add(permission);
                                }

                                break;
                            }
                        }
                        if (found)
                        {
                            break;
                        }
                    }
                    if (unknownException && !found)
                    {
                        throw new ArgumentException(string.Format("Unknown permission '{0}'.", name));
                    }
                }
            }
            return [.. list];
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                if (Permissions is { Count: > 0 })
                {
                    return Name + " [" + Permissions.Select(s => s.Name).Aggregate((current, next) => current + ", " + next) + "]";
                }
                return Name;
            }
            return nameof(GXRole);
        }
    }
}



