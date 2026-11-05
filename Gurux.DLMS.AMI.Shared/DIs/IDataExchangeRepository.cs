// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// Provides operations for importing and exporting AMI data.
    /// </summary>
    [System.ComponentModel.Description("Provides operations for importing and exporting AMI data.")]
    public interface IDataExchange
    {
        /// <summary>
        /// Returns the supported import and export target types.
        /// </summary>
        /// <param name="req">Request for the available data exchange types.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Available target type identifiers and localized names.</returns>
        [System.ComponentModel.Description("Returns the supported import and export target types.")]
        Task<GXDataExchangeTypeResponse> AvailableTypes(
            GXDataExchangeType? req,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Imports JSON data according to the supplied conflict rule.
        /// </summary>
        /// <param name="data">Import data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The imported target types and item counts.</returns>
        [System.ComponentModel.Description("Imports JSON data according to the supplied conflict rule.")]
        Task<GXImportDataResponse> ImportDataAsync(
            GXImportData data,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exported data.
        /// </summary>
        /// <param name="filter">Exported filter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the exported data and export response metadata.</returns>
        [System.ComponentModel.Description("Exported data.")]
        Task<GXExportDataResponse> ExportDataAsync(
            GXExportData filter,
            CancellationToken cancellationToken = default);
    }
}
