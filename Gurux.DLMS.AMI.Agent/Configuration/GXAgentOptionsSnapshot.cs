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

using System.Text.Json;
using System.Diagnostics;

namespace Gurux.DLMS.AMI.Agent.Shared;

/// <summary>A deep, validated configuration snapshot. Editing a returned copy cannot change this snapshot.</summary>
public sealed class GXAgentOptionsSnapshot
{
    private readonly GXAgentOptions value;
    private GXAgentOptionsSnapshot(GXAgentOptions value) => this.value = value;

    /// <summary>Creates a detached snapshot after validating the complete configuration.</summary>
    /// <param name="options">Editable configuration.</param>
    /// <param name="requireAgentId">Whether registration must already have finished.</param>
    /// <returns>The validated snapshot.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="GXAgentOptionsValidationException">The copied configuration does not satisfy validation rules.</exception>
    public static GXAgentOptionsSnapshot Capture(GXAgentOptions options, bool requireAgentId = true)
    {
        ArgumentNullException.ThrowIfNull(options);
        var copy = Copy(options);
        copy.Validate(requireAgentId);
        return new GXAgentOptionsSnapshot(copy);
    }

    private static GXAgentOptions Copy(GXAgentOptions source) =>
        JsonSerializer.Deserialize<GXAgentOptions>(JsonSerializer.Serialize(source))!;

    /// <summary>Gets a detached, editable copy suitable for JSON persistence. It contains the authentication token.</summary>
    /// <returns>A complete deep copy, including independent worker settings and byte arrays.</returns>
    public GXAgentOptions ToOptions() => Copy(value);

    /// <summary>Gets the agent identifier.</summary>
    public Guid Id => value.Id;
    /// <summary>Gets the server address.</summary>
    public string Address => value.Address;
    /// <summary>Gets the application version published in this configuration snapshot.</summary>
    /// <remarks>This value describes configuration and does not select an assembly to load.</remarks>
    public string? Version => value.Version;
    /// <summary>Gets the diagnostic trace level.</summary>
    public TraceLevel TraceLevel => value.TraceLevel;

    // Only the trusted worker assembly can read the published object without copying.
    // It must never mutate this object; every writer captures and publishes a new snapshot.
    internal GXAgentOptions RuntimeValue => value;

    /// <summary>Returns safe diagnostics without authentication or nested transport credentials.</summary>
    /// <returns>A safe diagnostic summary.</returns>
    public override string ToString() => value.ToString();
}
