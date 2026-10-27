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
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.View;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested view information.</summary>
    [Description("Response containing the requested view information.")]
    public class GetViewResponse
    {
        /// <summary>
        /// Device information.
        /// </summary>        
        [Description("Device information.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [IncludeOpenApi(typeof(GXRole), nameof(GXRole.Id))]
        [ExcludeOpenApi(typeof(GXViewGroup), nameof(GXViewGroup.Views), nameof(GXViewGroup.UserGroups))]
        [ExcludeOpenApi(typeof(GXViewColumn), nameof(GXViewColumn.View))]
        public GXView? Item { get; set; }
    }

    /// <summary>Requests a list of views.</summary>
    [Description("Requests a list of views.")]
    [DataContract]
    public class ListViews : IGXRequest<ListViewsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of views to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of views to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter views.
        /// </summary>
        [Description("Optional criteria used to filter views.")]
        [ExcludeOpenApi(typeof(GXView),
            nameof(GXView.Columns),
            nameof(GXView.Creator))]
        [ExcludeOpenApi(typeof(GXView), nameof(GXView.ViewGroups))]
        [ExcludeOpenApi(typeof(GXView),
            nameof(GXView.Name), nameof(GXView.Title), nameof(GXView.Description),
            nameof(GXView.Policies), nameof(GXView.SelectionMode), nameof(GXView.AllowColumnCustomization),
            nameof(GXView.ShowAllData), nameof(GXView.PageSize), nameof(GXView.ConcurrencyStamp))]
        public GXView? Filter { get; set; }

        /// <summary>
        /// Admin user can access views from all users.
        /// </summary>
        /// <remarks>
        /// If true, views from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access views from all users.")]
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

    /// <summary>Response containing the requested views.</summary>
    [Description("Response containing the requested views.")]
    [DataContract]
    public class ListViewsResponse
    {
        /// <summary>
        /// List of view items.
        /// </summary>
        [Description("List of view items.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXView), nameof(GXView.Columns),
            nameof(GXView.Creator))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXView), nameof(GXView.ViewGroups))]
        public IEnumerable<GXView>? Views { get; set; }

        /// <summary>
        /// Total number of matching viewrs before paging.
        /// </summary>
        [Description("Total number of matching viewrs before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified views.</summary>
    [Description("Requests changes to the specified views.")]
    [DataContract]
    public class UpdateView : IGXRequest<UpdateViewResponse>
    {
        /// <summary>Initializes a new instance of UpdateView.</summary>
        [Description("Initializes a new instance of UpdateView.")]
        public UpdateView()
        {
            Views = [];
        }

        /// <summary>
        /// Views to update.
        /// </summary>
        [Description("Views to update.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXView),nameof(GXView.Creator))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id))]
        [IncludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Id))]
        [IncludeOpenApi(typeof(GXRole), nameof(GXRole.Id))]
        [IncludeOpenApi(typeof(GXViewGroup), nameof(GXViewGroup.Id))]
        [ExcludeOpenApi(typeof(GXViewColumn), nameof(GXViewColumn.View))]
        [ExcludeOpenApi(typeof(GXView), nameof(GXView.CreationTime), nameof(GXView.Updated))]
        [ExcludeOpenApi(typeof(GXViewColumn), nameof(GXViewColumn.CreationTime), nameof(GXViewColumn.Updated))]
        public List<GXView> Views { get; set; }
    }

    /// <summary>Response to updating the specified views.</summary>
    [Description("Response to updating the specified views.")]
    [DataContract]
    public class UpdateViewResponse
    {
        /// <summary>Identifiers of the added or updated view records.</summary>
        [Description("Identifiers of the added or updated view records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected views.</summary>
    [Description("Requests deletion of the selected views.")]
    [DataContract]
    public class RemoveView : IGXRequest<RemoveViewResponse>
    {
        /// <summary>Identifiers of the view records to delete.</summary>
        [Description("Identifiers of the view records to delete.")]
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

    /// <summary>Response to deleting the selected views.</summary>
    [Description("Response to deleting the selected views.")]
    [DataContract]
    public class RemoveViewResponse
    {
    }

    /// <summary>
    /// Regenerate view.
    /// </summary>
    [Description("Regenerate view.")]
    [DataContract]
    public class RegenerateView : IGXRequest<RegenerateViewResponse>
    {
        /// <summary>Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.</summary>
        [Description("Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate views response.
    /// </summary>
    [Description("Regenerate views response.")]
    [DataContract]
    public class RegenerateViewResponse
    {
    }
}
