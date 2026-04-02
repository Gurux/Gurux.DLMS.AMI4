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
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get component view.
    /// </summary>
    public class GetComponentViewResponse
    {
        /// <summary>
        /// Component view information.
        /// </summary>
        [IncludeOpenApi(typeof(GXComponentViewGroup),
            nameof(GXComponentViewGroup.Id), nameof(GXComponentViewGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXComponentView? Item
        {
            get;
            set;
        }
    }
    /// <summary>
    /// Get list from component views.
    /// </summary>
    [DataContract]
    public class ListComponentViews : IGXRequest<ListComponentViewsResponse>
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
        /// Amount of the component views to retrieve.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter component views.
        /// </summary>
        [ExcludeOpenApi(typeof(GXComponentView),
            nameof(GXComponentView.ComponentViewGroups),
            nameof(GXComponentView.Icon))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXComponentView? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access component views from all users.
        /// </summary>
        /// <remarks>
        /// If true, component views from all users are retreaved, not just current user. 
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
    /// List ComponentView items reply.
    /// </summary>
    [DataContract]
    public class ListComponentViewsResponse
    {
        /// <summary>
        /// List of component view items.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXComponentViewGroup),
            nameof(GXComponentViewGroup.ComponentViews),
            nameof(GXComponentViewGroup.UserGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        public GXComponentView[]? ComponentViews
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the component viewrs.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update component views.
    /// </summary>
    [DataContract]
    public class UpdateComponentView : IGXRequest<UpdateComponentViewResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateComponentView()
        {
            ComponentViews = new List<GXComponentView>();
        }

        /// <summary>
        /// ComponentViews to update.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXComponentViewGroup),
            nameof(GXComponentViewGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public List<GXComponentView> ComponentViews
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update component views reply.
    /// </summary>
    [DataContract]
    public class UpdateComponentViewResponse
    {
        /// <summary>
        /// New component view identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? ComponentViewIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete component views.
    /// </summary>
    [DataContract]
    public class RemoveComponentView : IGXRequest<RemoveComponentViewResponse>
    {
        /// <summary>
        /// Removed component view identifiers.
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
    /// Reply from Delete component view.
    /// </summary>
    [DataContract]
    public class RemoveComponentViewResponse
    {
    }


    /// <summary>
    /// Refresh component views.
    /// </summary>
    [DataContract]
    public class RefreshComponentView : IGXRequest<RefreshComponentViewResponse>
    {
    }

    /// <summary>
    /// Reply from Refresh component views.
    /// </summary>
    [DataContract]
    public class RefreshComponentViewResponse
    {
        /// <summary>
        /// True, if there are new component views avialable.
        /// </summary>
        public bool NewItems
        {
            get;
            set;
        }
    }
}
