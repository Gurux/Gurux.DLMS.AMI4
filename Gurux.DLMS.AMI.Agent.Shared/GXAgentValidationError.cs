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
