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

using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>Received notification bytes and their deferred processing state.</summary>
    /// <remarks>
    /// Each record represents a received chunk, which may contain a partial DLMS message or several
    /// messages. Group chunks by Agent and ConnectionId and parse them in SequenceNumber order.
    /// The receiving agent supplies receipt metadata; the server manages processing and lease fields.
    /// </remarks>
    [Description("Received notification bytes and their deferred processing state.")]
    [DataContract]
    public class GXRawNotification : IUnique<Guid>
    {
        /// <summary>
        /// Notification identifier, preserved when delivery is retried.
        /// </summary>
        [DataMember]
        [Description("Notification identifier, preserved when delivery is retried.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }


        /// <summary>
        /// Agent that received the notification.
        /// </summary>
        [Description("Agent that received the notification.")]
        [DataMember]
        [ForeignKey(typeof(GXAgent), OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        public GXAgent? Agent { get; set; }

        /// <summary>
        /// Sender information.
        /// </summary>
        /// <remarks>
        /// Binary sender metadata supplied by the receiving transport. Any textual address must use
        /// an encoding agreed upon by the producer and parser; this property does not define one.
        /// </remarks>
        [DataMember]
        [Description("Sender information.")]
        [Filter(FilterType.Contains)]
        public byte[]? Sender { get; set; }

        /// <summary>
        /// Time when the receiving agent received this chunk, including its UTC offset.
        /// </summary>
        [DataMember]
        [Description("Time when the receiving agent received this chunk, including its UTC offset.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? Received { get; set; }

        /// <summary>
        /// Notification bytes exactly as received.
        /// </summary>
        /// <remarks>No DLMS parsing or transformation is applied. A connection-end record may have no bytes.</remarks>
        [DataMember]
        [Description("Notification bytes exactly as received.")]
        [Filter(FilterType.Contains)]
        public byte[]? Value { get; set; }

        /// <summary>Connection identifier shared by all chunks from one connection.</summary>
        /// <remarks>Assign a new nonempty identifier for each connection, including reconnects from the same sender.</remarks>
        [Description("Connection identifier shared by all chunks from one connection.")]
        [DataMember]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public Guid ConnectionId { get; set; }

        /// <summary>Chunk sequence number within the connection.</summary>
        /// <remarks>Starts at zero and increases by one for each chunk. Submit chunks in this order within each receiving agent's connection.</remarks>
        [Description("Chunk sequence number within the connection.")]
        [DataMember]
        [Filter(FilterType.Exact)]
        public long SequenceNumber { get; set; }

        /// <summary>Whether this record marks the end of the connection.</summary>
        /// <remarks>A connection-end record can have no notification bytes.</remarks>
        [Description("Whether this record marks the end of the connection.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        public bool ConnectionClosed { get; set; }

        /// <summary>DLMS interface type encoded as an integer.</summary>
        /// <remarks>Uses the same interface type values as NotifySettings.Interface.</remarks>
        [Description("DLMS interface type encoded as an integer.")]
        [DataMember]
        [Filter(FilterType.Exact)]
        public int Interface { get; set; }

        /// <summary>Current processing state.</summary>
        [Description("Current processing state.")]
        [DataMember]
        [DefaultValue(RawNotificationStatus.Pending)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public RawNotificationStatus Status { get; set; }

        /// <summary>Agent holding the current processing lease.</summary>
        [Description("Agent holding the current processing lease.")]
        [DataMember]
        [ForeignKey(typeof(GXAgent))]
        [Index(false)]
        [Filter(FilterType.Exact)]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        public GXAgent? Processor { get; set; }

        /// <summary>Token identifying the current lease, required when acknowledging processing.</summary>
        /// <remarks>Issued by the server. Chunks reserved together share a token; a new reservation gets a new token.</remarks>
        [Description("Token identifying the current lease, required when acknowledging processing.")]
        [DataMember]
        [Filter(FilterType.Exact)]
        public Guid? LeaseToken { get; set; }

        /// <summary>Time when the current processing lease expires.</summary>
        /// <remarks>Assigned using server UTC time. An expired lease cannot be used to acknowledge processing.</remarks>
        [Description("Time when the current processing lease expires.")]
        [DataMember]
        [Index(false)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LeaseExpires { get; set; }

        /// <summary>Number of processing attempts.</summary>
        /// <remarks>Incremented by the server when the chunk is reserved for a processing attempt.</remarks>
        [Description("Number of processing attempts.")]
        [DataMember]
        [DefaultValue(0)]
        [Filter(FilterType.Exact)]
        public int Attempts { get; set; }

        /// <summary>Earliest time when processing can be retried.</summary>
        /// <remarks>For a failed notification, null disables automatic retries. Uses server UTC time.</remarks>
        [Description("Earliest time when processing can be retried.")]
        [DataMember]
        [Index(false)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? NextAttempt { get; set; }

        /// <summary>Time when processing completed successfully.</summary>
        /// <remarks>Set by the server after acknowledgment; null until processing succeeds.</remarks>
        [Description("Time when processing completed successfully.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Processed { get; set; }

        /// <summary>Description of the most recent processing error.</summary>
        [Description("Description of the most recent processing error.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        public string? Error { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new();
            if (Received != null)
            {
                sb.Append(Received.ToString());
                sb.Append(' ');
            }
            sb.Append(Value);
            return sb.ToString();
        }
    }
}
