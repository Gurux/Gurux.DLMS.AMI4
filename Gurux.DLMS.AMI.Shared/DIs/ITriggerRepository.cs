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
    [System.ComponentModel.Description("This interface is used to handle triggers.")]
    public interface ITriggerRepository
    {
        /// <summary>
        /// List triggers.
        /// </summary>
        /// <returns>Triggers.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List triggers.")]
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
        [System.ComponentModel.Description("Read trigger.")]
        Task<GXTrigger> ReadAsync(Guid id, Expression<Func<GXTrigger, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new triggers or updates existing ones.
        /// </summary>
        /// <param name="triggers">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Inserts new triggers or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXTrigger> triggers,
            Expression<Func<GXTrigger, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete triggers.</summary>
        /// <param name="triggers">Trigger(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Delete triggers.")]
        Task DeleteAsync(IEnumerable<Guid> triggers, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this trigger.
        /// </summary>
        /// <param name="triggerId">Trigger id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this trigger.")]
        Task<List<string>> GetUsersAsync(Guid? triggerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access triggers.
        /// </summary>
        /// <param name="Ids">Trigger ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access triggers.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? Ids, CancellationToken cancellationToken = default);

        /// <summary>Refresh triggerss.</summary>
        /// <returns>True, if there are new triggers.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Refresh triggerss.")]
        Task<bool> RefrestAsync(CancellationToken cancellationToken = default);
    }
}
