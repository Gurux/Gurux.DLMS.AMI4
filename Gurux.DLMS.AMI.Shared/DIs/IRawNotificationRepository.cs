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
// This file is a part of Gurux RawNotification Framework.
//
// Gurux RawNotification Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux RawNotification Framework is distributed in the hope that it will be useful,
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
    /// <summary>Stores received bytes and coordinates deferred notification processing.</summary>
    /// <remarks>
    /// Implementations must enforce access to both receiving and processing agents.
    /// All lease transitions are atomic and use server UTC time. A connection is identified
    /// by its receiving Agent and ConnectionId; its chunks must never be split between processors.
    /// </remarks>
    public interface IRawNotificationRepository
    {
        /// <summary>Lists notifications for inspection without reserving them.</summary>
        /// <param name="request">Filtering and paging parameters.</param>
        /// <param name="response">Optional paging response.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Matching notifications.</returns>
        Task<IEnumerable<GXRawNotification>> ListAsync(
            ListRawNotifications? request = null,
            ListRawNotificationsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>Reads a notification without reserving it.</summary>
        /// <param name="id">Notification identifier.</param>
        /// <param name="columns">Optional columns to read.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The notification.</returns>
        Task<GXRawNotification> ReadAsync(Guid id,
            Expression<Func<GXRawNotification, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>Adds received chunks without parsing their bytes.</summary>
        /// <remarks>
        /// Delivery is idempotent by notification Id: an identical retry returns the existing Id;
        /// conflicting content is rejected. Agent and ConnectionId must be nonempty and
        /// Chunks must arrive in consecutive SequenceNumber order starting at zero. An identical
        /// retry is accepted, but new chunks after ConnectionClosed are rejected.
        /// Processing state is initialized by the server, ignoring caller-supplied lease fields.
        /// </remarks>
        /// <param name="notifications">Received chunks with stable identifiers.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Identifiers in input order, including identical retries.</returns>
        Task<IEnumerable<Guid>> AddAsync(IEnumerable<GXRawNotification> notifications,
            CancellationToken cancellationToken = default);

        /// <summary>Atomically reserves the oldest eligible connection for a processor.</summary>
        /// <remarks>
        /// Returns all uncompleted chunks of one connection ordered by SequenceNumber, or an empty
        /// collection if none is available. All returned chunks share a new LeaseToken and LeaseExpires.
        /// Pending chunks, due retries and expired leases are eligible; an unexpired lease excludes
        /// the entire connection. Attempts is incremented for each reserved chunk.
        /// A processor must retain incomplete parser state while it owns the connection.
        /// </remarks>
        /// <param name="processorId">Processing agent identifier.</param>
        /// <param name="leaseDuration">Lease duration of at least one millisecond.</param>
        /// <param name="receivingAgentId">Optional receiving agent filter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Reserved chunks carrying the server-generated lease token.</returns>
        Task<IEnumerable<GXRawNotification>> GetNextAsync(Guid processorId, TimeSpan leaseDuration,
            Guid? receivingAgentId = null, CancellationToken cancellationToken = default);

        /// <summary>Reads and reserves newly arrived chunks under an existing connection lease.</summary>
        /// <remarks>
        /// Validates the unexpired lease atomically, applies it to new chunks and increments their
        /// Attempts. Returns all chunks held by this lease in sequence order, including previously
        /// returned chunks. The processor must deduplicate by Id. An empty result does not indicate
        /// connection closure; ConnectionClosed identifies the final record.
        /// </remarks>
        /// <param name="processorId">Lease owner.</param>
        /// <param name="leaseToken">Current connection lease token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Chunks held by the connection lease.</returns>
        Task<IEnumerable<GXRawNotification>> GetLeasedAsync(Guid processorId, Guid leaseToken,
            CancellationToken cancellationToken = default);

        /// <summary>Extends an unexpired connection lease atomically.</summary>
        /// <param name="processorId">Lease owner.</param>
        /// <param name="leaseToken">Current connection lease token.</param>
        /// <param name="leaseDuration">Duration of at least one millisecond from server UTC time; never shortens the current lease.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated lease expiration time.</returns>
        Task<DateTimeOffset> RenewLeaseAsync(Guid processorId, Guid leaseToken, TimeSpan leaseDuration,
            CancellationToken cancellationToken = default);

        /// <summary>Completes a closed connection after its device updates have been persisted.</summary>
        /// <remarks>
        /// Atomically validates ownership and the unexpired lease, requires every chunk including
        /// ConnectionClosed to be held by this lease, marks them Completed and sets Processed.
        /// Rejects completion if additional chunks have arrived without being retrieved.
        /// Do not complete partial messages: retries must be able to reconstruct parser state.
        /// Device updates must be idempotent because they can be retried before this acknowledgment.
        /// </remarks>
        /// <param name="processorId">Lease owner.</param>
        /// <param name="leaseToken">Current connection lease token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DoneAsync(Guid processorId, Guid leaseToken, CancellationToken cancellationToken = default);

        /// <summary>Records a processing failure and releases the entire connection lease.</summary>
        /// <remarks>
        /// Atomically validates ownership and the unexpired lease, records Error and marks all
        /// uncompleted connection chunks Failed. A null nextAttempt means no automatic retry;
        /// otherwise the connection becomes eligible at that server UTC timestamp.
        /// </remarks>
        /// <param name="processorId">Lease owner.</param>
        /// <param name="leaseToken">Current connection lease token.</param>
        /// <param name="error">Processing error description.</param>
        /// <param name="nextAttempt">Optional automatic retry time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task FailAsync(Guid processorId, Guid leaseToken, string error, DateTimeOffset? nextAttempt = null,
            CancellationToken cancellationToken = default);

        /// <summary>Requeues failed or expired connections for an explicit retry.</summary>
        /// <remarks>
        /// Each identifier selects its entire receiving-agent/connection group. The operation is
        /// atomic and rejects groups with active leases or completed chunks. Clears lease and retry
        /// timestamps, preserves Attempts and Error, and sets Status to Pending.
        /// </remarks>
        /// <param name="notifications">Identifiers selecting connections to restart.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RestartAsync(IEnumerable<Guid> notifications, CancellationToken cancellationToken = default);

        /// <summary>Deletes selected connections, rejecting any with an active lease.</summary>
        /// <param name="notifications">Identifiers selecting entire connections to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> notifications, CancellationToken cancellationToken = default);

        /// <summary>Deletes only completed connections processed before the retention cutoff.</summary>
        /// <param name="processedBefore">Exclusive UTC retention cutoff.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ClearAsync(DateTimeOffset processedBefore, CancellationToken cancellationToken = default);
    }
}
