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
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle user properties.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle user properties.")]
    public interface IUserPropertiesRepository
    {
        /// <summary>
        /// List user properties.
        /// </summary>
        /// <returns>List of user properties.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List user properties.")]
        Task<IEnumerable<GXUserProperty>> ListAsync(
            ListUserProperties? request = null,
            ListUserPropertiesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read user properties.
        /// </summary>
        /// <param name="name">Property name.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>User property.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="userId">Property owner; defaults to the current user. Another owner requires administrator access.</param>
        [System.ComponentModel.Description("Read user properties.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068", Justification = "Preserves the existing public repository signature and positional callers.")]
        Task<GXUserProperty?> ReadAsync(string name, Expression<Func<GXUserProperty, object>>? columns = null, CancellationToken cancellationToken = default, string? userId = null);

        /// <summary>
        /// Inserts new user properties or updates existing ones.
        /// </summary>
        /// <param name="settings">The user properties to insert or update, matched by owner and name. Module-owned properties must be saved through the module interface.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to insert new user properties or update existing ones.</returns>
        [System.ComponentModel.Description("Inserts new user properties or updates existing ones.")]
        Task UpsertAsync(
            IEnumerable<GXUserProperty> settings,
            Expression<Func<GXUserProperty, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete user settings.
        /// </summary>
        /// <param name="ids">User settings to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete user settings.</returns>
        [System.ComponentModel.Description("Delete user settings.")]
        Task DeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    }
}
