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

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    public interface ITriggerRepository
    {
        /// <summary>
        /// List triggers.
        /// </summary>
        /// <returns>Triggers.</returns>
        Task<GXTrigger[]> ListAsync(
            ClaimsPrincipal User, 
            ListTriggers? request, 
            ListTriggersResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read trigger.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Trigger id.</param>
        /// <returns></returns>
        Task<GXTrigger> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Update trigger(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="triggers">Updated trigger(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User, 
            IEnumerable<GXTrigger> triggers,
            Expression<Func<GXTrigger, object?>>? columns = null);

        /// <summary>
        /// Delete trigger(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="triggers">Trigger(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> triggers, bool delete);

        /// <summary>
        /// Get all users that can access this trigger.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="triggerId">Trigger id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? triggerId);

        /// <summary>
        /// Get all users that can access triggers.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="triggerIds">Trigger ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? triggerIds);

        /// <summary>
        /// Refresh triggers(s).
        /// </summary>
        Task RefrestAsync(ClaimsPrincipal User);
    }
}
