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
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle triggers.
    /// </summary>
    public interface ITriggerRepository
    {
        /// <summary>
        /// List triggers.
        /// </summary>
        /// <returns>Triggers.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXTrigger>> ListAsync(
            ListTriggers? request = null,
            ListTriggersResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read trigger.
        /// </summary>
        /// <param name="id">Trigger id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXTrigger> ReadAsync(Guid id, Expression<Func<GXTrigger, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update trigger(s).
        /// </summary>
        /// <param name="triggers">Updated trigger(s).</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXTrigger> triggers,
            Expression<Func<GXTrigger, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete trigger(s).
        /// </summary>
        /// <param name="triggers">Trigger(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> triggers, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this trigger.
        /// </summary>
        /// <param name="triggerId">Trigger id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid? triggerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access triggers.
        /// </summary>
        /// <param name="Ids">Trigger ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? Ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh triggers(s).
        /// </summary>
        /// <returns>True, if there are new triggers.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<bool> RefrestAsync(CancellationToken cancellationToken = default);
    }
}
