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

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get view.
    /// </summary>
    public class GetViewResponse
    {
        /// <summary>
        /// Device information.
        /// </summary>        
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [IncludeOpenApi(typeof(GXRole), nameof(GXRole.Id))]
        public GXView? Item { get; set; }
    }

    /// <summary>
    /// Get list from views.
    /// </summary>
    [DataContract]
    public class ListViews : IGXRequest<ListViewsResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of views to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter views.
        /// </summary>
        [ExcludeOpenApi(typeof(GXView),
            nameof(GXView.Columns),
            nameof(GXView.Creator))]
        public GXView? Filter { get; set; }

        /// <summary>
        /// Admin user can access views from all users.
        /// </summary>
        /// <remarks>
        /// If true, views from all users are retrieved, not just current user.
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
    /// View items reply.
    /// </summary>
    [DataContract]
    public class ListViewsResponse
    {
        /// <summary>
        /// List of view items.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXView), nameof(GXView.Columns),
            nameof(GXView.Creator))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        public IEnumerable<GXView>? Views { get; set; }

        /// <summary>
        /// Total number of matching viewrs before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Update views.
    /// </summary>
    [DataContract]
    public class UpdateView : IGXRequest<UpdateViewResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateView()
        {
            Views = new List<GXView>();
        }

        /// <summary>
        /// Views to update.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXView),nameof(GXView.Creator))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id))]
        [IncludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Id))]
        [IncludeOpenApi(typeof(GXRole), nameof(GXRole.Id))]
        public List<GXView> Views { get; set; }
    }

    /// <summary>
    /// Update views reply.
    /// </summary>
    [DataContract]
    public class UpdateViewResponse
    {
        /// <summary>
        /// View identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Delete views.
    /// </summary>
    [DataContract]
    public class RemoveView : IGXRequest<RemoveViewResponse>
    {
        /// <summary>
        /// View identifiers.
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
    /// Reply from Delete view.
    /// </summary>
    [DataContract]
    public class RemoveViewResponse
    {
    }

    /// <summary>
    /// Regenerate view.
    /// </summary>
    [DataContract]
    public class RegenerateView : IGXRequest<RegenerateViewResponse>
    {
        /// <summary>
        /// Views IDs to regenerate.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate views response.
    /// </summary>
    [DataContract]
    public class RegenerateViewResponse
    {
    }
}
