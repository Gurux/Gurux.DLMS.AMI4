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
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get script group.
    /// </summary>
    public class GetScriptGroupResponse
    {
        /// <summary>
        /// Script group information.
        /// </summary>
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id), nameof(GXScript.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXScriptGroup? Item { get; set; }
    }

    /// <summary>
    /// Get script group list.
    /// </summary>
    [DataContract]
    public class ListScriptGroups : IGXRequest<ListScriptGroupsResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of script groups to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter script groups.
        /// </summary>
        [ExcludeOpenApi(typeof(GXScriptGroup),
           nameof(GXScriptGroup.Scripts),
           nameof(GXScriptGroup.UserGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXScriptGroup? Filter { get; set; }


        /// <summary>
        /// Admin user can access groups from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all users are retrieved, not just current user.
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
    }

    /// <summary>
    /// Get script groups response.
    /// </summary>
    [DataContract]
    public class ListScriptGroupsResponse
    {
        /// <summary>
        /// List of script groups.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXScriptGroup),
           nameof(GXScriptGroup.Scripts),
           nameof(GXScriptGroup.UserGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public IEnumerable<GXScriptGroup>? ScriptGroups { get; set; }

        /// <summary>
        /// Total number of matching script groups before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Add new script group.
    /// </summary>
    [DataContract]
    public class AddScriptGroup : IGXRequest<AddScriptGroupResponse>
    {
        /// <summary>
        /// New script group(s).
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXScriptGroup> ScriptGroups { get; set; } = default!;
    }

    /// <summary>
    /// Add new script group response.
    /// </summary>
    [DataContract]
    public class AddScriptGroupResponse
    {
        /// <summary>
        /// New script group IDs.
        /// </summary>
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Remove script group.
    /// </summary>
    [DataContract]
    public class RemoveScriptGroup : IGXRequest<RemoveScriptGroupResponse>
    {
        /// <summary>
        /// Script group Ids to remove.
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
    /// Remove script group response.
    /// </summary>
    [DataContract]
    public class RemoveScriptGroupResponse
    {
    }
}
