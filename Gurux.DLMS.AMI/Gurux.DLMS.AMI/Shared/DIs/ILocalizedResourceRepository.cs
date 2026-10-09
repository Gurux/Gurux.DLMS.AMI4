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
using System.Linq.Expressions;

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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXLocalizedResource>> ListAsync(
            ListLocalizedResources? request = null,
            ListLocalizedResourcesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read localized resource.
        /// </summary>
        /// <param name="id">Localized resource id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXLocalizedResource> ReadAsync(Guid id,
            Expression<Func<GXLocalizedResource, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read localized resource.
        /// </summary>
        /// <param name="lang">Language identifier.</param>
        /// <param name="hash">Hash of localized resource.</param>
        /// <param name="text">Localized text.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXLocalizedResource> ReadAsync(string lang, string hash, string? text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update localized resource(s).
        /// </summary>
        /// <param name="localizedResources">Updated localized resource(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(IEnumerable<GXLocalizedResource> localizedResources, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete localized resource(s).
        /// </summary>
        /// <param name="localizedResources">Localized resource(s) to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> localizedResources, CancellationToken cancellationToken = default);

        /// <summary>
        /// When the localized resource was last changed.
        /// </summary>
        Task<DateTimeOffset?> LastChanged();
    }
}
