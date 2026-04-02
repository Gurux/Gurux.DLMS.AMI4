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

using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Security.Claims;
using static Gurux.DLMS.AMI.Server.Internal.ServerHelpers;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class LocalizationRepository : ILocalizationRepository
    {
        private readonly ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly GXLanguageService _languageService;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LocalizationRepository(IGXHost host,
            IServiceProvider serviceProvider,
            IGXAmiContextAccessor contextAccessor,
            IUserRepository userRepository,
            GXLanguageService languageService,
            IGXEventsNotifier eventsNotifier)
        {
            var user = contextAccessor?.User;
            _serviceProvider = serviceProvider;
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _languageService = languageService;
        }

        /// <inheritdoc />
        public async Task<string?> GetUserLanguageAsync()
        {
            string value = await _languageService.GetUserLanguageAsync(User);
            //Check that language is installed or return the default language.
            GXSelectArgs args = GXSelectArgs.SelectAll<GXLanguage>(where => where.Active == true && where.Id == value);
            GXLanguage? lang = await _host.Connection.SingleOrDefaultAsync<GXLanguage>(args);
            if (lang != null)
            {
                return lang.Id;
            }
            args = GXSelectArgs.SelectAll<GXLanguage>(where => where.Default == true && where.Active == true);
            lang = await _host.Connection.SingleOrDefaultAsync<GXLanguage>(args);
            if (lang != null)
            {
                return lang.Id;
            }
            return null;
        }

        /// <inheritdoc />
        public async Task<GXLanguage[]> GetInstalledCulturesAsync(bool activeOnly)
        {
            if (activeOnly)
            {
                GXSelectArgs args = GXSelectArgs.SelectAll<GXLanguage>(where => where.Active == true);
                return (await _host.Connection.SelectAsync<GXLanguage>(args)).ToArray();
            }
            return (await _host.Connection.SelectAllAsync<GXLanguage>()).ToArray();
        }

        /// <inheritdoc />
        public async Task UpdateCulturesAsync(IEnumerable<GXLanguage> languages)
        {
            if (languages.Where(w => w.Default == true && w.Active == true).Count() != 1)
            {
                throw new ArgumentException("A default language must be selected.");
            }
            foreach (var it in languages)
            {
                if (it.Active != true)
                {
                    it.Default = false;
                    //Remove localized resources when language is removed.
                    GXSelectArgs s = GXSelectArgs.Select<GXLocalizedResource>(s => s.Id);
                    s.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(j => j.Language, j => j.Id);
                    s.Where.And<GXLanguage>(w => w.Id == it.Id);
                    List<Guid> ids = _host.Connection.Select<GXLocalizedResource>(s).Select(s => s.Id).ToList();
                    if (ids.Any())
                    {
                        GXDeleteArgs d = GXDeleteArgs.Delete<GXLocalizedResource>(w => ids.Contains(w.Id));
                        await _host.Connection.DeleteAsync(d);
                    }
                }
                GXUpdateArgs args = GXUpdateArgs.Update(it);
                args.Exclude<GXLanguage>(e => e.EnglishName);
                await _host.Connection.UpdateAsync(args);
            }

            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
                IEnumTypeRepository enumTypeRepository = scope.ServiceProvider.GetRequiredService<IEnumTypeRepository>();
                List<GXLanguage> updated = await UpdateLanguageResourcesAsync(_host,
                            enumTypeRepository,
                            languages, creator);
            }

            var users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            await _eventsNotifier.LanguageUpdate(users, languages);
        }

        /// <inheritdoc />
        public async Task<string?> GetLocalizedStringAsync(string language, string hash)
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
        public async Task RefreshLocalizationsAsync(IEnumerable<GXLanguage>? languages)
        {
            if (User == null || !User.IsInRole(GXRoles.Admin))
            {
                throw new UnauthorizedAccessException();
            }
            GXUser user = new GXUser
            {
                Id = GetUserId(User, true)
            };
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IEnumTypeRepository enumTypeRepository = scope.ServiceProvider.GetRequiredService<IEnumTypeRepository>();
                List<GXLanguage> updated = await UpdateLanguageResourcesAsync(_host,
                            enumTypeRepository,
                            languages, user);
                if (updated.Any())
                {
                    var users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
                    await _eventsNotifier.LanguageUpdate(users, updated);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<GXLanguage[]> ListAsync(
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
                        request.Filter.Id = await GetUserLanguageAsync();
                    }
                }
                arg.Where.FilterBy(request.Filter);
            }
            arg.Distinct = true;
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
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXLanguage>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXLanguage>(q => q.Id);
                arg.Descending = true;
            }

            var list = (await _host.Connection.SelectAsync<GXLanguage>(arg)).ToList();
            if (request != null && request.Filter != null && !string.IsNullOrEmpty(request.Filter.Id) &&
                list.Any())
            {
                //Get localized resources for the wanted language.
                if (request.Filter.Resources != null && request.Filter.Resources.Any())
                {
                    List<GXLocalizedResource> resources = new List<GXLocalizedResource>();
                    //Get hash for the asked resources.
                    foreach (var it in request.Filter.Resources)
                    {
                        string hash = ClientHelpers.GetHashCode(it.Value);
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
                    list[0].Resources = resources.ToArray();
                }
            }
            //The default language is the first language.
            var defs = list.Where(w => w.Default == true).ToList();
            list.RemoveAll(w => defs.Contains(w));
            list.InsertRange(0, defs);
            var languages = list.ToArray();
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

        public async Task<GXLanguage> ReadAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
