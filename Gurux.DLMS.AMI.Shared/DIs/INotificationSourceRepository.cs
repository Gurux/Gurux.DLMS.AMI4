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

using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// Lists registered notification sources and manages their server-wide delivery settings.
    /// </summary>
    [System.ComponentModel.Description("Lists registered notification sources and manages their server-wide delivery settings.")]
    public interface INotificationSourceRepository
    {
        /// <summary>Lists registered notification sources matching the request.</summary>
        /// <param name="request">Optional filtering, ordering, and paging parameters.</param>
        /// <param name="response">Optional response to populate with the selected sources and total matching count before paging.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the selected notification sources, including their delivery settings.</returns>
        [System.ComponentModel.Description("Lists registered notification sources and their server-wide delivery settings.")]
        Task<IEnumerable<GXNotificationSource>> ListAsync(
            ListNotificationSources? request = null,
            ListNotificationSourcesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>Enables or disables server-wide delivery for the specified registered sources.</summary>
        /// <remarks>
        /// Requires permission to manage server-wide notification settings.
        /// Changes only the delivery setting; source names and producer ownership are preserved.
        /// The implementation must refresh the server notification cache after successful persistence.
        /// Enabling a source does not bypass recipient authorization or subscription and delivery channel settings.
        /// </remarks>
        /// <param name="ids">Identifiers of the registered notification sources to update.</param>
        /// <param name="enabled">Whether delivery is permitted for the selected sources.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the settings update.</returns>
        [System.ComponentModel.Description("Enables or disables server-wide delivery for selected registered notification sources.")]
        Task SetEnabledAsync(
            IEnumerable<int> ids,
            bool enabled,
            CancellationToken cancellationToken = default);
    }
}
