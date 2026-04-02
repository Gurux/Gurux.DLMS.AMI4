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
using System.Runtime.Serialization;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Add new enum type.
    /// </summary>
    [DataContract]
    public class AddEnumType : IGXRequest<AddEnumTypeResponse>
    {
        /// <summary>
        /// Log item.
        /// </summary>
        [DataMember]
        [Description("Log item.")]
        [Required]
        public GXEnumType? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add enum type response.
    /// </summary>
    [DataContract]
    [Description("Add enum typeresponse.")]
    public class AddEnumTypeResponse
    {
    }

    /// <summary>
    /// Get enum types.
    /// </summary>
    [DataContract]
    [Description("Get enum types.")]
    public class ListEnumTypes : IGXRequest<ListEnumTypesResponse>
    {
        /// <summary>
        /// Filter can be used to filter enum types.
        /// </summary>
        public GXEnumType? Filter
        {
            get;
            set;
        }
      
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of the enum types to retrieve.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public string[]? Select
        {
            get;
            set;
        }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending
        {
            get;
            set;
        }

        /// <summary>
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public int[]? Included
        {
            get;
            set;
        }

        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
        /// </remarks>
        public int[]? Exclude
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get enum types response.
    /// </summary>
    [Description("Get enum types response.")]
    [DataContract]
    public class ListEnumTypesResponse
    {
        /// <summary>
        /// enum types.
        /// </summary>
        [DataMember]
        [Description("enum types.")]
        public GXEnumType[]? Types
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of the enum types.
        /// </summary>
        /// <remarks>
        /// With large databases reading the amount of the data can take a very long time.
        /// In those cases the count is set to -1.
        /// </remarks>
        [DataMember]
        [Description("Amount of the enum types.")]
        public int Count
        {
            get;
            set;
        }
    }
}
