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

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to hanfle workflow group.
    /// </summary>
    public interface IWorkflowGroupRepository
    {
        /// <summary>
        /// List workflow groups.
        /// </summary>
        /// <returns>User groups.</returns>
        Task<GXWorkflowGroup[]> ListAsync(
            ClaimsPrincipal User, 
            ListWorkflowGroups? request, 
            ListWorkflowGroupsResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read workflow group information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Workflow group id.</param>
        /// <returns></returns>
        Task<GXWorkflowGroup> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Update workflow groups.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">Updated workflow groups.</param>
        Task<Guid[]> UpdateAsync(ClaimsPrincipal User, IEnumerable<GXWorkflowGroup> groups);

        /// <summary>
        /// Delete user group(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="groups">User groups to delete.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> groups);

        /// <summary>
        /// Returns workflow groups list where workflow belongs.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="workflowId">Workflow ID</param>
        /// <returns>List of workflow groups.</returns>
        Task<List<GXWorkflowGroup>> GetJoinedWorkflowGroups(ClaimsPrincipal User, Guid workflowId);
    }
}
