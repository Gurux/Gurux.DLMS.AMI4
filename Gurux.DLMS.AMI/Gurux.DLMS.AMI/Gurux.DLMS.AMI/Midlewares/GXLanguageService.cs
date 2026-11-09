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

using Gurux.DLMS.AMI.Components;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Globalization;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    /// <summary>
    /// This class is used to get browser language.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    /// <param name="httpContextAccessor">Http context accessor.</param>
    /// <param name="serviceProvider">Service provider.</param>
    public class GXLanguageService(IHttpContextAccessor httpContextAccessor,
                IServiceProvider serviceProvider)
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        /// <summary>
        /// Resolves the profile culture, falling back to the browser's preferred valid culture.
        /// </summary>
        /// <returns>A culture name, retaining its region when supplied.</returns>
        public async Task<string> GetUserLanguageAsync(ClaimsPrincipal user)
        {
            string? id = ServerHelpers.GetUserId(user, false);
            if (id != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var ur = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                if (ur != null)
                {
                    var user2 = await ur.ReadAsync(id);
                    if (!string.IsNullOrEmpty(user2.Language))
                    {
                        try
                        {
                            return CultureInfo.GetCultureInfo(user2.Language).Name;
                        }
                        catch (CultureNotFoundException)
                        {
                            // Invalid stored values fall back to the browser preferences.
                        }
                    }
                }
            }
            return GetBrowserLanguage();
        }

        /// <summary>
        /// Get browser language.
        /// </summary>
        /// <returns>The preferred valid browser culture, or English when none is available.</returns>
        private string GetBrowserLanguage()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                try
                {
                    var languages = request.GetTypedHeaders().AcceptLanguage;
                    foreach (var language in (languages ?? []).Where(l => (l.Quality ?? 1) > 0)
                        .OrderByDescending(l => l.Quality ?? 1))
                    {
                        var name = language.Value.Value;
                        if (string.IsNullOrWhiteSpace(name) || name == "*")
                        {
                            continue;
                        }
                        try
                        {
                            return CultureInfo.GetCultureInfo(name).Name;
                        }
                        catch (CultureNotFoundException)
                        {
                        }
                    }
                }
                catch (FormatException)
                {
                }
            }
            //Default language.
            return "en";
        }
    }
}
