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
