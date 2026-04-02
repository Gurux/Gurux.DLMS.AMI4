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
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.Data;

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
        public string Id { get; set; } = default!;

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
        /// Localized role name.
        /// </summary>
        /// <remarks>
        /// Localized role name is not saved to the database.
        /// </remarks>
        [DataMember]
        [Description("Localized role name.")]
        [Ignore]
        public string? LocalizedName
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
        [ConcurrencyCheck]
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
        /// The creator module.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public GXModule? Module
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

        /// <summary>
        /// Role scopes.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScope))]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXScope>? Scopes
        {
            get;
            set;
        }

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
            NormalizedName = name?.ToUpper();
            Scopes = new List<GXScope>();
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
                LocalizedName = LocalizedName,
                ConcurrencyStamp = ConcurrencyStamp,
                Default = Default,
                Module = Module,
                Removed = Removed,
                Scopes = Scopes
            };
            if (Scopes != null)
            {
                item.Scopes = new List<GXScope>();
                foreach (var it in Scopes)
                {
                    GXScope s = it.Clone();
                    s.Role = item;
                    item.Scopes.Add(s);
                }
            }
            return item;
        }

        /// <summary>
        /// Get list of role scopes.
        /// </summary>
        /// <returns></returns>
        public string[] GetScopes()
        {
            string? name = Name?.ToLower();
            string[] roles;
            if (name != null && Default != true && Scopes?.Any() == true)
            {
                roles = Scopes.Select(s => (name + "." + s.Name?.ToLower()) ?? string.Empty).ToArray();
            }
            else
            {
                roles = [];
            }
            return roles;
        }

        /// <summary>
        /// Returns selected roles.
        /// </summary>
        /// <param name="roles">List of roles where values are search for.</param>
        /// <param name="names">Role names.</param>
        /// <param name="onlyDefault">Only default roles are returned.</param>
        /// <param name="unknownException">Throw exception if name is unknown.</param>
        /// <returns></returns>
        public static GXRole[] GetRoles(IEnumerable<GXRole> roles,
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
        /// Returns scopes from the roles.
        /// </summary>
        /// <param name="roles">List of roles where values are search for.</param>
        /// <param name="names">Scope names.</param>
        /// <param name="unknownException">Throw exception if name is unknown.</param>
        /// <returns></returns>
        public static GXScope[] GetScopes(IEnumerable<GXRole> roles,
            IEnumerable<string>? names,
            bool unknownException)
        {
            List<GXScope> list = new List<GXScope>();
            if (names != null)
            {
                foreach (var name in names)
                {
                    bool found = false;
                    foreach (var role in roles)
                    {
                        string? roleName = role.Name?.ToLower();
                        foreach (var scope in role.Scopes ?? Enumerable.Empty<GXScope>())
                        {
                            if (string.Compare(roleName + "." + scope.Name, name, true) == 0)
                            {
                                found = true;
                                list.Add(scope);
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
                        throw new ArgumentException(string.Format("Unknown role '{0}'.", name));
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
                if (Scopes?.Any() == true)
                {
                    return Name + " [" + Scopes.Select(s => s.Name).Aggregate((current, next) => current + ", " + next) + "]";
                }
                return Name;
            }
            return nameof(GXRole);
        }
    }
}
