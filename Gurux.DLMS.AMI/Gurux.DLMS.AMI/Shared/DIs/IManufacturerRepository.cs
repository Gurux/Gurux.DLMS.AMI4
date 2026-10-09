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
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle manufacturers.
    /// </summary>
    public interface IManufacturerRepository
    {
        /// <summary>
        /// List manufacturers.
        /// </summary>
        /// <returns>Manufacturers.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXManufacturer>> ListAsync(
            ListManufacturers? request = null,
            ListManufacturersResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read manufacturer.
        /// </summary>
        /// <param name="id">Manufacturer id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXManufacturer> ReadAsync(Guid id, Expression<Func<GXManufacturer, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read manufacturer model details.
        /// </summary>
        /// <param name="id">Manufacturer id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXDeviceModel> ReadModelAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Read model version details.
        /// </summary>
        /// <param name="id">Manufacturer id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXDeviceVersion> ReadVersionAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update manufacturer(s).
        /// </summary>
        /// <param name="manufacturers">Updated manufacturer(s).</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXManufacturer> manufacturers,
            Expression<Func<GXManufacturer, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete manufacturer(s).
        /// </summary>
        /// <param name="manufacturers">Manufacturer(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> manufacturers, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this manufacturer.
        /// </summary>
        /// <param name="manufacturerId">Manufacturer id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid? manufacturerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access manufacturers.
        /// </summary>
        /// <param name="manufacturerIds">Manufacturer ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? manufacturerIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Install device templates for the manufacturers.
        /// </summary>
        /// <param name="manufacturers">List of installed manufacturers.</param>
        /// <param name="models">List of installed models.</param>
        /// <param name="versions">List of installed versions.</param>
        /// <param name="settings">List of installed settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task InstallAsync(
            IEnumerable<GXManufacturer>? manufacturers,
            IEnumerable<GXDeviceModel>? models,
            IEnumerable<GXDeviceVersion>? versions,
            IEnumerable<GXDeviceSettings>? settings,
            CancellationToken cancellationToken = default);
    }
}
