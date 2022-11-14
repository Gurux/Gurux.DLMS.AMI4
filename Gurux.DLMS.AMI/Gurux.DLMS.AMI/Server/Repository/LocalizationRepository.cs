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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Security.Claims;
using static Gurux.DLMS.AMI.Server.Internal.ServerHelpers;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class LocalizationRepository : ILocalizationRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LocalizationRepository(IGXHost host,
                    IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task<string?> GetUserLanguageAsync(ClaimsPrincipal User, string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                userId = ServerHelpers.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return null;
                }
            }
            GXSelectArgs args = GXSelectArgs.SelectById<GXUser>(userId, s => s.Language);
            GXUser user = await _host.Connection.SingleOrDefaultAsync<GXUser>(args);
            if (user == null || string.IsNullOrEmpty(user.Language))
            {
                //Return default language.
                args = GXSelectArgs.SelectAll<GXLanguage>(where => where.Default == true && where.Active == true);
                GXLanguage? lang = await _host.Connection.SingleOrDefaultAsync<GXLanguage>(args);
                if (lang != null)
                {
                    return lang.Id;
                }
                return null;
            }
            return user.Language;
        }

        /// <inheritdoc />
        public async Task<GXLanguage[]> GetInstalledCulturesAsync(ClaimsPrincipal User, bool activeOnly)
        {
            if (activeOnly)
            {
                GXSelectArgs args = GXSelectArgs.SelectAll<GXLanguage>(where => where.Active == true);
                return (await _host.Connection.SelectAsync<GXLanguage>(args)).ToArray();
            }
            return (await _host.Connection.SelectAllAsync<GXLanguage>()).ToArray();
        }

        /// <inheritdoc />
        public async Task UpdateCulturesAsync(ClaimsPrincipal User, IEnumerable<GXLanguage> languages)
        {
            foreach (var it in languages)
            {
                GXUpdateArgs args = GXUpdateArgs.Update(it);
                args.Exclude<GXLanguage>(e => e.EnglishName);
                await _host.Connection.UpdateAsync(args);
            }
            var users = await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin });
            await _eventsNotifier.LanguageUpdate(users, languages);
        }

        /// <inheritdoc />
        public async Task<string?> GetLocalizedStringAsync(ClaimsPrincipal User, string language, int hash)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXLocalizedResource>(s => s.Value, w => w.Hash == hash);
            args.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(j => j.Language, j => j.Id);
            args.Where.And<GXLanguage>(where => where.Active == true && where.Id == language);
            string? ret = await _host.Connection.SingleOrDefaultAsync<string>(args);
            if (string.IsNullOrEmpty(ret))
            {
                return null;
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task RefreshLocalizationsAsync(ClaimsPrincipal User, IEnumerable<GXLanguage> languages)
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<GXConfiguration>();
            var configurations = await _host.Connection.SelectAsync<GXConfiguration>(args);
            foreach (GXConfiguration configuration in configurations)
            {
                GXLanguageResourceArgs arg = new GXLanguageResourceArgs(_host, languages, typeof(Properties.Resources),
                    configuration, null, null, null);
                List<GXLanguage> updated = await UpdateLanguageResourcesAsync(arg);
                if (updated.Any())
                {
                    var users = await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin });
                    await _eventsNotifier.LanguageUpdate(users, updated);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<GXLanguage[]> ListAsync(
            ClaimsPrincipal User,
            ListLanguages? request,
            ListLanguagesResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXLanguage>();
            if (request != null && request.Filter != null)
            {
                //Get localized resources for the wanted language.
                if (request.Filter.Resources != null && request.Filter.Resources.Any())
                {
                    if (string.IsNullOrEmpty(request.Filter.Id))
                    {
                        //Get user default language if language is not given.
                        request.Filter.Id = await GetUserLanguageAsync(User, null);
                    }
                }
                arg.Where.FilterBy(request.Filter);
            }
            arg.Distinct = true;
            arg.Descending = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXLanguage>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            arg.OrderBy.Add<GXLanguage>(q => q.Id);
            GXLanguage[] languages = (await _host.Connection.SelectAsync<GXLanguage>(arg)).ToArray();
            if (request != null && request.Filter != null && !string.IsNullOrEmpty(request.Filter.Id) &&
                languages.Any())
            {
                //Get localized resources for the wanted language.
                if (request.Filter.Resources != null && request.Filter.Resources.Any())
                {
                    List<GXLocalizedResource> resources = new List<GXLocalizedResource>();
                    //Get hash for the asked resources.
                    foreach (var it in request.Filter.Resources)
                    {
                        int hash = ServerHelpers.GetHashCode(it.Value);
                        arg = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Hash == hash);
                        arg.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(j => j.Language, j => j.Id);
                        arg.Where.And<GXLanguage>(w => w.Id == request.Filter.Id);
                        GXLocalizedResource? res = await _host.Connection.SingleOrDefaultAsync<GXLocalizedResource>(arg);
                        if (res == null)
                        {
                            //Return asked value if localized string is not found.
                            resources.Add(it);
                        }
                        else
                        {
                            resources.Add(res);
                        }
                    }
                    languages[0].Resources = resources.ToArray();
                }
            }
            if (response != null)
            {
                response.Languages = languages;
                if (response.Count == 0)
                {
                    response.Count = languages.Length;
                }
            }
            return languages;
        }

        public async Task<GXLanguage> ReadAsync(ClaimsPrincipal user, Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
