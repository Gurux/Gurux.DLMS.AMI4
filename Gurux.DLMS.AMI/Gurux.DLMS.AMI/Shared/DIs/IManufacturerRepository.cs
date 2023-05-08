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
        Task<GXManufacturer[]> ListAsync(
            ClaimsPrincipal User,
            ListManufacturers? request,
            ListManufacturersResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read manufacturer.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Manufacturer id.</param>
        /// <returns></returns>
        Task<GXManufacturer> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Read manufacturer model information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Manufacturer id.</param>
        /// <returns></returns>
        Task<GXDeviceModel> ReadModelAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Read model version information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Manufacturer id.</param>
        /// <returns></returns>
        Task<GXDeviceVersion> ReadVersionAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update manufacturer(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="manufacturers">Updated manufacturer(s).</param>
        /// <param name="columns">Updated column(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXManufacturer> manufacturers,
            Expression<Func<GXManufacturer, object?>>? columns = null);

        /// <summary>
        /// Delete manufacturer(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="manufacturers">Manufacturer(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> manufacturers, bool delete);

        /// <summary>
        /// Get all users that can access this manufacturer.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="manufacturerId">Manufacturer id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? manufacturerId);

        /// <summary>
        /// Get all users that can access manufacturers.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="manufacturerIds">Manufacturer ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? manufacturerIds);

        /// <summary>
        /// Install device templates for the manufacturers.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="manufacturers">List of installed manufacturers.</param>
        /// <param name="models">List of installed models.</param>
        /// <param name="versions">List of installed versions.</param>
        /// <param name="settings">List of installed settings.</param>
        Task InstallAsync(ClaimsPrincipal User,
            IEnumerable<GXManufacturer>? manufacturers,
            IEnumerable<GXDeviceModel>? models,
            IEnumerable<GXDeviceVersion>? versions,
            IEnumerable<GXDeviceSettings>? settings);
    }
}
