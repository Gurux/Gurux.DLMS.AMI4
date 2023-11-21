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
    /// Get performance list.
    /// </summary>
    [DataContract]
    public class ListPerformances : IGXRequest<ListPerformancesResponse>
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
        /// Amount of the performances to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter performances.
        /// </summary>
        public GXPerformance? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access performances from all users.
        /// </summary>
        /// <remarks>
        /// If true, performances from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
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
    }

    /// <summary>
    /// Performance items reply.
    /// </summary>
    [DataContract]
    public class ListPerformancesResponse
    {
        /// <summary>
        /// Performances.
        /// </summary>
        public GXPerformance[]? Performances
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the performancers.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add performances.
    /// </summary>
    [DataContract]
    public class AddPerformance : IGXRequest<AddPerformanceResponse>
    {
        /// <summary>
        /// Added performances.
        /// </summary>
        public List<GXPerformance>? Performances
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add performances reply.
    /// </summary>
    [DataContract]
    public class AddPerformanceResponse
    {
        /// <summary>
        /// New performance identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete performances.
    /// </summary>
    [DataContract]
    public class RemovePerformance : IGXRequest<RemovePerformanceResponse>
    {
        /// <summary>
        /// Removed performance identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from Delete performance.
    /// </summary>
    [DataContract]
    public class RemovePerformanceResponse
    {
    }

    /// <summary>
    /// Clear performance.
    /// </summary>
    [DataContract]
    public class ClearPerformance : IGXRequest<ClearRestStatisticResponse>
    {
    }

    /// <summary>
    /// Clear performance response.
    /// </summary>
    [DataContract]
    public class ClearPerformanceResponse
    {
    }
}
