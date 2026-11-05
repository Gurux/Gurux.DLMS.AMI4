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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle localization.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle localization.")]
    public interface ILocalizationRepository
    {
        /// <summary>
        /// List languages.
        /// </summary>
        /// <returns>Languages.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List languages.")]
        Task<IEnumerable<GXLanguage>> ListAsync(
            ListLanguages? request = null,
            ListLanguagesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read language.
        /// </summary>
        /// <param name="id">Language id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested language.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read language.")]
        Task<GXLanguage> ReadAsync(Guid id, Expression<Func<GXLanguage, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get default culture for the user.
        /// </summary>
        /// <returns>User culture.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get default culture for the user.")]
        Task<string?> GetUserLanguageAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get installed cultures.
        /// </summary>
        /// <param name="activeOnly">Only active cultures are returned.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Installed cultures.</returns>
        [System.ComponentModel.Description("Get installed cultures.")]
        Task<IEnumerable<GXLanguage>> GetInstalledCulturesAsync(bool activeOnly, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update active state of the cultures.
        /// </summary>
        /// <param name="languages">Languages to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to update active state of the cultures.</returns>
        [System.ComponentModel.Description("Update active state of the cultures.")]
        Task UpdateCulturesAsync(IEnumerable<GXLanguage> languages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get localized string.
        /// </summary>
        /// <param name="language">Used language</param>
        /// <param name="hash">Hash for invaliant string.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Localized string.</returns>
        [System.ComponentModel.Description("Get localized string.")]
        Task<string?> GetLocalizedStringAsync(string language, string hash, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh Localized strings.
        /// </summary>
        /// <param name="languages">Languages to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to refresh Localized strings.</returns>
        [System.ComponentModel.Description("Refresh Localized strings.")]
        Task RefreshLocalizationsAsync(IEnumerable<GXLanguage>? languages, CancellationToken cancellationToken = default);
    }
}
