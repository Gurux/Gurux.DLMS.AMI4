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
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get content type group.
    /// </summary>
    public class GetContentTypeGroupResponse
    {
        /// <summary>
        /// ContentType group information.
        /// </summary>
        [IncludeOpenApi(typeof(GXContentType), nameof(GXContentType.Id),
                nameof(GXContentType.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXContentTypeGroup), nameof(GXContentTypeGroup.Roles))]
        public GXContentTypeGroup? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get content type group list.
    /// </summary>
    [DataContract]
    public class ListContentTypeGroups : IGXRequest<ListContentTypeGroupsResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the content type groups to retrieve.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter content type groups.
        /// </summary>
        [ExcludeOpenApi(typeof(GXContentTypeGroup), nameof(GXContentTypeGroup.ContentTypes),
            nameof(GXContentTypeGroup.UserGroups))]
        [ExcludeOpenApi(typeof(GXContentTypeGroup), nameof(GXContentTypeGroup.Roles))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXContentTypeGroup? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access groups from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all users are retreaved, not just current user. 
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
    }

    /// <summary>
    /// Get content type groups response.
    /// </summary>
    [DataContract]
    public class ListContentTypeGroupsResponse
    {
        /// <summary>
        /// List of content type groups.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXContentTypeGroup), nameof(GXContentTypeGroup.ContentTypes),
            nameof(GXContentTypeGroup.UserGroups))]
        [ExcludeOpenApi(typeof(GXContentTypeGroup), nameof(GXContentTypeGroup.Roles))]
        public GXContentTypeGroup[]? ContentTypeGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the content type groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new content type group.
    /// </summary>
    [DataContract]
    public class AddContentTypeGroup : IGXRequest<AddContentTypeGroupResponse>
    {
        /// <summary>
        /// New content type group(s).
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXContentType), nameof(GXContentType.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXContentTypeGroup),
            nameof(GXContentTypeGroup.Roles),
            nameof(GXContentTypeGroup.CreationTime),
            nameof(GXContentTypeGroup.Updated))]
        public GXContentTypeGroup[] ContentTypeGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new content type group response.
    /// </summary>
    [DataContract]
    public class AddContentTypeGroupResponse
    {
        /// <summary>
        /// New content type group IDs.
        /// </summary>
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove content type group.
    /// </summary>
    [DataContract]
    public class RemoveContentTypeGroup : IGXRequest<RemoveContentTypeGroupResponse>
    {
        /// <summary>
        /// ContentType group Ids to remove.
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
    /// Remove content type group response.
    /// </summary>
    [DataContract]
    public class RemoveContentTypeGroupResponse
    {
    }
}
