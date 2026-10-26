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
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get log type.
    /// </summary>
    public class GetLogTypeResponse
    {
        /// <summary>
        /// Log type information.
        /// </summary>
        [IncludeOpenApi(typeof(GXLog), nameof(GXLog.PublicId),
                nameof(GXLog.Message))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXLogType? Item { get; set; }
    }

    /// <summary>
    /// Get log type list.
    /// </summary>
    [DataContract]
    public class ListLogTypes : IGXRequest<ListLogTypesResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of log types to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter log types.
        /// </summary>
        [ExcludeOpenApi(typeof(GXLogType), nameof(GXLogType.Logs))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXLogType? Filter { get; set; }

        /// <summary>
        /// Admin user can access types from all users.
        /// </summary>
        /// <remarks>
        /// If true, types from all users are retrieved, not just current user.
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
        /// Included public GUID identifiers (GXLogType.PublicId).
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Excluded public GUID identifiers (GXLogType.PublicId).
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// Get log types response.
    /// </summary>
    [DataContract]
    public class ListLogTypesResponse
    {
        /// <summary>
        /// List of log types.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXLogType), nameof(GXLogType.Logs))]
        public IEnumerable<GXLogType>? LogTypes { get; set; }

        /// <summary>
        /// Total number of matching log types before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Add new log type.
    /// </summary>
    [DataContract]
    public class AddLogType : IGXRequest<AddLogTypeResponse>
    {
        /// <summary>
        /// New log type(s).
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXLog), nameof(GXLog.PublicId))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeOpenApi(typeof(GXLogType), nameof(GXLogType.Id), nameof(GXLogType.CreationTime), nameof(GXLogType.Updated))]
        public IEnumerable<GXLogType> LogTypes { get; set; } = default!;
    }

    /// <summary>
    /// Add new log type response.
    /// </summary>
    [DataContract]
    public class AddLogTypeResponse
    {
        /// <summary>
        /// Log type public GUID identifiers.
        /// </summary>
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Remove log type.
    /// </summary>
    [DataContract]
    public class RemoveLogType : IGXRequest<RemoveLogTypeResponse>
    {
        /// <summary>
        /// Log type public GUID identifiers to remove.
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
    /// Remove log type response.
    /// </summary>
    [DataContract]
    public class RemoveLogTypeResponse
    {
    }
}
