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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle localized resources.
    /// </summary>
    public interface ILocalizedResourceRepository
    {
        /// <summary>
        /// List localized resources.
        /// </summary>
        /// <returns>LocalizedResources.</returns>
        Task<GXLocalizedResource[]> ListAsync(
            ListLocalizedResources? request,
            ListLocalizedResourcesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read localized resource.
        /// </summary>
        /// <param name="id">Localized resource id.</param>
        /// <returns></returns>
        Task<GXLocalizedResource> ReadAsync(Guid id);

        /// <summary>
        /// Read localized resource.
        /// </summary>
        /// <param name="lang">Language identifier.</param>
        /// <param name="hash">Hash of localized resource.</param>
        /// <param name="text">Localized text.</param>
        /// <returns></returns>
        Task<GXLocalizedResource> ReadAsync(string lang, string hash, string? text);

        /// <summary>
        /// Update localized resource(s).
        /// </summary>
        /// <param name="localizedResources">Updated localized resource(s).</param>
        Task<Guid[]> UpdateAsync(IEnumerable<GXLocalizedResource> localizedResources);

        /// <summary>
        /// Delete localized resource(s).
        /// </summary>
        /// <param name="localizedResources">Localized resource(s) to delete.</param>
        Task DeleteAsync(IEnumerable<Guid> localizedResources);

        /// <summary>
        /// When the localized resource was last changed.
        /// </summary>
        Task<DateTimeOffset?> LastChanged();
    }
}
