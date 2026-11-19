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

namespace Gurux.DLMS.AMI.Agent.Shared;

/// <summary>Changes the host must handle. Multiple changes may be delivered together.</summary>
[Flags]
public enum AgentHostChange
{
    /// <summary>No pending change.</summary>
    None = 0,
    /// <summary>A complete application release is staged for external installation.</summary>
    VersionAvailable = 1,
    /// <summary>The current configuration must be persisted.</summary>
    SettingsChanged = 2,
    /// <summary>The server rejected the PAT; stop the runtime and request new credentials.</summary>
    AuthenticationRequired = 4
}
