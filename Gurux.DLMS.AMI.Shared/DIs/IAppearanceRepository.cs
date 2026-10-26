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
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle appearances.
    /// </summary>
    public interface IAppearanceRepository
    {
        /// <summary>
        /// List appearances.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Appearances.</returns>
        Task<IEnumerable<GXAppearance>> ListAsync(
            ListAppearances? request = null,
            ListAppearancesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read appearance.
        /// </summary>
        /// <param name="type">Appearance type.</param>
        /// <param name="id">Appearance id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXAppearance> ReadAsync(int type, string id, Expression<Func<GXAppearance, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new appearances or updates existing ones.
        /// </summary>
        /// <param name="appearances">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<string>> UpsertAsync(
            IEnumerable<GXAppearance> appearances,
            Expression<Func<GXAppearance, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete appearance(s).
        /// </summary>
        /// <param name="appearances">Appearance(s) to delete.</param>
        /// <param name="delete">If true, appearances are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<string> appearances,
            bool delete = true,
            bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// When the appearance was last changed.
        /// </summary>
        /// <param name="type">Appearance type</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<DateTimeOffset?> LastChanged(byte type, CancellationToken cancellationToken = default);

        /// <summary>
        /// Regenerate appearance(s).
        /// </summary>
        /// <param name="type">Resource type. Image, Iconpack, Theme, Image. If null or empty, all types are regenerated.</param>
        /// <param name="appearances">Existing appearance IDs to restore, or null/empty to replace all appearances with built-in defaults.</param>
        /// <param name="overwriteExisting">If true, existing appearances will be overwritten.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RegenerateAsync(int? type, IEnumerable<string>? appearances, bool overwriteExisting = false, CancellationToken cancellationToken = default);
    }
}
