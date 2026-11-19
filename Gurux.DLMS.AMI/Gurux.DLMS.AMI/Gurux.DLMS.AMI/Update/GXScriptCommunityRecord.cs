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

using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Services.Database;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Model;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Private, durable submission state. Secrets never form part of API responses.</summary>
[IndexCollection(true, nameof(OwnerId), nameof(ScriptId), nameof(SourceHash))]
public sealed class GXScriptCommunityRecord : IUnique<Guid>
{
    /// <summary>Local durable submission identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Authenticated submitting user; never sent to the community service.</summary>
    [StringLength(450)] public string OwnerId { get; set; } = "";
    /// <summary>Persisted local script identifier.</summary>
    public Guid ScriptId { get; set; }
    /// <summary>SHA-256 used to suppress duplicate submissions.</summary>
    [StringLength(64)] public string SourceHash { get; set; } = "";
    /// <summary>Display name retained for notifications.</summary>
    [StringLength(200)] public string ScriptName { get; set; } = "";
    /// <summary>DeviceTemplate for profile submissions; null for existing script receipts.</summary>
    [StringLength(32)] public string? ContentType { get; set; }
    /// <summary>Durable publishing or review status.</summary>
    [StringLength(32)] public string Status { get; set; } = "Publishing";
    /// <summary>Approved catalog version, when available.</summary>
    [StringLength(64)] public string? Version { get; set; }
    /// <summary>Remote submission identifier.</summary>
    public Guid RemoteId { get; set; }
    /// <summary>Data-protected opaque receipt credential; never returned to clients.</summary>
    public string? ProtectedToken { get; set; }
    /// <summary>Sanitized actionable explanation for a failed receipt.</summary>
    [StringLength(500)] public string? FailureMessage { get; set; }
    /// <summary>Whether the owner has received the terminal notification.</summary>
    public bool Acknowledged { get; set; }
    /// <summary>Creation time of the local submission.</summary>
    public DateTimeOffset Created { get; set; }
    /// <summary>Time the receipt was last refreshed.</summary>
    public DateTimeOffset? Checked { get; set; }
    /// <summary>Projects only public submission metadata.</summary>
    /// <returns>Public receipt metadata with a safe failure explanation, excluding protected credentials.</returns>
    public GXScriptCommunitySubmission ToResponse() => new()
    {
        Id = Id, ScriptName = ScriptName, ContentType = ContentType, Status = Status, Version = Version,
        Message = Status == "Failed" ? FailureMessage ?? Properties.Resources.CommunitySubmissionReceiptUnconfirmed : null
    };
}
