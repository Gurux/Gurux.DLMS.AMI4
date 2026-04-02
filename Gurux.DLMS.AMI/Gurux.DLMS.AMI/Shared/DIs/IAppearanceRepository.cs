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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

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
        /// <returns>Appearances.</returns>
        Task<GXAppearance[]> ListAsync(
            ListAppearances? request,
            ListAppearancesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read appearance.
        /// </summary>
        /// <param name="type">Appearance type.</param>
        /// <param name="id">Appearance id.</param>
        /// <returns></returns>
        Task<GXAppearance> ReadAsync(int type, string id);

        /// <summary>
        /// Update appearance(s).
        /// </summary>
        /// <param name="appearances">Updated appearance(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<string[]> UpdateAsync(
            IEnumerable<GXAppearance> appearances,
            Expression<Func<GXAppearance, object?>>? columns = null);

        /// <summary>
        /// Delete appearance(s).
        /// </summary>
        /// <param name="appearances">Appearance(s) to delete.</param>
        /// <param name="delete">If true, appearances are deleted, not marked as removed.</param>
        Task DeleteAsync(IEnumerable<string> appearances, bool delete);

        /// <summary>
        /// Refresh appearance(s).
        /// </summary>
        /// <param name="force">Refresh system appearances are reload.</param>
        /// <param name="filter">Refreshed appearance(s).</param>
        /// <returns>True, if there are new appearances.</returns>
        Task<bool> RefreshAsync(bool force, GXAppearance? filter);

        /// <summary>
        /// When the appearance was last changed.
        /// </summary>
        /// <param name="type">Appearance type</param>
        Task<DateTimeOffset?> LastChanged(byte type);
    }
}
