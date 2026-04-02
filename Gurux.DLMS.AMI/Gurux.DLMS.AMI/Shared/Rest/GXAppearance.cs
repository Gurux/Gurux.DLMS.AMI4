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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get appearance.
    /// </summary>
    public class GetAppearanceResponse
    {
        /// <summary>
        /// Appearance information.
        /// </summary>        
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                      nameof(GXUser.UserName))]
        public GXAppearance? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from appearances.
    /// </summary>
    [DataContract]
    public class ListAppearances : IGXRequest<ListAppearancesResponse>
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
        /// Amount of the appearances to retrieve.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter appearances.
        /// </summary>
        [ExcludeOpenApi(typeof(GXAppearance),
            nameof(GXAppearance.Description),
            nameof(GXAppearance.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXAppearance? Filter
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
        public string[]? Included
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
        public string[]? Exclude
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Appearance items reply.
    /// </summary>
    [DataContract]
    public class ListAppearancesResponse
    {
        /// <summary>
        /// List of appearance items.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXAppearance[]? Appearances
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the resourcers.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update appearances.
    /// </summary>
    [DataContract]
    public class UpdateAppearance : IGXRequest<UpdateAppearanceResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateAppearance()
        {
            Appearances = new List<GXAppearance>();
        }

        /// <summary>
        /// Appearances to update.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public List<GXAppearance> Appearances
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update appearances reply.
    /// </summary>
    [DataContract]
    public class UpdateAppearanceResponse
    {
        /// <summary>
        /// New appearance identifiers.
        /// </summary>
        [DataMember]
        public string[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete appearances.
    /// </summary>
    [DataContract]
    public class RemoveAppearance : IGXRequest<RemoveAppearanceResponse>
    {
        /// <summary>
        /// Removed appearance identifiers.
        /// </summary>
        [DataMember]
        public string[]? Ids
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
    /// Reply from Delete appearance.
    /// </summary>
    [DataContract]
    public class RemoveAppearanceResponse
    {
    }

    /// <summary>
    /// Refresh appearances.
    /// </summary>
    [DataContract]
    public class RefreshAppearance : IGXRequest<RefreshAppearanceResponse>
    {
        /// <summary>
        /// If true, all system appearances are reload.
        /// </summary>
        public bool Force
        {
            get;
            set;
        }

        /// <summary>
        /// Appearances to update.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXAppearance), nameof(GXAppearance.Id),
            nameof(GXAppearance.ResourceType), nameof(GXAppearance.Category))]
        public GXAppearance? Filter
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from refresh appearances.
    /// </summary>
    [DataContract]
    public class RefreshAppearanceResponse
    {
        /// <summary>
        /// True, if there are new appearances available.
        /// </summary>
        public bool NewItems
        {
            get;
            set;
        }
    }
}
