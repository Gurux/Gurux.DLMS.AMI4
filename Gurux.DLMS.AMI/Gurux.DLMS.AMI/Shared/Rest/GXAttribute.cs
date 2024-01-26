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
using Gurux.Common;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get attribute.
    /// </summary>
    public class GetAttributeResponse
    {
        /// <summary>
        /// Attribute information.
        /// </summary>
        [IncludeSwagger(typeof(GXObject),
                nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXModule),
                nameof(GXModule.Id))]
        [ExcludeSwagger(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.ObjectTemplate))]
        [ExcludeSwagger(typeof(GXAttributeParameter),
                nameof(GXAttributeParameter.Attribute))]
        [ExcludeSwagger(typeof(GXAttribute),
            nameof(GXAttribute.Tasks),
            nameof(GXAttribute.Values),
            nameof(GXAttribute.Errors))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXAttribute Item
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Add COSEM attribute.
    /// </summary>
    [DataContract]
    public class UpdateAttribute : IGXRequest<UpdateAttributeResponse>
    {
        /// <summary>
        /// Added COSEM attributes.
        /// </summary>
        [DataMember]
        /// <summary>
        /// Attribute information.
        /// </summary>
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id))]
        [IncludeSwagger(typeof(GXModule), nameof(GXModule.Id))]
        [ExcludeSwagger(typeof(GXAttributeParameter), nameof(GXAttributeParameter.Attribute))]
        [ExcludeSwagger(typeof(GXAttribute),
            nameof(GXAttribute.Tasks),
            nameof(GXAttribute.Values),
            nameof(GXAttribute.Errors))]
        public GXAttribute[] Attributes
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add COSEM attribute Reply.
    /// </summary>
    [DataContract]
    public class UpdateAttributeResponse
    {
        /// <summary>
        /// Attribute identifiers.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get attribute.
    /// </summary>
    [DataContract]
    public class ListAttributes : IGXRequest<ListAttributesResponse>
    {
        /// <summary>
        /// Profile Generic Index. This is used only with Profile Generic attributes.
        /// </summary>
        [DataMember]
        public UInt64 Index
        {
            get;
            set;
        }

        /// <summary>
        /// Profile Generic Count. This is used only with Profile Generic attributes.
        /// </summary>
        [DataMember]
        public UInt64 Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter attributes.
        /// </summary>
        [ExcludeSwagger(typeof(GXAttribute),
            nameof(GXAttribute.Object),
            nameof(GXAttribute.Template),
            nameof(GXAttribute.Tasks),
            nameof(GXAttribute.Errors),
            nameof(GXAttribute.Parameters),
            nameof(GXAttribute.Values))]
        public GXAttribute? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access agents from all users.
        /// </summary>
        /// <remarks>
        /// If true, agents from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
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
        public Guid[]? Included
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
        public Guid[]? Exclude
        {
            get;
            set;
        }

        /// <summary>
        /// Device Ids.
        /// </summary>
        public Guid[]? Devices
        {
            get;
            set;
        }

        /// <summary>
        /// Filtered object types.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public int[]? ObjectTypes
        {
            get;
            set;
        }

        /// <summary>
        /// Ignored object types.
        /// </summary>
        public int[]? IgnoredObjectTypes
        {
            get;
            set;
        }
    }

    /// <summary>
    /// List COSEM attribute response.
    /// </summary>
    [DataContract]
    public class ListAttributesResponse
    {

        /// <summary>
        /// List of COSEM attributes.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXAttribute),
            nameof(GXAttribute.Object),
            nameof(GXAttribute.Tasks),
            nameof(GXAttribute.Errors),
            nameof(GXAttribute.Parameters),
            nameof(GXAttribute.Values))]
        [IncludeSwagger(typeof(GXAttributeTemplate),
            nameof(GXAttributeTemplate.Id),
            nameof(GXAttributeTemplate.Name))]
        public GXAttribute[] Attributes
        {
            get;
            set;
        }
        /// <summary>
        /// Total count of the attributes.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove COSEM attributes.
    /// </summary>
    public class RemoveAttribute : IGXRequest<RemoveAttributeResponse>
    {
        /// <summary>
        /// Removed COSEM attributes identifiers.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove COSEM attribute response.
    /// </summary>
    [DataContract]
    public class RemoveAttributeResponse
    {
    }

    /// <summary>
    /// Update data type of the attribute.
    /// </summary>
    [DataContract]
    public class UpdateDatatype : IGXRequest<UpdateAttributeResponse>
    {
        /// <summary>
        /// Updated attributes.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXAttribute),
            nameof(GXAttribute.Object),
            nameof(GXAttribute.Tasks),
            nameof(GXAttribute.Errors),
            nameof(GXAttribute.Values),
            nameof(GXAttribute.Parameters))]
        [IncludeSwagger(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.Id))]
        public GXAttribute[] Items
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update data type of the attribute reply.
    /// </summary>
    [DataContract]
    public class UpdateDatatypeResponse
    {
    }
}
