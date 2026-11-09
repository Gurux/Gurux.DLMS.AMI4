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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to import and export the data.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class DataExchangeController(IDataExchange dataExchangeRepository) : ControllerBase
    {
        private readonly IDataExchange _dataExchangeRepository = dataExchangeRepository;


        /// <summary>
        /// Return available data types.
        /// </summary>
        /// <returns>Available data types.</returns>
        [Authorize(Policy = GXUserPolicies.View)]
        [HttpGet()]
        public async Task<ActionResult<GXDataExchangeTypeResponse>> Get(CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return await _dataExchangeRepository.AvailableTypes(null, cancellationToken);
        }

        /// <summary>
        /// Return available data types.
        /// </summary>
        /// <param name="data">Imported data.</param>
        /// <returns>Available data types.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [Authorize(Policy = GXUserPolicies.View)]
        [HttpPost("AvailableTypes")]
        public async Task<ActionResult<GXDataExchangeTypeResponse>> Post(GXDataExchangeType data, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return await _dataExchangeRepository.AvailableTypes(data, cancellationToken);
        }

        /// <summary>
        /// Import data.
        /// </summary>
        /// <param name="data">Imported data.</param>
        /// <returns>Imported data.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [Authorize(Policy = GXUserPolicies.View)]
        [HttpPost("Import")]
        public async Task<ActionResult<GXImportDataResponse>> Post(GXImportData data, CancellationToken cancellationToken = default)
        {
            return await _dataExchangeRepository.ImportDataAsync(data, cancellationToken);
        }

        /// <summary>
        /// Export data.
        /// </summary>
        /// <param name="filter">Exported data filter.</param>
        /// <returns>Exported data.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Export")]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<GXExportDataResponse>> Post(GXExportData filter, CancellationToken cancellationToken = default)
        {
            return await _dataExchangeRepository.ExportDataAsync(filter, cancellationToken);
        }
    }
}
