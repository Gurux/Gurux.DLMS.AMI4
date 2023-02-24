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
using Gurux.Common;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

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
        [IncludeSwagger(typeof(GXComponentViewGroup), nameof(GXComponentViewGroup.Id), nameof(GXComponentViewGroup.Name))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXComponentView Item
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
        /// Amount of the component views to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter component views.
        /// </summary>
        [ExcludeSwagger(typeof(GXComponentView),
            nameof(GXComponentView.ComponentViewGroups),
            nameof(GXComponentView.Icon))]
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
        public TargetType Select
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
        [ExcludeSwagger(typeof(GXComponentViewGroup),
            nameof(GXComponentViewGroup.ComponentViews),
            nameof(GXComponentViewGroup.UserGroups))]
        public GXComponentView[] ComponentViews
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
        [IncludeSwagger(typeof(GXComponentViewGroup),
            nameof(GXComponentViewGroup.Id))]
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
        public Guid[] ComponentViewIds
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
        public Guid[] Ids
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
