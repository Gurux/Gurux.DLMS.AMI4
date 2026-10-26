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
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.ObjectTemplate),
                nameof(GXAttributeTemplate.ListItems))]
        public GXAttributeTemplate? Item { get; set; }
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
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.ListItems))]
        public IEnumerable<GXAttributeTemplate>? AttributeTemplates { get; set; }
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
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Get attribute template.
    /// </summary>
    [DataContract]
    public class ListAttributeTemplates : IGXRequest<ListAttributeTemplatesResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        [DataMember]
        public UInt64 Index { get; set; }

        /// <summary>
        /// Maximum number of items to retrieve; zero disables paging.
        /// </summary>
        [DataMember]
        public UInt64 Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter attribute templates.
        /// </summary>
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
               nameof(GXAttributeTemplate.ObjectTemplate),
               nameof(GXAttributeTemplate.ListItems))]
        public GXAttributeTemplate? Filter { get; set; }

        /// <summary>
        /// Admin user can access attribute templates from all users.
        /// </summary>
        /// <remarks>
        /// If true, attribute templates from all users are retrieved, not just current user.
        /// </remarks>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }

        /// <summary>
        /// Device template Ids.
        /// </summary>
        public IEnumerable<Guid>? DeviceTemplates { get; set; }

        /// <summary>
        /// Filtered object types.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public IEnumerable<int>? ObjectTypes { get; set; }

        /// <summary>
        /// Ignored object types.
        /// </summary>
        public IEnumerable<int>? IgnoredObjectTypes { get; set; }
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
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.ObjectTemplate),
                nameof(GXAttributeTemplate.ListItems))]
        public IEnumerable<GXAttributeTemplate>? AttributeTemplates { get; set; }
        /// <summary>
        /// Total number of matching attribute templates before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Remove COSEM attribute templates.
    /// </summary>
    public class RemoveAttributeTemplate : IGXRequest<RemoveAttributeTemplateResponse>
    {
        /// <summary>
        /// COSEM attribute templates identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>
        /// Whether to permanently delete the items from the database.
        /// </summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete { get; set; }
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
        [IncludeOpenApi(typeof(GXAttributeTemplate),
            nameof(GXAttributeTemplate.Id),
            nameof(GXAttributeTemplate.DataType),
            nameof(GXAttributeTemplate.UIDataType))]
        public IEnumerable<GXAttributeTemplate>? Items { get; set; }
    }

    /// <summary>
    /// Update data type of the attribute template reply.
    /// </summary>
    [DataContract]
    public class UpdateAttributeTemplateDataTypeResponse
    {
    }
}
