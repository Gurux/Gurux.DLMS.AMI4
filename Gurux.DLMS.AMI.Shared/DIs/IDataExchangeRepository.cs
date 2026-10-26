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

using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// Provides operations for importing and exporting AMI data.
    /// </summary>
    public interface IDataExchange
    {
        /// <summary>
        /// Returns the supported import and export target types.
        /// </summary>
        /// <param name="req">Request for the available data exchange types.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Available target type identifiers and localized names.</returns>
        Task<GXDataExchangeTypeResponse> AvailableTypes(
            GXDataExchangeType? req,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Imports JSON data according to the supplied conflict rule.
        /// </summary>
        /// <param name="data">Import data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The imported target types and item counts.</returns>
        Task<GXImportDataResponse> ImportDataAsync(
            GXImportData data,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exported data.
        /// </summary>
        /// <param name="filter">Exported filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXExportDataResponse> ExportDataAsync(
            GXExportData filter,
            CancellationToken cancellationToken = default);
    }
}
