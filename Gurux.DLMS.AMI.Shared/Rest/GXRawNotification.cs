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
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Submits received notification chunks for deferred parsing.</summary>
    [DataContract]
    public class AddRawNotification
    {
        /// <summary>Received chunks with stable identifiers for idempotent delivery retries.</summary>
        /// <remarks>The server initializes processing state and lease fields. Submit chunks in consecutive sequence order starting at zero.</remarks>
        [DataMember]
        public IEnumerable<GXRawNotification>? RawNotifications { get; set; }
    }

    /// <summary>Result of storing received notification chunks.</summary>
    [DataContract]
    public class AddRawNotificationResponse
    {
        /// <summary>Stored notification identifiers in input order, including identical retries.</summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Filters and pages raw notifications without reserving them for processing.</summary>
    [DataContract]
    public class ListRawNotifications
    {
        /// <summary>Zero-based index of the first notification to retrieve.</summary>
        [DataMember]
        public int Index { get; set; }

        /// <summary>Maximum number of notifications to retrieve; zero disables paging.</summary>
        [DataMember]
        public int Count { get; set; }

        /// <summary>Optional notification filter criteria.</summary>
        [DataMember]
        public GXRawNotification? Filter { get; set; }

        /// <summary>Whether to include notifications belonging to other users.</summary>
        /// <remarks>Requires permission to access the requested users' data.</remarks>
        [DataMember]
        public bool AllUsers { get; set; }

        /// <summary>Additional information to select; reserved for future use.</summary>
        [DataMember]
        public IEnumerable<string>? Select { get; set; }

        /// <summary>Property to sort by; null uses the endpoint's default ordering.</summary>
        [DataMember]
        public string? OrderBy { get; set; }

        /// <summary>Whether to sort in descending order.</summary>
        [DataMember]
        public bool Descending { get; set; }

        /// <summary>Notification identifiers to include; a nonempty collection restricts the results.</summary>
        [DataMember]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>Notification identifiers to omit from the results.</summary>
        [DataMember]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>A page of raw notifications and the matching total count.</summary>
    [DataContract]
    public class ListRawNotificationsResponse
    {
        /// <summary>Notifications matching the requested filter and paging parameters.</summary>
        [DataMember]
        public IEnumerable<GXRawNotification>? RawNotifications { get; set; }

        /// <summary>Total matching notifications before paging, or -1 when the count is unavailable.</summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>Requests an atomic processing lease for the oldest eligible connection.</summary>
    [DataContract]
    public class GetNextRawNotification
    {
        /// <summary>Identifier of the agent that will parse the notification bytes.</summary>
        [DataMember]
        public Guid ProcessorId { get; set; }

        /// <summary>Lease duration of at least one millisecond, measured from server UTC time.</summary>
        [DataMember]
        public TimeSpan LeaseDuration { get; set; }

        /// <summary>Optional filter identifying the agent that originally received the bytes.</summary>
        [DataMember]
        public Guid? ReceivingAgentId { get; set; }
    }

    /// <summary>Chunks reserved under one connection lease.</summary>
    [DataContract]
    public class GetNextRawNotificationResponse
    {
        /// <summary>Reserved chunks in sequence order, or an empty collection when none are available.</summary>
        /// <remarks>Chunks share a lease token and expiration. A chunk need not contain a complete DLMS message.</remarks>
        [DataMember]
        public IEnumerable<GXRawNotification> RawNotifications { get; set; } = [];
    }

    /// <summary>Retrieves all chunks held by an existing lease, including newly arrived chunks.</summary>
    [DataContract]
    public class GetLeasedRawNotifications
    {
        /// <summary>Identifier of the agent holding the lease.</summary>
        [DataMember]
        public Guid ProcessorId { get; set; }

        /// <summary>Server-issued token for the current unexpired connection lease.</summary>
        [DataMember]
        public Guid LeaseToken { get; set; }
    }

    /// <summary>Chunks held by an existing lease, in sequence order.</summary>
    [DataContract]
    public class GetLeasedRawNotificationsResponse
    {
        /// <summary>All leased chunks; previously returned chunks must be deduplicated by identifier.</summary>
        /// <remarks>ConnectionClosed marks the final record; an empty collection does not indicate closure.</remarks>
        [DataMember]
        public IEnumerable<GXRawNotification> RawNotifications { get; set; } = [];
    }

    /// <summary>Requests an extension of an unexpired connection lease.</summary>
    [DataContract]
    public class RenewRawNotificationLease
    {
        /// <summary>Identifier of the agent holding the lease.</summary>
        [DataMember]
        public Guid ProcessorId { get; set; }

        /// <summary>Server-issued token for the current lease.</summary>
        [DataMember]
        public Guid LeaseToken { get; set; }

        /// <summary>Lease duration of at least one millisecond from server UTC time; never shortens the current lease.</summary>
        [DataMember]
        public TimeSpan LeaseDuration { get; set; }
    }

    /// <summary>Result of extending a connection lease.</summary>
    [DataContract]
    public class RenewRawNotificationLeaseResponse
    {
        /// <summary>Updated server UTC expiration time.</summary>
        [DataMember]
        public DateTimeOffset LeaseExpires { get; set; }
    }

    /// <summary>Acknowledges a closed connection after parsing and persisting its device updates.</summary>
    /// <remarks>All chunks, including the final ConnectionClosed record, must belong to this unexpired lease.</remarks>
    [DataContract]
    public class RawNotificationDone
    {
        /// <summary>Identifier of the agent holding the lease.</summary>
        [DataMember]
        public Guid ProcessorId { get; set; }

        /// <summary>Server-issued token identifying the connection to complete.</summary>
        [DataMember]
        public Guid LeaseToken { get; set; }
    }

    /// <summary>Acknowledgment that the connection was marked as processed.</summary>
    [DataContract]
    public class RawNotificationDoneResponse
    {
    }

    /// <summary>Reports a processing failure and releases the connection lease.</summary>
    [DataContract]
    public class RawNotificationFailed
    {
        /// <summary>Identifier of the agent holding the lease.</summary>
        [DataMember]
        public Guid ProcessorId { get; set; }

        /// <summary>Server-issued token for the current unexpired connection lease.</summary>
        [DataMember]
        public Guid LeaseToken { get; set; }

        /// <summary>Description of the processing failure.</summary>
        [DataMember]
        public string Error { get; set; } = string.Empty;

        /// <summary>Earliest UTC retry time; null disables automatic retries for this failure.</summary>
        [DataMember]
        public DateTimeOffset? NextAttempt { get; set; }
    }

    /// <summary>Acknowledgment that a processing failure was recorded.</summary>
    [DataContract]
    public class RawNotificationFailedResponse
    {
    }

    /// <summary>Deletes the connections selected by notification identifiers.</summary>
    /// <remarks>Each identifier selects its entire connection. Active leases prevent deletion.</remarks>
    [DataContract]
    public class DeleteRawNotification
    {
        /// <summary>Notification identifiers selecting the connections to delete.</summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Acknowledgment that the selected connections were deleted.</summary>
    [DataContract]
    public class DeleteRawNotificationResponse
    {
    }

    /// <summary>Deletes completed connections older than a retention cutoff.</summary>
    [DataContract]
    public class ClearRawNotification
    {
        /// <summary>Exclusive UTC cutoff compared with successful processing time.</summary>
        [DataMember]
        public DateTimeOffset ProcessedBefore { get; set; }
    }

    /// <summary>Acknowledgment that completed connections older than the cutoff were deleted.</summary>
    [DataContract]
    public class ClearRawNotificationResponse
    {
    }
}
