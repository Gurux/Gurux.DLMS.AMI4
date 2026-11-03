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
    /// <summary>Gets the selected worker version, including a version waiting for host reload.</summary>
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
