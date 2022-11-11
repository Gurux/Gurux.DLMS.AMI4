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

namespace Gurux.DLMS.AMI.Shared.Rest
{
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
    public class DeleteComponentView : IGXRequest<DeleteComponentViewResponse>
    {
        /// <summary>
        /// Removed component view identifiers.
        /// </summary>
        [DataMember]
        public Guid[] ComponentViewIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from Delete component view.
    /// </summary>
    [DataContract]
    public class DeleteComponentViewResponse
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
