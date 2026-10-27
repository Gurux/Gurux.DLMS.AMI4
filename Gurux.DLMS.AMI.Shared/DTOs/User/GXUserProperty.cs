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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.User
{
    /// <summary>A user's saved value for a named application setting.</summary>
    [Description("A user's saved value for a named application setting.")]
    [DataContract(Name = nameof(GXUserProperty)), Serializable]
    [IndexCollection(true, nameof(User), nameof(Module), nameof(Name))]
    public class GXUserProperty : IUnique<Guid>, IGXProperty
    {
        /// <summary>
        /// User settings identifier.
        /// </summary>
        [Description("User settings identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// User settings name.
        /// </summary>
        [Description("User settings name.")]
        [DataMember]
        [StringLength(64)]
        [IsRequired]
        public string? Name { get; set; }


        /// <summary>
        /// Settings owner.
        /// </summary>
        [Description("Settings owner.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? User { get; set; }

        /// <summary>
        /// Settings module.
        /// </summary>
        [Description("Settings module.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        // Module properties are deleted explicitly when a module is removed to avoid multiple
        // SQL Server cascade paths through the owning user and the module creator.
        [Filter(FilterType.Exact)]
        public GXModule? Module { get; set; }

        /// <summary>
        /// User settings value.
        /// </summary>
        [Description("User settings value.")]
        [DataMember]
        public string? Value { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the user property was last updated.</summary>
        [Description("Time when the user property was last updated.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }
    }
}
