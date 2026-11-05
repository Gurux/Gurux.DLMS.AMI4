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

using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle manufacturers.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle manufacturers.")]
    public interface IManufacturerRepository
    {
        /// <summary>
        /// List manufacturers.
        /// </summary>
        /// <returns>Manufacturers.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List manufacturers.")]
        Task<IEnumerable<GXManufacturer>> ListAsync(
            ListManufacturers? request = null,
            ListManufacturersResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read manufacturer.
        /// </summary>
        /// <param name="id">Manufacturer id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested manufacturer.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read manufacturer.")]
        Task<GXManufacturer> ReadAsync(Guid id, Expression<Func<GXManufacturer, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read manufacturer model details.
        /// </summary>
        /// <param name="id">Manufacturer id.</param>
        /// <returns>A task whose result contains the requested manufacturer model.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read manufacturer model details.")]
        Task<GXDeviceModel> ReadModelAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Read model version details.
        /// </summary>
        /// <param name="id">Manufacturer id.</param>
        /// <returns>A task whose result contains the requested device model version.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read model version details.")]
        Task<GXDeviceVersion> ReadVersionAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new manufacturers or updates existing ones.
        /// </summary>
        /// <param name="manufacturers">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new manufacturers or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXManufacturer> manufacturers,
            Expression<Func<GXManufacturer, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete manufacturers.</summary>
        /// <param name="manufacturers">Manufacturer(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete manufacturers.</returns>
        [System.ComponentModel.Description("Delete manufacturers.")]
        Task DeleteAsync(IEnumerable<Guid> manufacturers, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this manufacturer.
        /// </summary>
        /// <param name="manufacturerId">Manufacturer id.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this manufacturer.")]
        Task<List<string>> GetUsersAsync(Guid? manufacturerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access manufacturers.
        /// </summary>
        /// <param name="manufacturerIds">Manufacturer ids.</param>
        /// <returns>A task whose result contains the identifiers of users with access to the specified resources.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access manufacturers.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? manufacturerIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Install device templates for the manufacturers.
        /// </summary>
        /// <param name="manufacturers">List of installed manufacturers.</param>
        /// <param name="models">List of installed models.</param>
        /// <param name="versions">List of installed versions.</param>
        /// <param name="settings">List of installed settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to install device templates for the manufacturers.</returns>
        [System.ComponentModel.Description("Install device templates for the manufacturers.")]
        Task InstallAsync(
            IEnumerable<GXManufacturer>? manufacturers,
            IEnumerable<GXDeviceModel>? models,
            IEnumerable<GXDeviceVersion>? versions,
            IEnumerable<GXDeviceSettings>? settings,
            CancellationToken cancellationToken = default);
    }
}
