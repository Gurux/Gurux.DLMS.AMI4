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

/// <summary>Host-managed JSON user data, restricted to the bound module.</summary>
public interface IAmiModuleUserSettings
{
    /// <summary>Reads a named value belonging to the specified user in this module.</summary>
    /// <param name="userId">Identifier of the user who owns the module setting.</param>
    /// <param name="name">Name identifying the value within the user's module settings.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <typeparam name="T">Model serialized as JSON for the module-owned user setting.</typeparam>
    /// <returns>A task containing the stored value, or the default value when the setting does not exist.</returns>
    Task<T?> ReadAsync<T>(string userId, string name, CancellationToken cancellationToken = default);
    /// <summary>Lists this module's user values; values listed together must share a common JSON model.</summary>
    /// <param name="userId">Identifier of the user who owns the module setting.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <typeparam name="T">Model serialized as JSON for the module-owned user setting.</typeparam>
    /// <returns>A task containing the user's named values and their concurrency versions for this module.</returns>
    Task<IReadOnlyList<AmiModuleUserSetting<T>>> ListAsync<T>(string userId, CancellationToken cancellationToken = default);
    /// <summary>Upserts a value. Exclusive names can belong to only one user in this module.
    /// A module must use the same exclusivity mode for a given name throughout its lifetime.</summary>
    /// <param name="userId">Identifier of the user who owns the module setting.</param>
    /// <param name="name">Name identifying the value within the user's module settings.</param>
    /// <param name="value">Value to serialize as JSON and store for the user.</param>
    /// <param name="exclusiveToUser">Whether the setting name may belong to only one user in this module.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <typeparam name="T">Model serialized as JSON for the module-owned user setting.</typeparam>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task UpsertAsync<T>(string userId, string name, T value, bool exclusiveToUser = false, CancellationToken cancellationToken = default);
    /// <summary>Deletes only the version returned by ListAsync, preserving refreshed values.</summary>
    /// <param name="userId">Identifier of the user who owns the module setting.</param>
    /// <param name="name">Name identifying the value within the user's module settings.</param>
    /// <param name="version">Version returned when the setting was read; deletion succeeds only if it is unchanged.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task DeleteIfUnchangedAsync(string userId, string name, string version, CancellationToken cancellationToken = default);
    /// <summary>Deletes a named value belonging to the specified user in this module.</summary>
    /// <param name="userId">Identifier of the user who owns the module setting.</param>
    /// <param name="name">Name identifying the value within the user's module settings.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task DeleteAsync(string userId, string name, CancellationToken cancellationToken = default);
}
