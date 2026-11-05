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

namespace Gurux.DLMS.AMI.Agent.Shared;

/// <summary>Stable, locale-independent configuration error codes. Existing values must not be renumbered.</summary>
public enum GXAgentValidationErrorCode
{
    /// <summary>The server address is invalid.</summary>
    InvalidAddress = 1,
    /// <summary>The access token is missing or malformed.</summary>
    InvalidToken = 2,
    /// <summary>A registered agent identifier is required.</summary>
    AgentIdRequired = 3,
    /// <summary>The worker version identifier is invalid.</summary>
    InvalidVersion = 4,
    /// <summary>The diagnostic trace level is invalid.</summary>
    InvalidTraceLevel = 5,
    /// <summary>A worker thread count is invalid.</summary>
    InvalidThreadCount = 6,
    /// <summary>A nonnegative value is required.</summary>
    NegativeValue = 7,
    /// <summary>Enabled transport settings are missing.</summary>
    MediaSettingsRequired = 8,
    /// <summary>An enabled raw parser requires a script method.</summary>
    ScriptMethodRequired = 9
}

/// <summary>A structured configuration error without user-supplied secret values.</summary>
/// <param name="Code">Stable error identifier for UI translation and programmatic handling.</param>
/// <param name="PropertyPath">Configuration property path.</param>
/// <param name="Message">Human-readable diagnostic message.</param>
public sealed record GXAgentValidationError(GXAgentValidationErrorCode Code, string PropertyPath, string Message);

/// <summary>Preserves structured errors when configuration validation fails.</summary>
public sealed class GXAgentOptionsValidationException : ValidationException
{
    /// <summary>Creates an exception from configuration errors.</summary>
    /// <param name="errors">Configuration errors.</param>
    public GXAgentOptionsValidationException(IEnumerable<GXAgentValidationError> errors)
        : this(errors?.ToArray() ?? throw new ArgumentNullException(nameof(errors))) { }
    private GXAgentOptionsValidationException(GXAgentValidationError[] errors)
        : base(string.Join(Environment.NewLine, errors.Select(e => $"{e.PropertyPath}: {e.Message}")))
        => Errors = Array.AsReadOnly(errors);
    /// <summary>Gets the original error codes and property paths.</summary>
    public IReadOnlyList<GXAgentValidationError> Errors { get; }
}
