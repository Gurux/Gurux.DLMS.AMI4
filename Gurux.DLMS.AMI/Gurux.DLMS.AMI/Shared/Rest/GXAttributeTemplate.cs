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
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get attribute template.
    /// </summary>
    public class GetAttributeTemplateResponse
    {
        /// <summary>
        /// AttributeTemplate information.
        /// </summary>
        [ExcludeSwagger(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.ObjectTemplate),
                nameof(GXAttributeTemplate.ListItems))]
        public GXAttributeTemplate? Item
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Add COSEM attribute template.
    /// </summary>
    [DataContract]
    public class UpdateAttributeTemplate : IGXRequest<UpdateAttributeTemplateResponse>
    {
        /// <summary>
        /// Added COSEM attribute templates.
        /// </summary>
        [DataMember]
        /// <summary>
        /// AttributeTemplate information.
        /// </summary>
        [IncludeSwagger(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id))]
        [ExcludeSwagger(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.ListItems))]
        public GXAttributeTemplate[]? AttributeTemplates
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add COSEM attribute template Reply.
    /// </summary>
    [DataContract]
    public class UpdateAttributeTemplateResponse
    {
        /// <summary>
        /// AttributeTemplate identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get attribute template.
    /// </summary>
    [DataContract]
    public class ListAttributeTemplates : IGXRequest<ListAttributeTemplatesResponse>
    {
        /// <summary>
        /// Profile Generic Index. This is used only with Profile Generic attribute templates.
        /// </summary>
        [DataMember]
        public UInt64 Index
        {
            get;
            set;
        }

        /// <summary>
        /// Profile Generic Count. This is used only with Profile Generic attribute templates.
        /// </summary>
        [DataMember]
        public UInt64 Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter attribute templates.
        /// </summary>
        [ExcludeSwagger(typeof(GXAttributeTemplate),
               nameof(GXAttributeTemplate.ObjectTemplate),
               nameof(GXAttributeTemplate.ListItems))]
        public GXAttributeTemplate? Filter
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
        /// Device template Ids.
        /// </summary>
        public Guid[]? DeviceTemplates
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
    /// List COSEM attribute template response.
    /// </summary>
    [DataContract]
    public class ListAttributeTemplatesResponse
    {

        /// <summary>
        /// List of COSEM attribute templates.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.ObjectTemplate),
                nameof(GXAttributeTemplate.ListItems))]
        public GXAttributeTemplate[]? AttributeTemplates
        {
            get;
            set;
        }
        /// <summary>
        /// Total count of the attribute templates.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove COSEM attribute templates.
    /// </summary>
    public class RemoveAttributeTemplate : IGXRequest<RemoveAttributeTemplateResponse>
    {
        /// <summary>
        /// Removed COSEM attribute templates identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
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
    /// Remove COSEM attribute template response.
    /// </summary>
    [DataContract]
    public class RemoveAttributeTemplateResponse
    {
    }

    /// <summary>
    /// Update data type of the attribute template.
    /// </summary>
    [DataContract]
    public class UpdateAttributeTemplateDataType : IGXRequest<UpdateAttributeTemplateDataTypeResponse>
    {
        /// <summary>
        /// Updated attribute templates.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXAttributeTemplate),
            nameof(GXAttributeTemplate.Id),
            nameof(GXAttributeTemplate.DataType),
            nameof(GXAttributeTemplate.UIDataType))]
        public GXAttributeTemplate[]? Items
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update data type of the attribute template reply.
    /// </summary>
    [DataContract]
    public class UpdateAttributeTemplateDataTypeResponse
    {
    }
}
