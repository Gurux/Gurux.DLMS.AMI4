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

using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle report groups.
    /// </summary>
    public interface IReportGroupRepository
    {
        /// <summary>
        /// List report groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXReportGroup[]> ListAsync(
            ListReportGroups? request,
            ListReportGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read report group information.
        /// </summary>
        /// <param name="id">Report group id.</param>
        /// <returns></returns>
        Task<GXReportGroup> ReadAsync(Guid id);

        /// <summary>
        /// Update report groups.
        /// </summary>
        /// <param name="groups">Updated report groups.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            IEnumerable<GXReportGroup> groups,
            Expression<Func<GXReportGroup, object?>>? columns = null);

        /// <summary>
        /// Delete report group(s).
        /// </summary>
        /// <param name="groups">Report groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete);

        /// <summary>
        /// Returns report groups list where report belongs.
        /// </summary>
        /// <param name="reportId">Report ID</param>
        /// <returns>List of report groups.</returns>
        Task<List<GXReportGroup>> GetJoinedReportGroups(Guid reportId);

        /// <summary>
        /// Get all users that can access this report group.
        /// </summary>
        /// <param name="reportGroupId">Report group id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(Guid? reportGroupId);

        /// <summary>
        /// Get all users that can access report group.
        /// </summary>
        /// <param name="reportGroupIds">Agent Report ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? reportGroupIds);
    }
}
