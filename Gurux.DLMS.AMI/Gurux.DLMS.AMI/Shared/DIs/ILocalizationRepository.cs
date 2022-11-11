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
using System.Security.Claims;

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
        Task<GXLanguage[]> ListAsync(
            ClaimsPrincipal User, 
            ListLanguages? request, 
            ListLanguagesResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read language.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Language id.</param>
        /// <returns></returns>
        Task<GXLanguage> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Get default culture for the user.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="userId">User ID.</param>
        /// <returns>Default culture.</returns>
        Task<string?> GetUserLanguageAsync(ClaimsPrincipal User, string? userId);

        /// <summary>
        /// Get installed cultures.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="activeOnly">Only active cultures are returned.</param>
        /// <returns>Installed cultures.</returns>
        Task<GXLanguage[]> GetInstalledCulturesAsync(ClaimsPrincipal User, bool activeOnly);

        /// <summary>
        /// Update active state of the cultures.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="languages">Languages to update.</param>
        Task UpdateCulturesAsync(ClaimsPrincipal User, IEnumerable<GXLanguage> languages);

        /// <summary>
        /// Get localized string.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="language">Used language</param>
        /// <param name="hash">Hash for invaliant string.</param>
        /// <returns>Localized string.</returns>
        Task<string?> GetLocalizedStringAsync(ClaimsPrincipal User, string language, int hash);

        /// <summary>
        /// Refresh Localized strings.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="languages">Languages to update.</param>
        Task RefreshLocalizationsAsync(ClaimsPrincipal User, IEnumerable<GXLanguage> languages);
    }
}
