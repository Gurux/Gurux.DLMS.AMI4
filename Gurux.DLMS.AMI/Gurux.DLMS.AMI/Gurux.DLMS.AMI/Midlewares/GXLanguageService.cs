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

using Gurux.DLMS.AMI.Components;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    /// <summary>
    /// This class is used to get browser language.
    /// </summary>
    public class GXLanguageService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="httpContextAccessor">Http context accessor.</param>
        /// <param name="serviceProvider">Service provider.</param>
        public GXLanguageService(IHttpContextAccessor httpContextAccessor,
                    IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Get user language.
        /// </summary>
        /// <returns>Returns null if english is used.</returns>
        public async Task<string> GetUserLanguageAsync(ClaimsPrincipal user)
        {
            string? id = ServerHelpers.GetUserId(user, false);
            if (id != null)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var ur = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                    if (ur != null)
                    {
                        var user2 = await ur.ReadAsync(id);
                        if (!string.IsNullOrEmpty(user2.Language))
                        {
                            return user2.Language;
                        }
                    }
                }
            }
            string value = GetBrowserLanguage();
            if (!string.IsNullOrEmpty(value) && value.Length > 2)
            {
                value = value.Substring(0, 2);
            }
            return value;
        }

        /// <summary>
        /// Get browser language.
        /// </summary>
        /// <returns>Returns null if english is used.</returns>
        private string GetBrowserLanguage()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request?.Headers.TryGetValue("Accept-Language", out var languages) == true)
            {
                return languages.ToString().Split(',').FirstOrDefault() ?? "en";
            }
            //Default language.
            return "en";
        }
    }
}
