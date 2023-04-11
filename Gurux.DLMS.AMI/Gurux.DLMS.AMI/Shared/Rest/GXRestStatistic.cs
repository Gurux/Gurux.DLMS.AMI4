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
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get statistic list.
    /// </summary>
    [DataContract]
    public class ListRestStatistics : IGXRequest<ListRestStatisticsResponse>
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
        /// Amount of the statistics to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter statistics.
        /// </summary>
        public GXRestStatistic? Filter
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
    /// Get statistics response.
    /// </summary>
    [DataContract]
    public class ListRestStatisticsResponse
    {
        /// <summary>
        /// List of statistics.
        /// </summary>
        [DataMember]
        public GXRestStatistic[] Statistics
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the statistics.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add statistics.
    /// </summary>
    [DataContract]
    public class AddRestStatistic : IGXRequest<AddRestStatisticResponse>
    {
        /// <summary>
        /// Statistics to add or update.
        /// </summary>
        [DataMember]
        public GXRestStatistic[] Statistics
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add or update statistics response.
    /// </summary>
    [DataContract]
    public class AddRestStatisticResponse
    {
        /// <summary>
        /// New statistics IDs.
        /// </summary>
        public GXRestStatistic[] Statistics
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove REST statistic.
    /// </summary>
    [DataContract]
    public class RemoveRestStatistic : IGXRequest<RemoveRestStatisticResponse>
    {
        /// <summary>
        /// Statistic Ids to remove.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove statistic response.
    /// </summary>
    [DataContract]
    public class RemoveRestStatisticResponse
    {
    }

    /// <summary>
    /// Clear REST statistics.
    /// </summary>
    [DataContract]
    public class ClearRestStatistic : IGXRequest<ClearRestStatisticResponse>
    {
    }

    /// <summary>
    /// Clear REST statistic response.
    /// </summary>
    [DataContract]
    public class ClearRestStatisticResponse
    {
    }
}
