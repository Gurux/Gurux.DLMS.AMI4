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
using Gurux.DLMS.AMI.Shared.DTOs.Content;
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Localized resources.
    /// </summary>
    [IndexCollection(true, nameof(Language), nameof(Hash))]
    public class GXLocalizedResource : IUnique<Guid>
    {
        /// <summary>
        /// Localized string identifier.
        /// </summary>
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Localized string.
        /// </summary>
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Value { get; set; }

        /// <summary>
        /// Hash value for Localized string key.
        /// </summary>
        [Index(false)]
        [Filter(FilterType.Exact)]
        [StringLength(64)]
        [IsRequired]
        public string? Hash
        {
            get;
            set;
        }

        /// <summary>
        /// Status is used to tell when a resource is missing. 
        /// </summary>
        [Index(false)]
        [DefaultValue(0)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public int? Status
        {
            get;
            set;
        }

        /// <summary>
        /// Parent language.
        /// </summary>
        /// <remarks>
        /// Creator 
        /// </remarks>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [Index(false)]
        public GXLanguage? Language
        {
            get;
            set;
        }

        /// <summary>
        /// The creator of the localized resource.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When was the localized resource last updated.
        /// </summary>
        [Description("When was the localized resource last updated.")]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
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

        /// <inheritdoc/>
        public override string ToString()
        {
            return Id + ": " + Value;
        }
    }
}
