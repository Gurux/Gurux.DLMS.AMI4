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
    [System.ComponentModel.Description("Conficurationn update notification.")]
    public delegate void ConfigurationModifiedEventHandler(IEnumerable<GXConfiguration> configurations);


    /// <summary>
    /// This interface is used to handle configurations.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle configurations.")]
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
        [System.ComponentModel.Description("List configuration settings.")]
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
        /// <returns>A task whose result contains the requested configuration.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read configuration details.")]
        Task<GXConfiguration> ReadAsync(ClaimsPrincipal user, Guid id, string? culture, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new configuration settings or updates existing configuration settings.
        /// </summary>
        /// <param name="user">The user making the request.</param>
        /// <param name="configurations">The records to insert or update.</param>
        /// <param name="notification">Whether to publish change notifications after saving the configurations.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to insert new configuration settings or update existing configuration settings.</returns>
        [System.ComponentModel.Description("Inserts new configuration settings or updates existing configuration settings.")]
        Task UpsertAsync(
            ClaimsPrincipal user,
            IEnumerable<GXConfiguration> configurations,
            bool notification, CancellationToken cancellationToken = default);

        /// <summary>Delete configurations.</summary>
        /// <param name="user">The user making the request.</param>
        /// <param name="configurations">Deleted configuration(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete configurations.</returns>
        [System.ComponentModel.Description("Delete configurations.")]
        Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> configurations, CancellationToken cancellationToken = default);

        /// <summary>
        /// Configuration has been updated.
        /// </summary>
        [System.ComponentModel.Description("Configuration has been updated.")]
        event ConfigurationModifiedEventHandler? Updated;

        /// <summary>
        /// Returns a list from changed database schemas.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>List of tables with schema changes.</returns>
        [System.ComponentModel.Description("Returns a list from changed database schemas.")]
        Task<IEnumerable<string>?> GetTablesWithSchemaChangesAsync(CancellationToken cancellationToken = default);
        /// <summary>Returns descriptions of pending schema changes grouped by table.</summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task whose result describes pending schema changes for each table.</returns>
        [System.ComponentModel.Description("Returns descriptions of pending schema changes grouped by table.")]
        Task<IEnumerable<GXConfigurationTableChange>> GetTableChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>Inserts missing built-in configurations, preserving existing settings.</summary>
        /// <returns>A task representing completion of the request to insert missing built-in configurations, preserving existing settings.</returns>
        /// <param name="user">The caller whose permissions authorize the configuration changes.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("Inserts missing built-in configurations, preserving existing settings.")]
        Task EnsureDefaultsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

        /// <summary>Resets selected built-in configurations in place by name or identifier. An empty selection is rejected.</summary>
        /// <returns>A task representing completion of the request to reset selected built-in configurations in place by name or identifier. An empty selection is rejected.</returns>
        /// <param name="user">The caller whose permissions authorize the configuration changes.</param>
        /// <param name="configurations">Names of the built-in configuration settings to reset. The selection must not be empty.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("Resets selected built-in configurations in place by name or identifier. An empty selection is rejected.")]
        Task ResetToDefaultsAsync(ClaimsPrincipal user, IEnumerable<string> configurations, CancellationToken cancellationToken = default);
    }
}
