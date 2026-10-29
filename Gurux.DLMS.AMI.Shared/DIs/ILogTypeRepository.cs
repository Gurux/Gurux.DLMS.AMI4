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

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// Provides persistence operations for log type definitions.
    /// </summary>
    [System.ComponentModel.Description("Provides persistence operations for log type definitions.")]
    public interface ILogTypeRepository
    {
        /// <summary>Adds missing built-in log types without changing existing types or access policies.</summary>
        /// <returns>A task representing completion of the request to add missing built-in log types without changing existing types or access policies.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

        /// <summary>Restores selected built-in log types and their default access policies, preserving identifiers and logs.</summary>
        /// <param name="ids">Public identifiers of the built-in log types to reset.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task representing completion of the request to restore selected built-in log types and their default access policies, preserving identifiers and logs.</returns>
        Task ResetToDefaultsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// List log types.
        /// </summary>
        /// <returns>Log types.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List log types.")]
        Task<IEnumerable<GXLogType>> ListAsync(
            ListLogTypes? request = null,
            ListLogTypesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read log type details.
        /// </summary>
        /// <param name="id">The log type PublicId GUID.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested log type.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read log type details.")]
        Task<GXLogType> ReadAsync(Guid id, Expression<Func<GXLogType, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new log types or updates existing ones.
        /// </summary>
        /// <param name="types">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new log types or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXLogType> types,
            Expression<Func<GXLogType, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes log type definitions.
        /// </summary>
        /// <param name="types">PublicId GUIDs of log types to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete log type definitions.</returns>
        [System.ComponentModel.Description("Deletes log type definitions.")]
        Task DeleteAsync(IEnumerable<Guid> types, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

    }
}
