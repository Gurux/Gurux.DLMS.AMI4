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
using Gurux.Common.Db;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// System configuration.
    /// </summary>
    [Description("System configuration")]
    public class GXConfiguration : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// System setting category identifier.
        /// </summary>
        [DataMember(Name = "ID")]
        [Description("System setting category identifier.")]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// System setting category Name.
        /// </summary>
        [DataMember]
        [Description("System setting category Name.")]
        [Index(true)]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Optional configuration User interface path.
        /// </summary>
        /// <remarks>
        /// This is used when custom UI is needed, for example with Cultures.
        /// </remarks>
        [DataMember]
        [StringLength(128)]
        public string? Path
        {
            get;
            set;
        }

        /// <summary>
        /// Order number.
        /// </summary>
        [DataMember]
        public int Order
        {
            get;
            set;
        }

        /// <summary>
        /// Configuration description.
        /// </summary>
        [DataMember]
        [StringLength(256)]
        [DefaultValue(null)]
        [Description("Configuration description.")]
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Icon name.
        /// </summary>
        [Description("Icon name.")]
        public string? Icon
        {
            get;
            set;
        }

        /// <summary>
        /// Configuration settings.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        public string? Settings
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When value is last updated.
        /// </summary>
        [DataMember]
        [Description("When value is last updated.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the schedule group.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        public bool Modified
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

        /// <summary>
        /// Localized resources for this configuration.
        /// </summary>
        /// <remarks>
        /// This is used only for database.
        /// </remarks>
        [DataMember]
        [JsonIgnore]
        public GXLocalizedResource[]? Resources
        {
            get;
            set;
        }

        /// <summary>
        /// Localized resources for this configuration.
        /// </summary>
        /// <remarks>
        /// Localized resources are return with this.
        /// </remarks>
        [DataMember]
        [Ignore(IgnoreType.Db)]
        public GXLanguage[]? Languages
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXConfiguration()
        {
            Name = "";
        }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Update concurrency stamp.
        /// </summary>
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXConfiguration);
        }
    }

}
