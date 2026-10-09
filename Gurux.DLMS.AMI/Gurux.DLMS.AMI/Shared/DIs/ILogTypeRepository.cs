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
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle log groups.
    /// </summary>
    public interface ILogTypeRepository
    {
        /// <summary>
        /// List log types.
        /// </summary>
        /// <returns>Log types.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXLogType>> ListAsync(
            ListLogTypes? request = null,
            ListLogTypesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read log type details.
        /// </summary>
        /// <param name="id">The log type PublicId GUID.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXLogType> ReadAsync(Guid id, Expression<Func<GXLogType, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update log types.
        /// </summary>
        /// <param name="types">Updated log types.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXLogType> types,
            Expression<Func<GXLogType, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete log group(s).
        /// </summary>
        /// <param name="types">PublicId GUIDs of log types to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> types, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Regenerate log types.
        /// </summary>
        Task RegenerateAsync();
    }
}
