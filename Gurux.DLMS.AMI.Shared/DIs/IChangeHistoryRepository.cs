// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared.DTOs.History;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// Provides authorized history queries, append operations, restoration and retention cleanup.
    /// </summary>
    [System.ComponentModel.Description("Provides authorized history queries, append operations, restoration and retention cleanup.")]
    public interface IChangeHistoryRepository
    {
        /// <summary>
        /// Lists accessible history items without their JSON snapshots.
        /// </summary>
        /// <remarks>Authorization and filtering precede counting and paging. The total count includes only accessible matching items. Use ReadAsync to retrieve snapshots.</remarks>
        /// <returns>The requested page of accessible history items.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">Optional filtering, ordering, user scope and paging criteria.</param>
        /// <param name="response">Optional response populated with the selected page and total accessible matching count before paging.</param>
        [System.ComponentModel.Description("Lists accessible history items without JSON snapshots, applying authorization before counting and paging.")]
        Task<IEnumerable<GXChangeHistoryItem>> ListAsync(
            ListChangeHistoryItems? request = null,
            ListChangeHistoryItemsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads an accessible history item, including its JSON snapshots unless a projection is specified.
        /// </summary>
        /// <param name="id">The GUID of the history item, not the target entity or history type.</param>
        /// <param name="columns">Properties to retrieve, or null to retrieve the complete history item. Projection does not bypass authorization.</param>
        /// <returns>A task whose result contains the accessible history item. Missing or inaccessible items are not returned.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Reads an accessible history item with an optional property projection.")]
        Task<GXChangeHistoryItem> ReadAsync(Guid id, Expression<Func<GXChangeHistoryItem, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Appends a new history item without modifying existing history items.
        /// </summary>
        /// <remarks>
        /// The server assigns the actor, change time, change-set identifier, schema version and entity revision.
        /// An empty item identifier is generated; an existing item identifier must not be overwritten.
        /// The entity type must reference a registered history type by its stable GUID.
        /// Snapshots must match the target identifier and operation and contain only supported configuration fields.
        /// Recording follows the persisted history type's RecordHistory setting; disabled recording creates no history item.
        /// The entity revision still advances when recording is disabled.
        /// This operation records history; it does not apply the represented change to the target entity.
        /// When recording a target mutation, the mutation and history insertion must share one transaction.
        /// </remarks>
        /// <param name="history">The new history item containing the target reference, operation and snapshots.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The identifier of the inserted history item, or Guid.Empty when recording is disabled for the history type.</returns>
        [System.ComponentModel.Description("Appends a validated history item with server-assigned actor, timestamp and revision.")]
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
        /// The target entity must still exist; restoration does not recreate a deleted target.
        /// Existing history items are preserved. When recording is enabled for the type, restoration creates a history item whose
        /// before-state is the current entity state and whose after-state is the restored state.
        /// The new revision follows the current entity revision, not the selected historical revision.
        /// The operation must check the caller's permissions and validate the restored data.
        /// The current revision check, entity update, and any history item insertion must be atomic.
        /// If the current revision differs from the expected revision, or validation fails,
        /// the operation must fail without modifying the entity or its history.
        /// </remarks>
        /// <param name="historyItemId">The identifier of the history item containing the saved state to restore.</param>
        /// <param name="expectedRevision">The expected current revision of the target entity, not the revision to restore.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The identifier of the new history item, or Guid.Empty when restoration succeeds with history recording disabled.</returns>
        [System.ComponentModel.Description("Restores a saved entity state as a new latest revision, preserving existing history. Returns the new history item identifier, or Guid.Empty when recording is disabled.")]
        Task<Guid> RestoreAsync(Guid historyItemId, long expectedRevision, CancellationToken cancellationToken = default);
    }
}
