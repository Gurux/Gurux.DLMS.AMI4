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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Content;
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
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
    /// Get content.
    /// </summary>
    public class GetContentResponse
    {
        /// <summary>
        /// Device information.
        /// </summary>        
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXContentGroup), nameof(GXContentGroup.Id), nameof(GXContentGroup.Name))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXContentGroup), nameof(GXContentGroup.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [ExcludeOpenApi(typeof(GXContent),
            nameof(GXContent.Parent))]
        [IncludeOpenApi(typeof(GXModuleAssembly), nameof(GXModuleAssembly.Id))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        [ExcludeOpenApi(typeof(GXRole), nameof(GXRole.Scopes))]
        [ExcludeOpenApi(typeof(GXContentType), nameof(GXContentType.ContentTypeGroups))]
        [ExcludeOpenApi(typeof(GXContentTypeField), nameof(GXContentTypeField.Source),
            nameof(GXContentTypeField.Parent))]
        [ExcludeOpenApi(typeof(GXContentTypeFieldValue),
            nameof(GXContentTypeFieldValue.Parent))]
        [ExcludeOpenApi(typeof(GXLocalizedResource),
            nameof(GXLocalizedResource.Language))]
        [ExcludeOpenApi(typeof(GXMenuGroup),
            nameof(GXMenuGroup.Menus),
            nameof(GXMenuGroup.Creator))]
        [ExcludeOpenApi(typeof(GXMenu),
            nameof(GXMenu.MenuGroups),
            nameof(GXMenu.Parent))]
        [ExcludeOpenApi(typeof(GXMenuLink),
            nameof(GXMenuLink.Parent),
            nameof(GXMenuLink.Menu))]
        [ExcludeOpenApi(typeof(GXContentType),
            nameof(GXContentField.Parent))]
        [ExcludeOpenApi(typeof(GXContentField),
            nameof(GXContentField.Parent))]
        [ExcludeOpenApi(typeof(GXContentFieldValue),
            nameof(GXContentFieldValue.Parent))]
        [IncludeOpenApi(typeof(GXContentGroup),
            nameof(GXContentGroup.Id), nameof(GXContentGroup.Name))]
        public GXContent? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from contents.
    /// </summary>
    [DataContract]
    public class ListContents : IGXRequest<ListContentsResponse>
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
        /// Amount of the contents to retrieve.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter contents.
        /// </summary>
        [ExcludeOpenApi(typeof(GXContent),
            nameof(GXContent.ContentGroups),
            nameof(GXContent.User),
            nameof(GXContent.Fields),
            nameof(GXContent.Type),
            nameof(GXContent.Parent))]
        [IncludeOpenApi(typeof(GXUser),
            nameof(GXUser.Id),
            nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXContentTypeGroup),
            nameof(GXContentTypeGroup.Id),
            nameof(GXContentTypeGroup.Name))]
        [ExcludeOpenApi(typeof(GXContentTypeField),
            nameof(GXContentTypeField.Source),
            nameof(GXContentTypeField.Parent))]
        [ExcludeOpenApi(typeof(GXContentTypeFieldValue), nameof(GXContentTypeFieldValue.Parent))]
        [ExcludeOpenApi(typeof(GXContentType), nameof(GXContentType.Menu))]
        public GXContent? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access contents from all users.
        /// </summary>
        /// <remarks>
        /// If true, contents from all users are retreaved, not just current user. 
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
    /// Content items reply.
    /// </summary>
    [DataContract]
    public class ListContentsResponse
    {
        /// <summary>
        /// List of content items.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXContent), nameof(GXContent.ContentGroups),
            nameof(GXContent.User),
            nameof(GXContent.Parent))]
        [IncludeOpenApi(typeof(GXContentGroup), nameof(GXContentGroup.Id), nameof(GXContentGroup.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        public GXContent[]? Contents
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the contentrs.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update contents.
    /// </summary>
    [DataContract]
    public class UpdateContent : IGXRequest<UpdateContentResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateContent()
        {
            Contents = new List<GXContent>();
        }

        /// <summary>
        /// Contents to update.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXContentGroup), nameof(GXContentGroup.Id))]
        [ExcludeOpenApi(typeof(GXContent),
            nameof(GXContent.Parent),
            nameof(GXContent.User))]
        [IncludeOpenApi(typeof(GXContentType), nameof(GXContentType.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXContentField),
            nameof(GXContentField.Parent))]
        [ExcludeOpenApi(typeof(GXContentFieldValue),
            nameof(GXContentFieldValue.Parent))]
        [ExcludeOpenApi(typeof(GXContentTypeFieldValue),
            nameof(GXContentTypeFieldValue.Parent))]
        [ExcludeOpenApi(typeof(GXContentTypeField),
            nameof(GXContentTypeField.Parent))]
        public List<GXContent> Contents
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update contents reply.
    /// </summary>
    [DataContract]
    public class UpdateContentResponse
    {
        /// <summary>
        /// New content identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete contents.
    /// </summary>
    [DataContract]
    public class RemoveContent : IGXRequest<RemoveContentResponse>
    {
        /// <summary>
        /// Removed content identifiers.
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
    /// Reply from Delete content.
    /// </summary>
    [DataContract]
    public class RemoveContentResponse
    {
    }

    /// <summary>
    /// Close content.
    /// </summary>
    [DataContract]
    public class CloseContent : IGXRequest<CloseContentResponse>
    {
        /// <summary>
        /// Contents IDs to close.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Close contents response.
    /// </summary>
    [DataContract]
    public class CloseContentResponse
    {
    }
}
