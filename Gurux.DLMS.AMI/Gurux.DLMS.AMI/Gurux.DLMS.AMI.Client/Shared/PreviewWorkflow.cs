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

using Gurux.DLMS.AMI.Shared.DTOs.Workflow;

namespace Gurux.DLMS.AMI.Client.Shared;

/// <summary>A read-only preview of unsaved workflow values. The caller is the hypothetical event actor.</summary>
public sealed class PreviewWorkflow
{
    /// <summary>Unsaved workflow values to validate and match.</summary>
    public GXWorkflow Workflow { get; set; } = new();
    /// <summary>Registered sample event code.</summary>
    public string EventCode { get; set; } = "";
    /// <summary>Existing sample device for device and value events.</summary>
    public Guid? DeviceId { get; set; }
    /// <summary>Sample agent or schedule identifier.</summary>
    public string? TargetId { get; set; }
}

/// <summary>Matching information only; scripts are never evaluated.</summary>
public sealed class PreviewWorkflowResponse
{
    /// <summary>Whether the selected trigger matches the sample event.</summary>
    public bool TriggerMatches { get; set; }
    /// <summary>Whether all device event filters match the sample.</summary>
    public bool FiltersMatch { get; set; }
    /// <summary>Whether the draft workflow is enabled.</summary>
    public bool Enabled { get; set; }
    /// <summary>Resolved automatic execution account identifier.</summary>
    public string ExecutionUserId { get; set; } = "";
    /// <summary>Resolved automatic execution account name.</summary>
    public string ExecutionUserName { get; set; } = "";
    /// <summary>Explicit confirmation that scripts were not evaluated.</summary>
    public string ConditionStatus { get; set; } = "Not evaluated. No condition or action scripts were run.";
}
