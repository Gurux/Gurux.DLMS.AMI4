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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get menu.
    /// </summary>
    public class GetMenuResponse
    {
        /// <summary>
        /// Device information.
        /// </summary>        
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXMenuGroup), nameof(GXMenuGroup.Id), nameof(GXMenuGroup.Name))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXMenuGroup), nameof(GXMenuGroup.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [ExcludeOpenApi(typeof(GXMenu),
            nameof(GXMenu.Parent))]
        [ExcludeOpenApi(typeof(GXMenuLink),
            nameof(GXMenuLink.Parent),
            nameof(GXMenuLink.Menu))]
        [IncludeOpenApi(typeof(GXRole), nameof(GXRole.Id))]
        public GXMenu? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from menus.
    /// </summary>
    [DataContract]
    public class ListMenus : IGXRequest<ListMenusResponse>
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
        /// Amount of the menus to retrieve.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter menus.
        /// </summary>
        [ExcludeOpenApi(typeof(GXMenu),
            nameof(GXMenu.Roles),
            nameof(GXMenu.Links),
            nameof(GXMenu.MenuGroups),
            nameof(GXMenu.Creator),
            nameof(GXMenu.Parent))]
        public GXMenu? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access menus from all users.
        /// </summary>
        /// <remarks>
        /// If true, menus from all users are retreaved, not just current user. 
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
    /// Menu items reply.
    /// </summary>
    [DataContract]
    public class ListMenusResponse
    {
        /// <summary>
        /// List of menu items.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXMenu), nameof(GXMenu.MenuGroups),
            nameof(GXMenu.Creator),
            nameof(GXMenu.Parent))]
        [IncludeOpenApi(typeof(GXMenuGroup), nameof(GXMenuGroup.Id), nameof(GXMenuGroup.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        public GXMenu[]? Menus
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the menurs.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update menus.
    /// </summary>
    [DataContract]
    public class UpdateMenu : IGXRequest<UpdateMenuResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateMenu()
        {
            Menus = new List<GXMenu>();
        }

        /// <summary>
        /// Menus to update.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXMenuGroup), nameof(GXMenuGroup.Id))]
        [ExcludeOpenApi(typeof(GXMenu),
            nameof(GXMenu.Parent),
            nameof(GXMenu.Creator))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXMenuLink),
            nameof(GXMenuLink.Menu),
            nameof(GXMenuLink.Parent))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id))]
        [IncludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Id))]
        [IncludeOpenApi(typeof(GXRole), nameof(GXRole.Id))]
        public List<GXMenu> Menus
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update menus reply.
    /// </summary>
    [DataContract]
    public class UpdateMenuResponse
    {
        /// <summary>
        /// New menu identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete menus.
    /// </summary>
    [DataContract]
    public class RemoveMenu : IGXRequest<RemoveMenuResponse>
    {
        /// <summary>
        /// Removed menu identifiers.
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
    /// Reply from Delete menu.
    /// </summary>
    [DataContract]
    public class RemoveMenuResponse
    {
    }

    /// <summary>
    /// Close menu.
    /// </summary>
    [DataContract]
    public class CloseMenu : IGXRequest<CloseMenuResponse>
    {
        /// <summary>
        /// Menus IDs to close.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Close menus response.
    /// </summary>
    [DataContract]
    public class CloseMenuResponse
    {
    }
}
