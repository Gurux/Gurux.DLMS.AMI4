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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to import and export the data.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DataExchangeController : ControllerBase
    {
        private readonly IDataExchange _dataExchangeRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DataExchangeController(IDataExchange dataExchangeRepository)
        {
            _dataExchangeRepository = dataExchangeRepository;
        }


        /// <summary>
        /// Return available data types.
        /// </summary>
        /// <returns>Available data types.</returns>
        [Authorize(Policy = GXUserPolicies.View)]
        [HttpGet()]
        public async Task<ActionResult<GXDataExchangeTypeResponse>> Get()
        {
            return await _dataExchangeRepository.AvailableTypes(null);
        }

        /// <summary>
        /// Return available data types.
        /// </summary>
        /// <param name="data">Imported data.</param>
        /// <returns>Available data types.</returns>
        [Authorize(Policy = GXUserPolicies.View)]
        [HttpPost("AvailableTypes")]
        public async Task<ActionResult<GXDataExchangeTypeResponse>> Post(GXDataExchangeType data)
        {
            return await _dataExchangeRepository.AvailableTypes(data);
        }

        /// <summary>
        /// Import data.
        /// </summary>
        /// <param name="data">Imported data.</param>
        /// <returns>Imported data.</returns>
        [Authorize(Policy = GXUserPolicies.View)]
        [HttpPost("Import")]
        public async Task<ActionResult<GXImportDataResponse>> Post(GXImportData data)
        {
            return await _dataExchangeRepository.ImportDataAsync(data);
        }

        /// <summary>
        /// Export data.
        /// </summary>
        /// <param name="filter">Exported data filter.</param>
        /// <returns>Exported data.</returns>
        [HttpPost("Export")]
        [Authorize(Policy = GXUserPolicies.View)]
        public async Task<ActionResult<GXExportDataResponse>> Post(GXExportData filter)
        {
            return await _dataExchangeRepository.ExportDataAsync(filter);
        }
    }
}
