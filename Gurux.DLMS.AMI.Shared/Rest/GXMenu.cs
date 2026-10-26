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

using MenuKind = Gurux.DLMS.AMI.Shared.DTOs.Enums.MenuKind;
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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

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
        public GXMenu? Item { get; set; }
    }

    /// <summary>
    /// Get list from menus.
    /// </summary>
    [DataContract]
    public class ListMenus : IGXRequest<ListMenusResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of menus to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter menus.
        /// </summary>
        [ExcludeOpenApi(typeof(GXMenu),
            nameof(GXMenu.Links),
            nameof(GXMenu.MenuGroups),
            nameof(GXMenu.Creator),
            nameof(GXMenu.Parent))]
        public GXMenu? Filter { get; set; }

        /// <summary>
        /// Admin user can access menus from all users.
        /// </summary>
        /// <remarks>
        /// If true, menus from all users are retrieved, not just current user.
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
        public IEnumerable<GXMenu>? Menus { get; set; }

        /// <summary>
        /// Total number of matching menurs before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
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
        public List<GXMenu> Menus { get; set; }
    }

    /// <summary>
    /// Update menus reply.
    /// </summary>
    [DataContract]
    public class UpdateMenuResponse
    {
        /// <summary>
        /// Menu identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Delete menus.
    /// </summary>
    [DataContract]
    public class RemoveMenu : IGXRequest<RemoveMenuResponse>
    {
        /// <summary>
        /// Menu identifiers.
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
    /// Reply from Delete menu.
    /// </summary>
    [DataContract]
    public class RemoveMenuResponse
    {
    }

    /// <summary>
    /// Regenerate menu.
    /// </summary>
    [DataContract]
    public class RegenerateMenu : IGXRequest<RegenerateMenuResponse>
    {
        /// <summary>
        /// Menu kind to regenerate.
        /// </summary>
        [DataMember]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public MenuKind Kind { get; set; } = MenuKind.All;

        /// <summary>
        /// Menus IDs to regenerate.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate menus response.
    /// </summary>
    [DataContract]
    public class RegenerateMenuResponse
    {
    }
}
