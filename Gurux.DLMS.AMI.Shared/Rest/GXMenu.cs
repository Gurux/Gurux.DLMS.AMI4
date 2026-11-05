// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using MenuKind = Gurux.DLMS.AMI.Shared.DTOs.Enums.MenuKind;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested menu information.</summary>
    [Description("Response containing the requested menu information.")]
    public class GetMenuResponse
    {
        /// <summary>
        /// Device information.
        /// </summary>        
        [Description("Device information.")]
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

    /// <summary>Requests a list of menus.</summary>
    [Description("Requests a list of menus.")]
    [DataContract]
    public class ListMenus : IGXRequest<ListMenusResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of menus to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of menus to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter menus.
        /// </summary>
        [Description("Optional criteria used to filter menus.")]
        [ExcludeOpenApi(typeof(GXMenu),
            nameof(GXMenu.Links),
            nameof(GXMenu.MenuGroups),
            nameof(GXMenu.Creator),
            nameof(GXMenu.Parent))]
        [ExcludeOpenApi(typeof(GXMenu),
            nameof(GXMenu.Order), nameof(GXMenu.Visibility), nameof(GXMenu.Icon),
            nameof(GXMenu.Title), nameof(GXMenu.Description), nameof(GXMenu.Policies),
            nameof(GXMenu.PublishTime), nameof(GXMenu.ConcurrencyStamp))]
        public GXMenu? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>
        /// Admin user can access menus from all users.
        /// </summary>
        /// <remarks>
        /// If true, menus from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access menus from all users.")]
        public bool AllUsers { get; set; }

        /// <summary>Additional related information to include in the response. Reserved for future use.</summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [Description("Additional related information to include in the response. Reserved for future use.")]
        public IEnumerable<string>? Select { get; set; }

        /// <summary>Name of the property to sort by. If omitted, the endpoint uses its default ordering.</summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        [Description("Name of the property to sort by. If omitted, the endpoint uses its default ordering.")]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        [Description("Whether to sort results in descending order.")]
        public bool Descending { get; set; }

        /// <summary>Identifiers to include in the results. A nonempty collection restricts results to these identifiers.</summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Identifiers to include in the results. A nonempty collection restricts results to these identifiers.")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested menus.</summary>
    [Description("Response containing the requested menus.")]
    [DataContract]
    public class ListMenusResponse
    {
        /// <summary>
        /// List of menu items.
        /// </summary>
        [Description("List of menu items.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXMenu), nameof(GXMenu.MenuGroups),
            nameof(GXMenu.Creator),
            nameof(GXMenu.Parent))]
        [IncludeOpenApi(typeof(GXMenuGroup), nameof(GXMenuGroup.Id), nameof(GXMenuGroup.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXMenu), nameof(GXMenu.Links))]
        public IEnumerable<GXMenu>? Menus { get; set; }

        /// <summary>
        /// Total number of matching menurs before paging.
        /// </summary>
        [Description("Total number of matching menurs before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified menus.</summary>
    [Description("Requests changes to the specified menus.")]
    [DataContract]
    public class UpdateMenu : IGXRequest<UpdateMenuResponse>
    {
        /// <summary>Initializes a new instance of UpdateMenu.</summary>
        [Description("Initializes a new instance of UpdateMenu.")]
        public UpdateMenu()
        {
            Menus = [];
        }

        /// <summary>
        /// Menus to update.
        /// </summary>
        [Description("Menus to update.")]
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
        [ExcludeOpenApi(typeof(GXConfiguration), nameof(GXConfiguration.CreationTime), nameof(GXConfiguration.Updated))]
        [ExcludeOpenApi(typeof(GXMenuLink), nameof(GXMenuLink.CreationTime), nameof(GXMenuLink.Updated))]
        [ExcludeOpenApi(typeof(GXMenu), nameof(GXMenu.CreationTime), nameof(GXMenu.Updated))]
        public List<GXMenu> Menus { get; set; }
    }

    /// <summary>Response to updating the specified menus.</summary>
    [Description("Response to updating the specified menus.")]
    [DataContract]
    public class UpdateMenuResponse
    {
        /// <summary>Identifiers of the added or updated menu records.</summary>
        [Description("Identifiers of the added or updated menu records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected menus.</summary>
    [Description("Requests deletion of the selected menus.")]
    [DataContract]
    public class RemoveMenu : IGXRequest<RemoveMenuResponse>
    {
        /// <summary>Identifiers of the menu records to delete.</summary>
        [Description("Identifiers of the menu records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected menus.</summary>
    [Description("Response to deleting the selected menus.")]
    [DataContract]
    public class RemoveMenuResponse
    {
    }

    /// <summary>
    /// Regenerate menu.
    /// </summary>
    [Description("Regenerate menu.")]
    [DataContract]
    public class RegenerateMenu : IGXRequest<RegenerateMenuResponse>
    {
        /// <summary>
        /// Menu kind to regenerate.
        /// </summary>
        [Description("Menu kind to regenerate.")]
        [DataMember]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public MenuKind Kind { get; set; } = MenuKind.All;

        /// <summary>Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.</summary>
        [Description("Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate menus response.
    /// </summary>
    [Description("Regenerate menus response.")]
    [DataContract]
    public class RegenerateMenuResponse
    {
    }
}
