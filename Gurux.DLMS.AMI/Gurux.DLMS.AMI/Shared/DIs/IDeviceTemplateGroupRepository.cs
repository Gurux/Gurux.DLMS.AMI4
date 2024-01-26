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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle device template groups.
    /// </summary>
    public interface IDeviceTemplateGroupRepository
    {
        /// <summary>
        /// List device template groups.
        /// </summary>
        /// <returns>Device template groups.</returns>
        Task<GXDeviceTemplateGroup[]> ListAsync(
            ClaimsPrincipal User,
            ListDeviceTemplateGroups? request,
            ListDeviceTemplateGroupsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read device template group.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Device template group id.</param>
        /// <returns>Device template group information.</returns>
        Task<GXDeviceTemplateGroup> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Update device template groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">Updated device template groups.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXDeviceTemplateGroup> groups,
            Expression<Func<GXDeviceTemplateGroup, object?>>? columns = null);

        /// <summary>
        /// Delete device template group(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">Device template groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(
            ClaimsPrincipal User,
            IEnumerable<Guid> groups, bool delete);

        /// <summary>
        /// Returns device template groups list where device template belongs.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="deviceTemplateId">Device template ID</param>
        /// <returns>List of device template groups.</returns>
        Task<List<GXDeviceTemplateGroup>> GetJoinedDeviceTemplateGroups(ClaimsPrincipal User, Guid deviceTemplateId);

        /// <summary>
        /// Returns list of users that can access this device template group.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groupId">Device template group Id.</param>
        /// <returns>List of users.</returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? groupId);

        /// <summary>
        /// Returns list of users that can access device template groups.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="groupIds">Device template group Ids.</param>
        /// <returns>List of users.</returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? groupIds);
    }
}
