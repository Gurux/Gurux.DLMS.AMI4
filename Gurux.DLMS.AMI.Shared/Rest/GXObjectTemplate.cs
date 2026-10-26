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
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get object template.
    /// </summary>
    public class GetObjectTemplateResponse
    {
        /// <summary>
        /// Object information.
        /// </summary>        
        [IncludeOpenApi(typeof(GXUser),
        nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXDeviceModel),
            nameof(GXDeviceModel.Manufacturer))]
        [ExcludeOpenApi(typeof(GXDeviceVersion),
            nameof(GXDeviceVersion.Model))]
        [ExcludeOpenApi(typeof(GXDeviceSettings),
            nameof(GXDeviceSettings.Version))]
        [IncludeOpenApi(typeof(GXDeviceTemplate),
            nameof(GXDeviceTemplate.Id))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
            nameof(GXAttributeTemplate.ObjectTemplate))]
        [ExcludeOpenApi(typeof(GXAttributeListItem),
            nameof(GXAttributeListItem.Template))]
        public GXObjectTemplate? Item { get; set; }
    }


    /// <summary>
    /// Add COSEM object template.
    /// </summary>
    [DataContract]
    public class UpdateObjectTemplate : IGXRequest<UpdateObjectTemplateResponse>
    {
        /// <summary>
        /// Added COSEM object templates.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXDeviceTemplate),
            nameof(GXDeviceTemplate.Id))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
            nameof(GXAttributeTemplate.ObjectTemplate))]
        [ExcludeOpenApi(typeof(GXAttributeListItem),
            nameof(GXAttributeListItem.Template))]
        public IEnumerable<GXObjectTemplate>? ObjectTemplates { get; set; }
    }

    /// <summary>
    /// Identifiers of the added or updated COSEM object templates.
    /// </summary>
    [DataContract]
    public class UpdateObjectTemplateResponse
    {
        /// <summary>
        /// Object template identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }


    /// <summary>
    /// Get object templates.
    /// </summary>
    [DataContract]
    public class ListObjectTemplates : IGXRequest<ListObjectTemplatesResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public UInt64 Index { get; set; }

        /// <summary>
        /// Maximum number of COSEM object templates to retrieve; zero disables paging.
        /// </summary>
        [DataMember]
        public UInt64 Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter object templates.
        /// </summary>
        [ExcludeOpenApi(typeof(GXObjectTemplate),
                    nameof(GXObjectTemplate.Attributes),
            nameof(GXObjectTemplate.DeviceTemplate))]
        public GXObjectTemplate? Filter { get; set; }

        /// <summary>
        /// Admin user can access COSEM object templates from all users.
        /// </summary>
        /// <remarks>
        /// If true, COSEM object templates from all users are retrieved, not just current user.
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
    /// List COSEM object template response.
    /// </summary>
    [DataContract]
    public class ListObjectTemplatesResponse
    {

        /// <summary>
        /// List of COSEM object templates.
        /// </summary>
        [DataMember]
        public IEnumerable<GXObjectTemplate>? ObjectTemplates { get; set; }
        /// <summary>
        /// Total number of matching object templates before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Delete COSEM object templates.
    /// </summary>
    public class RemoveObjectTemplate : IGXRequest<RemoveObjectTemplateResponse>
    {
        /// <summary>
        /// COSEM object templates identifiers.
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
    /// Delete COSEM object template response.
    /// </summary>
    [DataContract]
    public class RemoveObjectTemplateResponse
    {
    }
}
