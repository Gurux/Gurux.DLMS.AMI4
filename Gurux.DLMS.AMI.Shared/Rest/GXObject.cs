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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get object.
    /// </summary>
    public class GetObjectResponse
    {
        /// <summary>
        /// Object information.
        /// </summary>        
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id), nameof(GXAttribute.Template))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [ExcludeOpenApi(typeof(GXObject), nameof(GXObject.Tasks), nameof(GXObject.Errors))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [ExcludeOpenApi(typeof(GXObjectParameter),
            nameof(GXObjectParameter.Object), nameof(GXObjectParameter.Module))]
        public GXObject? Item { get; set; }
    }

    /// <summary>
    /// Add COSEM object.
    /// </summary>
    [DataContract]
    public class UpdateObject : IGXRequest<UpdateObjectResponse>
    {
        /// <summary>
        /// Added COSEM objects.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXObject),
            nameof(GXObject.Template),
            nameof(GXObject.Device),
            nameof(GXObject.Tasks),
            nameof(GXObject.Errors))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [ExcludeOpenApi(typeof(GXObjectParameter),
            nameof(GXObjectParameter.Object),
            nameof(GXObjectParameter.Module))]
        public IEnumerable<GXObject>? Objects { get; set; }
    }

    /// <summary>
    /// Identifiers of the added or updated COSEM objects.
    /// </summary>
    [DataContract]
    public class UpdateObjectResponse
    {
        /// <summary>
        /// Object identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }


    /// <summary>
    /// Get object.
    /// </summary>
    [DataContract]
    public class ListObjects : IGXRequest<ListObjectsResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        [DataMember]
        public UInt64 Index { get; set; }

        /// <summary>
        /// Maximum number of COSEM objects to retrieve; zero disables paging.
        /// </summary>
        [DataMember]
        public UInt64 Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter objects.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXObject), nameof(GXObject.Tasks),
            nameof(GXObject.Template),
            nameof(GXObject.Errors), nameof(GXObject.Device),
            nameof(GXObject.Attributes), nameof(GXObject.Parameters))]
        public GXObject? Filter { get; set; }

        /// <summary>
        /// Admin user can access COSEM objects from all users.
        /// </summary>
        /// <remarks>
        /// If true, COSEM objects from all users are retrieved, not just current user.
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
        /// Device Ids.
        /// </summary>
        public IEnumerable<Guid>? Devices { get; set; }

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
    /// List COSEM object response.
    /// </summary>
    [DataContract]
    public class ListObjectsResponse
    {

        /// <summary>
        /// List of COSEM objects.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXObject), nameof(GXObject.Tasks), nameof(GXObject.Errors), nameof(GXObject.Device), nameof(GXObject.Attributes), nameof(GXObject.Parameters))]
        public IEnumerable<GXObject>? Objects { get; set; }
        /// <summary>
        /// Total number of matching objects before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Delete COSEM objects.
    /// </summary>
    public class RemoveObject : IGXRequest<RemoveObjectResponse>
    {
        /// <summary>
        /// COSEM objects identifiers.
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
    /// Delete COSEM object response.
    /// </summary>
    [DataContract]
    public class RemoveObjectResponse
    {
    }
}
