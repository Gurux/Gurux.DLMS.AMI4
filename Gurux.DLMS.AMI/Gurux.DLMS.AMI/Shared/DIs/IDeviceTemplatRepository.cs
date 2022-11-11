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

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// </summary>
    public interface IDeviceTemplateRepository
    {
        /// <summary>
        /// List device templates.
        /// </summary>
        /// <returns>Device templates.</returns>
        Task<GXDeviceTemplate[]> ListAsync(
            ClaimsPrincipal user,
            ListDeviceTemplates? request,
            ListDeviceTemplatesResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read device template information.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Device template id.</param>
        /// <returns></returns>
        Task<GXDeviceTemplate> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update device template.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="templates">Updated device templates.</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXDeviceTemplate> templates);

        /// <summary>
        /// Delete device template(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="templates">Device templates to delete.</param>
        Task DeleteAsync(
            ClaimsPrincipal user,
            IEnumerable<Guid> templates);

        /// <summary>
        /// Get all users that can access device template.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="deviceTemplateId">Device template id.</param>
        /// <returns>User Ids that can access device template.</returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, Guid? deviceTemplateId);

        /// <summary>
        /// Get all users that can access given device templates.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="deviceTemplateIds">Device template ids.</param>
        /// <returns>User Ids that can access device template.</returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal User, IEnumerable<Guid>? deviceTemplateIds);
    }
}
