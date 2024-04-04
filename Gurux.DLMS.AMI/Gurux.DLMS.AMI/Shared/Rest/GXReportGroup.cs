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
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get report group.
    /// </summary>
    public class GetReportGroupResponse
    {
        /// <summary>
        /// Report group information.
        /// </summary>
        [IncludeSwagger(typeof(GXReport), nameof(GXReport.Id),
                nameof(GXReport.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        public GXReportGroup? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get report group list.
    /// </summary>
    [DataContract]
    public class ListReportGroups : IGXRequest<ListReportGroupsResponse>
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
        /// Amount of the report groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter report groups.
        /// </summary>
        [ExcludeSwagger(typeof(GXReportGroup), nameof(GXReportGroup.Reports),
            nameof(GXReportGroup.UserGroups))]
        public GXReportGroup? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access groups from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all users are retreaved, not just current user. 
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
    /// Get report groups response.
    /// </summary>
    [DataContract]
    public class ListReportGroupsResponse
    {
        /// <summary>
        /// List of report groups.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXReportGroup), nameof(GXReportGroup.Reports),
            nameof(GXReportGroup.UserGroups))]
        [ExcludeSwagger(typeof(GXReportGroup))]
        public GXReportGroup[]? ReportGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the report groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new report group.
    /// </summary>
    [DataContract]
    public class AddReportGroup : IGXRequest<AddReportGroupResponse>
    {
        /// <summary>
        /// New report group(s).
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXReport), nameof(GXReport.Id))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeSwagger(typeof(GXReportGroup),
            nameof(GXReportGroup.CreationTime),
            nameof(GXReportGroup.Updated))]
        public GXReportGroup[]? ReportGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new report group response.
    /// </summary>
    [DataContract]
    public class AddReportGroupResponse
    {
        /// <summary>
        /// New report group IDs.
        /// </summary>
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove report group.
    /// </summary>
    [DataContract]
    public class RemoveReportGroup : IGXRequest<RemoveReportGroupResponse>
    {
        /// <summary>
        /// Report group Ids to remove.
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
    /// Remove report group response.
    /// </summary>
    [DataContract]
    public class RemoveReportGroupResponse
    {
    }
}
