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

/// <summary>Preserves structured errors when configuration validation fails.</summary>
public sealed class GXAgentOptionsValidationException : ValidationException
{
    /// <summary>Creates an exception from configuration errors.</summary>
    /// <param name="errors">Configuration errors.</param>
    public GXAgentOptionsValidationException(IEnumerable<GXAgentValidationError> errors)
        : this(errors?.ToArray() ?? throw new ArgumentNullException(nameof(errors))) { }
    /// <summary>Preserves structured validation errors and builds their combined diagnostic message.</summary>
    /// <param name="errors">Structured configuration validation failures retained by the exception.</param>
    private GXAgentOptionsValidationException(GXAgentValidationError[] errors)
        : base(string.Join(Environment.NewLine, errors.Select(e => $"{e.PropertyPath}: {e.Message}")))
        => Errors = Array.AsReadOnly(errors);
    /// <summary>Gets the original error codes and property paths.</summary>
    public IReadOnlyList<GXAgentValidationError> Errors { get; }
}
