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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A named value in a persisted enumeration category.</summary>
    [Description("A named value in a persisted enumeration category.")]
    [DataContract]
    public class GXEnumType : GXTableBase, IUnique<int>
    {
        /// <summary>Database identifier of this enumeration entry.</summary>
        [Description("Database identifier of this enumeration entry.")]
        [DataMember]
        [DefaultValue(null)]
        [AutoIncrement]
        public int Id { get; set; }

        /// <summary>Category containing the entry, such as a resource type or log severity category.</summary>
        [Description("Category containing the entry, such as a resource type or log severity category.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        [Index(false)]
        [StringLength(64)]
        [IsRequired]
        public string? Type { get; set; }

        /// <summary>Name of the entry within its category.</summary>
        [Description("Name of the entry within its category.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [Index(false)]
        [StringLength(64)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>Optional integer value represented by this entry, distinct from its database identifier.</summary>
        [Description("Optional integer value represented by this entry, distinct from its database identifier.")]
        [DataMember]
        public int? Value { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Id + " " + Name;
        }
    }
}
