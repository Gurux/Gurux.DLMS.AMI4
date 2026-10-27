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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A translated resource value associated with a language and resource key.</summary>
    [Description("A translated resource value associated with a language and resource key.")]
    [IndexCollection(true, nameof(Language), nameof(Hash))]
    public class GXLocalizedResource : IUnique<Guid>
    {
        /// <summary>
        /// Localized string identifier.
        /// </summary>
        [Description("Localized string identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Localized string.
        /// </summary>
        [Description("Localized string.")]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Value { get; set; }

        /// <summary>
        /// Hash value for Localized string key.
        /// </summary>
        [Description("Hash value for Localized string key.")]
        [Index(false)]
        [Filter(FilterType.Exact)]
        [StringLength(64)]
        [IsRequired]
        public string? Hash { get; set; }

        /// <summary>
        /// Status is used to tell when a resource is missing.
        /// </summary>
        [Description("Status is used to tell when a resource is missing.")]
        [Index(false)]
        [DefaultValue(0)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public int? Status { get; set; }

        /// <summary>
        /// Parent language.
        /// </summary>
        /// <remarks>
        /// Creator
        /// </remarks>
        [Description("Parent language.")]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [Index(false)]
        public GXLanguage? Language { get; set; }

        /// <summary>
        /// The creator of the localized resource.
        /// </summary>
        [Description("The creator of the localized resource.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the localized resource was last updated.</summary>
        [Description("Time when the localized resource was last updated.")]
        [DefaultValue(null)]
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

        /// <inheritdoc/>
        public override string ToString()
        {
            return Id + ": " + Value;
        }
        /// <summary>Load this resource during client initialization when access is allowed.</summary>
        [Description("Load this resource during client initialization when access is allowed.")]
        [DataMember, IsRequired, DefaultValue(false), Filter(FilterType.Exact)]
        public bool? Preload { get; set; }
    }
}
