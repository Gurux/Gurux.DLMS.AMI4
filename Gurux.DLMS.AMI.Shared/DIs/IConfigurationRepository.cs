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
// This file is a part of Gurux Task Framework.
//
// Gurux Task Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Task Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// Conficurationn update notification.
    /// </summary>
    /// <param name="configurations">Modified configurations.</param>
    public delegate void ConfigurationModifiedEventHandler(IEnumerable<GXConfiguration> configurations);


    /// <summary>
    /// This interface is used to handle configurations.
    /// </summary>
    public interface IConfigurationRepository
    {
        /// <summary>
        /// List configuration settings.
        /// </summary>
        /// <param name="user">The user making the request.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Tasks.</returns>
        Task<IEnumerable<GXConfiguration>> ListAsync(
            ClaimsPrincipal user,
            ListConfiguration? request = null,
            ListConfigurationResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read configuration details.
        /// </summary>
        /// <param name="user">The user making the request.</param>
        /// <param name="id">Configuration id.</param>
        /// <param name="culture">Used culture.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXConfiguration> ReadAsync(ClaimsPrincipal user, Guid id, string? culture, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new configuration settings or updates existing configuration settings.
        /// </summary>
        /// <param name="user">The user making the request.</param>
        /// <param name="configurations">The records to insert or update.</param>
        /// <param name="notification">Whether to publish change notifications after saving the configurations.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task UpsertAsync(
            ClaimsPrincipal user,
            IEnumerable<GXConfiguration> configurations,
            bool notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete configuration(s).
        /// </summary>
        /// <param name="user">The user making the request.</param>
        /// <param name="configurations">Deleted configuration(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> configurations, CancellationToken cancellationToken = default);

        /// <summary>
        /// Configuration has been updated.
        /// </summary>
        event ConfigurationModifiedEventHandler? Updated;

        /// <summary>
        /// Returns a list from changed database schemas.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>List of tables with schema changes.</returns>
        Task<IEnumerable<string>?> GetTablesWithSchemaChangesAsync(CancellationToken cancellationToken = default);
        /// <summary>Returns descriptions of pending schema changes grouped by table.</summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task<IEnumerable<GXConfigurationTableChange>> GetTableChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Regenerate configurations.
        /// </summary>
        /// <param name="user">The user making the request.</param>
        /// <param name="configurations">Configurations to regenerate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <remarks>
        /// A null or empty selection replaces all configurations. A nonempty selection resets only the given
        /// built-in configurations, preserving their IDs. Each key can be a configuration name or ID string.
        /// Configurations without built-in defaults are rejected before any changes.
        /// </remarks>
        Task RegenerateAsync(ClaimsPrincipal user, IEnumerable<string>? configurations, CancellationToken cancellationToken = default);

    }
}
