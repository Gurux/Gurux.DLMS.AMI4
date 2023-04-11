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
        /// <returns>Tasks.</returns>
        Task<GXConfiguration[]> ListAsync(
            ClaimsPrincipal User,
            ListConfiguration? request,
            ListConfigurationResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read configuration information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Configuration id.</param>
        /// <param name="culture">Used culture.</param>
        /// <returns></returns>
        Task<GXConfiguration> ReadAsync(ClaimsPrincipal User, Guid id, string? culture);

        /// <summary>
        /// Add or update configuration settings.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="configurations">Updated configuration(s).</param>
        /// <param name="notify">Is update notified.</param>
        Task UpdateAsync(
        ClaimsPrincipal User,
            IEnumerable<GXConfiguration> configurations,
            bool notify);

        /// <summary>
        /// Delete configuration(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="configurations">Deleted configuration(s).</param>
        Task DeleteAsync(
            ClaimsPrincipal User,
            IEnumerable<Guid> configurations);

        /// <summary>
        /// Configuration has been updated.
        /// </summary>
        event ConfigurationModifiedEventHandler? Updated;

    }
}
