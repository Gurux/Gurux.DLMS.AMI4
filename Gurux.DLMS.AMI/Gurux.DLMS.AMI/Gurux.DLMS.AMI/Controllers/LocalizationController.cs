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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle localization settings.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class LocalizationController(ILocalizationRepository localizationRepository, Gurux.DLMS.AMI.Update.GXLanguageCatalog languageCatalog) : ControllerBase
    {
        private readonly ILocalizationRepository _localizationRepository = localizationRepository;

        /// <summary>Lists installable application languages from Gurux.Updater alongside installed state.</summary>
        [HttpGet("Available")]
        [Authorize(Policy = GXLocalizationPolicies.View)]
        public async Task<Gurux.DLMS.AMI.Client.Rest.GXAvailableLanguage[]> GetAvailable(CancellationToken cancellationToken)
        {
            var available = await languageCatalog.ListAsync(cancellationToken);
            var installed = (await _localizationRepository.GetInstalledCulturesAsync(false, cancellationToken))
                .Select(l => l.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var item in available) {
                item.Installed |= installed.Contains(item.Id);
            }
            return available;
        }


        /// <summary>
        /// User asks localization information.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<string?>> Get(CancellationToken cancellationToken = default)
        {
            string language = await _localizationRepository.GetUserLanguageAsync(cancellationToken) ?? "en";
            return Content(language, "text/plain");
        }

        /// <summary>
        /// Returns list from languages.
        /// </summary>
        [HttpPost("List")]
        [AllowAnonymous]
        public async Task<ActionResult<ListLanguagesResponse>> Post(
            ListLanguages request,
            CancellationToken cancellationToken)
        {
            ListLanguagesResponse ret = new();
            await _localizationRepository.ListAsync(request, ret, cancellationToken);
            return ret;
        }


        /// <summary>
        /// Get available languages.
        /// </summary>
        /// <returns></returns>
        [HttpPost("Languages")]
        [Authorize(Policy = GXLocalizationPolicies.View)]
        public async Task<ActionResult<ListLanguagesResponse>> Post(CancellationToken cancellationToken = default)
        {
            ListLanguagesResponse ret = new()
            {
                Languages = await _localizationRepository.GetInstalledCulturesAsync(false, cancellationToken)
            };
            return ret;
        }

        /// <summary>
        /// Update activate state of the cultures.
        /// </summary>
        /// <returns></returns>
        [Authorize(Policy = GXLocalizationPolicies.Edit)]
        [HttpPost("Update")]
        [HttpPost("Add")]
        public async Task<ActionResult> Post(UpdateLocalization request, CancellationToken cancellationToken = default)
        {
            await _localizationRepository.UpdateCulturesAsync(request.Localizations, cancellationToken);
            return Ok();
        }

        /// <summary>
        /// Refresh localized strings.
        /// </summary>
        /// <returns></returns>
        [Authorize(Policy = GXLocalizationPolicies.Refresh)]
        [HttpPost("Refresh")]
        public async Task<ActionResult> Post(RefreshLocalization request, CancellationToken cancellationToken = default)
        {
            await _localizationRepository.RefreshLocalizationsAsync(request.Languages, cancellationToken);
            return Ok();
        }
    }
}
