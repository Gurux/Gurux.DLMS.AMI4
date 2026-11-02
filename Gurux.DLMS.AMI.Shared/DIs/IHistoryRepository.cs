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


using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle history items.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle history items.")]
    public interface IHistoryRepository
    {
        /// <summary>
        /// List history items.
        /// </summary>
        /// <returns>History items.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List history items.")]
        Task<IEnumerable<GXChangeHistoryItem>> ListAsync(
            ListHistoryItems? request = null,
            ListHistoryItemsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read history.
        /// </summary>
        /// <param name="id">History id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested history.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read history.")]
        Task<GXChangeHistoryItem> ReadAsync(Guid id, Expression<Func<GXChangeHistoryItem, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new history.
        /// </summary>
        /// <param name="history">The record to insert.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifier of the inserted record.</returns>
        [System.ComponentModel.Description("Inserts new history.")]
        Task<Guid> AppendAsync(
            GXChangeHistoryItem history,
            CancellationToken cancellationToken = default);

        /// <summary>Permanently deletes history items recorded strictly before the specified cutoff.</summary>
        /// <remarks>
        /// Requires administrator access and the history deletion permission. Applies to all users.
        /// Deletion uses bounded batches; cancellation does not undo previously committed batches.
        /// Entity revision clocks and links on retained history items are preserved.
        /// </remarks>
        /// <param name="cutoff">Exclusive cutoff instant; items recorded at this instant or later are retained.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of history items deleted by this operation.</returns>
        [System.ComponentModel.Description("Permanently deletes history items older than an exclusive cutoff while preserving entity revision clocks.")]
        Task<long> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);

        /// <summary>Reads the target entity's current configuration revision for an accessible history item.</summary>
        /// <param name="historyItemId">The identifier of an accessible history item for the target entity.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current entity revision to supply as the expected revision during restoration.</returns>
        [System.ComponentModel.Description("Reads the current entity revision independently of the selected historical version.")]
        Task<long> GetCurrentRevisionAsync(Guid historyItemId, CancellationToken cancellationToken = default);

        /// <summary>Restores the target entity to the selected history item's saved state as a new latest revision.</summary>
        /// <remarks>
        /// Uses <see cref="GXChangeHistoryItem.AfterJson"/> from the selected history item as the restoration source.
        /// A history item without a saved after-state cannot be restored by this operation.
        /// Existing history items are preserved; the restoration creates a new history item whose
        /// before-state is the current entity state and whose after-state is the restored state.
        /// The new revision follows the current entity revision, not the selected historical revision.
        /// The operation must check the caller's permissions and validate the restored data.
        /// The current revision check, entity update, and new history item insertion must be atomic.
        /// If the current revision differs from the expected revision, or validation fails,
        /// the operation must fail without modifying the entity or its history.
        /// </remarks>
        /// <param name="historyItemId">The identifier of the history item containing the saved state to restore.</param>
        /// <param name="expectedRevision">The expected current revision of the target entity, not the revision to restore.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifier of the new history item recording the restoration.</returns>
        [System.ComponentModel.Description("Restores a saved entity state as a new latest revision, preserving existing history, and returns the new history item identifier.")]
        Task<Guid> RestoreAsync(Guid historyItemId, long expectedRevision, CancellationToken cancellationToken = default);
    }
}
