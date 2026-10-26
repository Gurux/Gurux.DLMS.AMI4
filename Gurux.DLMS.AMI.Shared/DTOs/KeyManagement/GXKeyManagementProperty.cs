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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.KeyManagement
{
    /// <summary>A key management's saved value for a named application setting.</summary>
    [DataContract(Name = nameof(GXKeyManagementProperty)), Serializable]
    [IndexCollection(true, nameof(KeyManagement), nameof(Module), nameof(Name))]
    public class GXKeyManagementProperty : IUnique<Guid>, IGXProperty
    {
        /// <summary>
        /// Key management property identifier.
        /// </summary>
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Key management property name.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Property owner.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        [JsonIgnore]
        public GXKeyManagement? KeyManagement { get; set; }

        /// <summary>
        /// Settings module.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        public GXModule? Module { get; set; }

        /// <summary>
        /// Key management property value.
        /// </summary>
        [DataMember]
        public string? Value { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// When the key management property was last updated.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }
    }
}
