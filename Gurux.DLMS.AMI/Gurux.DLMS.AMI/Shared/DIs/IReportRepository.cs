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
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle reports.
    /// </summary>
    public interface IReportRepository
    {
        /// <summary>
        /// List reports.
        /// </summary>
        /// <returns>Reports.</returns>
        Task<GXReport[]> ListAsync(
            ClaimsPrincipal user,
            ListReports? request,
            ListReportsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read report.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Report id.</param>
        /// <returns></returns>
        Task<GXReport> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update report(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="reports">Updated report(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXReport> reports,
            Expression<Func<GXReport, object?>>? columns = null);

        /// <summary>
        /// Delete report(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="reports">Report(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(
            ClaimsPrincipal user,
            IEnumerable<Guid> reports,
            bool delete);

        /// <summary>
        /// Get all users that can access this report.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="reportId">Report id.</param>
        /// <returns>Users.</returns>
        Task<List<string>> GetUsersAsync(
            ClaimsPrincipal User,
            Guid? reportId);

        /// <summary>
        /// Get all users that can access reports.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="reportIds">Report ids.</param>
        /// <returns>Users.</returns>
        Task<List<string>> GetUsersAsync(
            ClaimsPrincipal User,
            IEnumerable<Guid>? reportIds);

        /// <summary>
        /// Send report(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="reports">Generated Reports.</param>
        /// <returns>Generated reports.</returns>
        Task<IEnumerable<GXReport>> SendAsync(
            ClaimsPrincipal user,
            IEnumerable<GXReport> reports);

        /// <summary>
        /// Cancel report(s) sending.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="reports">Canceled reports.</param>
        Task CancelAsync(
            ClaimsPrincipal user,
            IEnumerable<Guid>? reports);
    }
}
