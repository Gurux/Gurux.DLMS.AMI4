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
        /// Start index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Amount of the views to retrieve.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Filter can be used to filter views.
        /// </summary>
        [ExcludeOpenApi(typeof(GXView),
            nameof(GXView.Columns),
            nameof(GXView.Creator))]
        public GXView? Filter { get; set; }

        /// <summary>
        /// Admin user can access views from all users.
        /// </summary>
        /// <remarks>
        /// If true, views from all users are retreaved, not just current user. 
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
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
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
        /// Total count of the viewrs.
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
        /// New view identifiers.
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
        /// Removed view identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
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
