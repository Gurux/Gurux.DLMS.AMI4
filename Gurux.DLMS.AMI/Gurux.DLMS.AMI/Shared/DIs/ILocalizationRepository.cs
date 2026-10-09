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

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle localization.
    /// </summary>
    public interface ILocalizationRepository
    {
        /// <summary>
        /// List languages.
        /// </summary>
        /// <returns>Languages.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXLanguage>> ListAsync(
            ListLanguages? request = null,
            ListLanguagesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read language.
        /// </summary>
        /// <param name="id">Language id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXLanguage> ReadAsync(Guid id, Expression<Func<GXLanguage, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get default culture for the user.
        /// </summary>
        /// <returns>User culture.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<string?> GetUserLanguageAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get installed cultures.
        /// </summary>
        /// <param name="activeOnly">Only active cultures are returned.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Installed cultures.</returns>
        Task<IEnumerable<GXLanguage>> GetInstalledCulturesAsync(bool activeOnly, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update active state of the cultures.
        /// </summary>
        /// <param name="languages">Languages to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task UpdateCulturesAsync(IEnumerable<GXLanguage> languages, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get localized string.
        /// </summary>
        /// <param name="language">Used language</param>
        /// <param name="hash">Hash for invaliant string.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Localized string.</returns>
        Task<string?> GetLocalizedStringAsync(string language, string hash, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh Localized strings.
        /// </summary>
        /// <param name="languages">Languages to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RefreshLocalizationsAsync(IEnumerable<GXLanguage>? languages, CancellationToken cancellationToken = default);
    }
}
