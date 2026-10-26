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

namespace Gurux.DLMS.AMI.Module;

/// <summary>
/// Module configuration and durable server-managed state supplied by the host.
/// </summary>
public interface IAmiModuleSettingsSnapshot
{
    /// <summary>
    /// Current serialized settings, or <see langword="null"/> when no settings have been supplied.
    /// </summary>
    string? Value { get; }

    /// <summary>
    /// Deserializes the current settings, creating a new instance when settings are blank or deserialize to null.
    /// </summary>
    /// <typeparam name="T">Settings model with a public parameterless constructor.
    /// </typeparam>
    /// <returns>
    /// The deserialized settings or a newly constructed model.
    /// </returns>
    T Read<T>() where T : new();

    /// <summary>
    /// Reads durable server-managed state stored with this module's settings.
    /// </summary>
    /// <remarks>
    /// Use one state model per module. State is separate from editable configuration.
    /// </remarks>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <typeparam name="T">Durable module state model with a public parameterless constructor.</typeparam>
    /// <returns>A task containing the stored module state, or a new state model when none exists.</returns>
    Task<T> ReadStateAsync<T>(CancellationToken cancellationToken = default) where T : class, new()
        => throw new NotSupportedException("Persistent module settings are unavailable.");

    /// <summary>
    /// Atomically updates server-managed state while preserving configuration.
    /// </summary>
    /// <remarks>
    /// The update runs under the host's module settings lock; do not perform other settings operations inside it.
    /// </remarks>
    /// <param name="update">Callback that modifies the module's durable state under the host settings lock.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <typeparam name="T">Durable module state model with a public parameterless constructor.</typeparam>
    /// <returns>A task containing the state saved after applying the update callback.</returns>
    Task<T> UpdateStateAsync<T>(Action<T> update, CancellationToken cancellationToken = default) where T : class, new()
        => throw new NotSupportedException("Persistent module settings are unavailable.");
}
