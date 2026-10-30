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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Submits received notification chunks for deferred parsing.</summary>
    [Description("Submits received notification chunks for deferred parsing.")]
    [DataContract]
    public class AddRawNotification
    {
        /// <summary>Received chunks with stable identifiers for idempotent delivery retries.</summary>
        /// <remarks>The server initializes processing state and lease fields. Submit chunks in consecutive sequence order starting at zero.</remarks>
        [Description("Received chunks with stable identifiers for idempotent delivery retries.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        public IEnumerable<GXRawNotification>? RawNotifications { get; set; }
    }

    /// <summary>Result of storing received notification chunks.</summary>
    [Description("Result of storing received notification chunks.")]
    [DataContract]
    public class AddRawNotificationResponse
    {
        /// <summary>Identifiers of the newly added raw notification records.</summary>
        [Description("Identifiers of the newly added raw notification records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Filters and pages raw notifications without reserving them for processing.</summary>
    [Description("Filters and pages raw notifications without reserving them for processing.")]
    [DataContract]
    public class ListRawNotifications
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DataMember]
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of notifications to retrieve. Use 0 to return all matching items without paging.</summary>
        [DataMember]
        [DefaultValue(0)]
        [Description("Maximum number of notifications to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>Optional notification filter criteria.</summary>
        [Description("Optional notification filter criteria.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        public GXRawNotification? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>Whether to include notifications belonging to other users.</summary>
        /// <remarks>Requires permission to access the requested users' data.</remarks>
        [Description("Whether to include notifications belonging to other users.")]
        [DataMember]
        public bool AllUsers { get; set; }

        /// <summary>Additional information to select; reserved for future use.</summary>
        [Description("Additional information to select; reserved for future use.")]
        [DataMember]
        public IEnumerable<string>? Select { get; set; }

        /// <summary>Property to sort by; null uses the endpoint's default ordering.</summary>
        [Description("Property to sort by; null uses the endpoint's default ordering.")]
        [DataMember]
        public string? OrderBy { get; set; }

        /// <summary>Whether to sort in descending order.</summary>
        [Description("Whether to sort in descending order.")]
        [DataMember]
        public bool Descending { get; set; }

        /// <summary>Notification identifiers to include; a nonempty collection restricts the results.</summary>
        [Description("Notification identifiers to include; a nonempty collection restricts the results.")]
        [DataMember]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>Notification identifiers to omit from the results.</summary>
        [Description("Notification identifiers to omit from the results.")]
        [DataMember]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>A page of raw notifications and the matching total count.</summary>
    [Description("A page of raw notifications and the matching total count.")]
    [DataContract]
    public class ListRawNotificationsResponse
    {
        /// <summary>Notifications matching the requested filter and paging parameters.</summary>
        [Description("Notifications matching the requested filter and paging parameters.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        public IEnumerable<GXRawNotification>? RawNotifications { get; set; }

        /// <summary>Total matching notifications before paging, or -1 when the count is unavailable.</summary>
        [Description("Total matching notifications before paging, or -1 when the count is unavailable.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests an atomic processing lease for the oldest eligible connection.</summary>
    [Description("Requests an atomic processing lease for the oldest eligible connection.")]
    [DataContract]
    public class GetNextRawNotification
    {
        /// <summary>Identifier of the agent that will parse the notification bytes.</summary>
        [Description("Identifier of the agent that will parse the notification bytes.")]
        [DataMember]
        public Guid ProcessorId { get; set; }

        /// <summary>Lease duration of at least one millisecond, measured from server UTC time.</summary>
        [Description("Lease duration of at least one millisecond, measured from server UTC time.")]
        [DataMember]
        public TimeSpan LeaseDuration { get; set; }

        /// <summary>Optional filter identifying the agent that originally received the bytes.</summary>
        [Description("Optional filter identifying the agent that originally received the bytes.")]
        [DataMember]
        public Guid? ReceivingAgentId { get; set; }
    }

    /// <summary>Chunks reserved under one connection lease.</summary>
    [Description("Chunks reserved under one connection lease.")]
    [DataContract]
    public class GetNextRawNotificationResponse
    {
        /// <summary>Reserved chunks in sequence order, or an empty collection when none are available.</summary>
        /// <remarks>Chunks share a lease token and expiration. A chunk need not contain a complete DLMS message.</remarks>
        [Description("Reserved chunks in sequence order, or an empty collection when none are available.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        public IEnumerable<GXRawNotification> RawNotifications { get; set; } = [];
    }

    /// <summary>Retrieves all chunks held by an existing lease, including newly arrived chunks.</summary>
    [Description("Retrieves all chunks held by an existing lease, including newly arrived chunks.")]
    [DataContract]
    public class GetLeasedRawNotifications
    {
        /// <summary>Identifier of the agent holding the lease.</summary>
        [Description("Identifier of the agent holding the lease.")]
        [DataMember]
        public Guid ProcessorId { get; set; }

        /// <summary>Server-issued token for the current unexpired connection lease.</summary>
        [Description("Server-issued token for the current unexpired connection lease.")]
        [DataMember]
        public Guid LeaseToken { get; set; }
    }

    /// <summary>Chunks held by an existing lease, in sequence order.</summary>
    [Description("Chunks held by an existing lease, in sequence order.")]
    [DataContract]
    public class GetLeasedRawNotificationsResponse
    {
        /// <summary>All leased chunks; previously returned chunks must be deduplicated by identifier.</summary>
        /// <remarks>ConnectionClosed marks the final record; an empty collection does not indicate closure.</remarks>
        [Description("All leased chunks; previously returned chunks must be deduplicated by identifier.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        public IEnumerable<GXRawNotification> RawNotifications { get; set; } = [];
    }

    /// <summary>Requests an extension of an unexpired connection lease.</summary>
    [Description("Requests an extension of an unexpired connection lease.")]
    [DataContract]
    public class RenewRawNotificationLease
    {
        /// <summary>Identifier of the agent holding the lease.</summary>
        [Description("Identifier of the agent holding the lease.")]
        [DataMember]
        public Guid ProcessorId { get; set; }

        /// <summary>Server-issued token for the current lease.</summary>
        [Description("Server-issued token for the current lease.")]
        [DataMember]
        public Guid LeaseToken { get; set; }

        /// <summary>Lease duration of at least one millisecond from server UTC time; never shortens the current lease.</summary>
        [Description("Lease duration of at least one millisecond from server UTC time; never shortens the current lease.")]
        [DataMember]
        public TimeSpan LeaseDuration { get; set; }
    }

    /// <summary>Result of extending a connection lease.</summary>
    [Description("Result of extending a connection lease.")]
    [DataContract]
    public class RenewRawNotificationLeaseResponse
    {
        /// <summary>Updated server UTC expiration time.</summary>
        [Description("Updated server UTC expiration time.")]
        [DataMember]
        public DateTimeOffset LeaseExpires { get; set; }
    }

    /// <summary>Acknowledges a closed connection after parsing and persisting its device updates.</summary>
    /// <remarks>All chunks, including the final ConnectionClosed record, must belong to this unexpired lease.</remarks>
    [Description("Acknowledges a closed connection after parsing and persisting its device updates.")]
    [DataContract]
    public class RawNotificationDone
    {
        /// <summary>Identifier of the agent holding the lease.</summary>
        [Description("Identifier of the agent holding the lease.")]
        [DataMember]
        public Guid ProcessorId { get; set; }

        /// <summary>Server-issued token identifying the connection to complete.</summary>
        [Description("Server-issued token identifying the connection to complete.")]
        [DataMember]
        public Guid LeaseToken { get; set; }
    }

    /// <summary>Acknowledgment that the connection was marked as processed.</summary>
    [Description("Acknowledgment that the connection was marked as processed.")]
    [DataContract]
    public class RawNotificationDoneResponse
    {
    }

    /// <summary>Reports a processing failure and releases the connection lease.</summary>
    [Description("Reports a processing failure and releases the connection lease.")]
    [DataContract]
    public class RawNotificationFailed
    {
        /// <summary>Identifier of the agent holding the lease.</summary>
        [Description("Identifier of the agent holding the lease.")]
        [DataMember]
        public Guid ProcessorId { get; set; }

        /// <summary>Server-issued token for the current unexpired connection lease.</summary>
        [Description("Server-issued token for the current unexpired connection lease.")]
        [DataMember]
        public Guid LeaseToken { get; set; }

        /// <summary>Description of the processing failure.</summary>
        [Description("Description of the processing failure.")]
        [DataMember]
        public string Error { get; set; } = string.Empty;

        /// <summary>Earliest UTC retry time; null disables automatic retries for this failure.</summary>
        [Description("Earliest UTC retry time; null disables automatic retries for this failure.")]
        [DataMember]
        public DateTimeOffset? NextAttempt { get; set; }
    }

    /// <summary>Acknowledgment that a processing failure was recorded.</summary>
    [Description("Acknowledgment that a processing failure was recorded.")]
    [DataContract]
    public class RawNotificationFailedResponse
    {
    }

    /// <summary>Deletes the connections selected by notification identifiers.</summary>
    /// <remarks>Each identifier selects its entire connection. Active leases prevent deletion.</remarks>
    [Description("Deletes the connections selected by notification identifiers.")]
    [DataContract]
    public class DeleteRawNotification
    {
        /// <summary>Identifiers of the raw notification records to delete.</summary>
        [Description("Identifiers of the raw notification records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Acknowledgment that the selected connections were deleted.</summary>
    [Description("Acknowledgment that the selected connections were deleted.")]
    [DataContract]
    public class DeleteRawNotificationResponse
    {
    }

    /// <summary>Deletes completed connections older than a retention cutoff.</summary>
    [Description("Deletes completed connections older than a retention cutoff.")]
    [DataContract]
    public class ClearRawNotification
    {
        /// <summary>Exclusive UTC cutoff compared with successful processing time.</summary>
        [Description("Exclusive UTC cutoff compared with successful processing time.")]
        [DataMember]
        public DateTimeOffset ProcessedBefore { get; set; }
    }

    /// <summary>Acknowledgment that completed connections older than the cutoff were deleted.</summary>
    [Description("Acknowledgment that completed connections older than the cutoff were deleted.")]
    [DataContract]
    public class ClearRawNotificationResponse
    {
    }
}
