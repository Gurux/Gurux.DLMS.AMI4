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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.Data;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// User roles.
    /// </summary>
    [DataContract(Name = "GXRole"), Serializable]
    public class GXRole : IUnique<string>
    {
        /// <summary>
        /// Role Identifier.
        /// </summary>
        [StringLength(36)]
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public string Id { get; set; } = default!;

        /// <summary>
        /// Name of the role.
        /// </summary>
        [DataMember]
        [Index]
        [StringLength(256)]
        [Filter(FilterType.Equals)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Normalized name.
        /// </summary>
        [DataMember]
        [Index(Unique = true)]
        [StringLength(256)]
        [IsRequired]
        public string? NormalizedName { get; set; }

        /// <summary>
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// Is user added to this role when new user is created.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default { get; set; }

        /// <summary>
        /// Notify users when content associated with this role changes.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [IsRequired]
        public bool? NotifyOnContentChange { get; set; }

        /// <summary>
        /// Role description.
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
        /// Time when role was removed.
        /// </summary>
        /// <remarks>
        /// In filter if the removed time is set it will return values that are not null.
        /// </remarks>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// Role permissions.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXPermission), typeof(GXRolePermission))]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXPermission>? Permissions { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXRole()
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Role name.</param>
        public GXRole(string? name)
        {
            Name = name;
            NormalizedName = name?.ToUpperInvariant();
            Permissions = new List<GXPermission>();
        }

        /// <summary>
        /// Make role clone.
        /// </summary>
        /// <returns></returns>
        public GXRole Clone()
        {
            GXRole item = new GXRole()
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
                item.Permissions = new List<GXPermission>();
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
        public IEnumerable<string> GetPermissions()
        {
            return (Permissions ?? []).Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .Select(p => p.Name!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        /// <summary>
        /// Returns selected roles.
        /// </summary>
        /// <param name="roles">List of roles where values are search for.</param>
        /// <param name="names">Role names.</param>
        /// <param name="onlyDefault">Only default roles are returned.</param>
        /// <param name="unknownException">Throw exception if name is unknown.</param>
        /// <returns></returns>
        public static IEnumerable<GXRole> GetRoles(IEnumerable<GXRole> roles,
            IEnumerable<string>? names,
            bool onlyDefault,
            bool unknownException)
        {
            List<GXRole> list = new List<GXRole>();
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
            return list.ToArray();
        }

        /// <summary>
        /// Returns permissions from the roles.
        /// </summary>
        /// <param name="roles">List of roles where values are search for.</param>
        /// <param name="names">Permission names.</param>
        /// <param name="unknownException">Throw exception if name is unknown.</param>
        /// <returns></returns>
        public static IEnumerable<GXPermission> GetPermissions(IEnumerable<GXRole> roles,
            IEnumerable<string>? names,
            bool unknownException)
        {
            List<GXPermission> list = new List<GXPermission>();
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
            return list.ToArray();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                if (Permissions?.Any() == true)
                {
                    return Name + " [" + Permissions.Select(s => s.Name).Aggregate((current, next) => current + ", " + next) + "]";
                }
                return Name;
            }
            return nameof(GXRole);
        }
    }
}



