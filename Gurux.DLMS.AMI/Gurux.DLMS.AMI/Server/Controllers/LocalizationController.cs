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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle localization settings.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LocalizationController : ControllerBase
    {
        private readonly ILocalizationRepository _localizationRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LocalizationController(ILocalizationRepository localizationRepository)
        {
            _localizationRepository = localizationRepository;
        }

        /// <summary>
        /// User asks localization information.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<string?>> Get()
        {
            return await _localizationRepository.GetUserLanguageAsync(User, ServerHelpers.GetUserId(User));
        }

        /// <summary>
        /// List languages.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        [Authorize(Policy = GXLocalizationPolicies.View)]
        public async Task<ActionResult<ListLanguagesResponse>> Post(
            ListLanguages request, 
            CancellationToken cancellationToken)
        {
            ListLanguagesResponse ret = new ListLanguagesResponse();
            await _localizationRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }


        /// <summary>
        /// Get available languages.
        /// </summary>
        /// <returns></returns>
        [HttpPost("Languages")]
        [Authorize(Policy = GXLocalizationPolicies.View)]
        public async Task<ActionResult<GXLanguage[]>> Post()
        {
            return await _localizationRepository.GetInstalledCulturesAsync(User, false);
        }

        /// <summary>
        /// Update activate state of the cultures.
        /// </summary>
        /// <returns></returns>
        [Authorize(Policy = GXLocalizationPolicies.Edit)]
        [HttpPost("Update")]
        [HttpPost("Add")]
        public async Task<ActionResult> Post(GXLanguage[] cultures)
        {
            await _localizationRepository.UpdateCulturesAsync(User, cultures);
            return Ok();
        }

        /// <summary>
        /// Refresh localized strings.
        /// </summary>
        /// <returns></returns>
        [Authorize(Policy = GXLocalizationPolicies.Refresh)]
        [HttpPost("Refresh")]
        public async Task<ActionResult> Post(RefreshLocalization request)
        {
            await _localizationRepository.RefreshLocalizationsAsync(User, request.Languages);
            return Ok();
        }
    }
}
