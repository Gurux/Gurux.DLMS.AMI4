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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Gurux.Service.Orm.Common;

namespace Gurux.DLMS.AMI.Shared.DTOs.History;

/// <summary>Private durable runtime-cache update awaiting delivery after a configuration transaction commits.</summary>
/// <remarks>This outbox is not user-visible change history. Its runtime payload is removed after delivery and must not be returned by history APIs.</remarks>
[Description("Private durable runtime-cache update awaiting delivery after a configuration transaction commits.")]
public sealed class GXChangeHistoryRuntimeEffect : IUnique<Guid>
{
    /// <summary>Gets or sets the deterministic key identifying the runtime resource family and target.</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the runtime resource family, Script or Workflow.</summary>
    [IsRequired, StringLength(16)]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the target resource identifier.</summary>
    public Guid EntityId { get; set; }

    /// <summary>Gets or sets the delivery version used to prevent acknowledgement of a newer pending update.</summary>
    public Guid Version { get; set; }

    /// <summary>Gets or sets whether the runtime resource must be removed.</summary>
    public bool Deleted { get; set; }

    /// <summary>Gets or sets the private serialized runtime DTO needed to rebuild the workflow or script cache.</summary>
    [IsRequired]
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which this latest pending update was recorded.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
