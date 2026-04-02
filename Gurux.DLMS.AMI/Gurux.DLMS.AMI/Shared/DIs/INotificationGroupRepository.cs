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
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle notification groups.
    /// </summary>
    public interface INotificationGroupRepository
    {
        /// <summary>
        /// List notification groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXNotificationGroup[]> ListAsync(
            ListNotificationGroups? request,
            ListNotificationGroupsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read notification group information.
        /// </summary>
        /// <param name="id">Notification group id.</param>
        /// <returns></returns>
        Task<GXNotificationGroup> ReadAsync(Guid id);

        /// <summary>
        /// Update notification groups.
        /// </summary>
        /// <param name="groups">Updated notification groups.</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            IEnumerable<GXNotificationGroup> groups,
            Expression<Func<GXNotificationGroup, object?>>? columns = null);

        /// <summary>
        /// Delete notification group(s).
        /// </summary>
        /// <param name="groups">Notification groups to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(IEnumerable<Guid> groups, bool delete);

        /// <summary>
        /// Returns notification groups list where notification belongs.
        /// </summary>
        /// <param name="notificationId">Notification ID</param>
        /// <returns>List of notification groups.</returns>
        Task<List<GXNotificationGroup>> GetJoinedNotificationGroups(Guid notificationId);

        /// <summary>
        /// Get all users that can access this notification group.
        /// </summary>
        /// <param name="notificationGroupId">Notification group id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(Guid? notificationGroupId);

        /// <summary>
        /// Get all users that can access notification group.
        /// </summary>
        /// <param name="notificationGroupIds">Agent Notification ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? notificationGroupIds);
    }
}
