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

using Gurux.DLMS.AMI.Module.Enums;
namespace Gurux.DLMS.AMI.Module.Health;
/// <summary>Safe point-in-time runtime status without settings or exception contents.</summary>
public sealed record AmiModuleHealth(string ModuleId, string Generation, AmiModuleState State,
    int ActiveCalls, DateTimeOffset? StartedAt, DateTimeOffset? StopRequestedAt,
    DateTimeOffset? StoppedAt, bool PendingShutdown, bool ForcedCancellation,
    string? LastError, IReadOnlyList<AmiModuleBackgroundHealth> BackgroundServices);
/// <summary>Status of an already-created module background service.</summary>
public sealed record AmiModuleBackgroundHealth(string Name, string State);
