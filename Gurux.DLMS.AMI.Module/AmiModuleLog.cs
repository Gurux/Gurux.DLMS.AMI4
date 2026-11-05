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

namespace Gurux.DLMS.AMI.Module;

/// <summary>Log data; the host assigns source, identity, log type and enum membership.</summary>
/// <param name="Category">Category identifying the kind of recorded event.</param>
/// <param name="UserId">Optional identifier of the user associated with the event; null represents a system event.</param>
/// <param name="Message">Human-readable event message.</param>
/// <param name="Data">Optional serialized details associated with the log entry.</param>
/// <param name="Level">Trace severity of the recorded event.</param>
/// <param name="Closable">Whether the log entry supports being marked as closed.</param>
/// <param name="Duration">Optional operation duration in milliseconds.</param>
/// <param name="CreationTime">Optional event time; the host supplies the time when omitted.</param>
public sealed record AmiModuleLog(string Category, string? UserId, string Message, string? Data = null,
    int Level = 4, bool Closable = true, int? Duration = null, DateTimeOffset? CreationTime = null)
{
    /// <summary>Optional identifier correlating entries from one workflow execution.</summary>
    public Guid? ExecutionId { get; init; }
}
