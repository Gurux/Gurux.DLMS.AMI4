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

using System.Linq.Expressions;
using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;

using static Gurux.DLMS.AMI.Server.Internal.ServerHelpers;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class LocalizationRepository : ILocalizationRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IUserRepository _userRepository;
        private readonly GXLanguageService _languageService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAuthorizationService _authorizationService;
        private readonly NotificationCache _notificationCache;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LocalizationRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor,
            IUserRepository userRepository,
            GXLanguageService languageService,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            else
            {
                User = httpContextAccessor.HttpContext.User;
            }
            _serviceProvider = serviceProvider;
            _dbFactory = dbFactory;
            _userRepository = userRepository;
            _languageService = languageService;
            _authorizationService = authorizationService;
            _notificationCache = notificationCache;
        }

        /// <inheritdoc />
        public async Task<string?> GetUserLanguageAsync(CancellationToken cancellationToken)
        {
            //User roles are not checked.
            string value = await _languageService.GetUserLanguageAsync(User);
            //Check that language is installed or return the default language.
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            var culture = CultureInfo.GetCultureInfo(value);
            GXSelectArgs args;
            GXLanguage? lang;
            while (!string.IsNullOrEmpty(culture.Name))
            {
                var name = culture.Name;
                args = GXSelectArgs.SelectAll<GXLanguage>(where => where.Enabled == true && where.Id == name);
                lang = await connection.SingleOrDefaultAsync<GXLanguage>(args);
                if (lang != null)
                {
                    return lang.Id;
                }
                culture = culture.Parent;
            }
            args = GXSelectArgs.SelectAll<GXLanguage>(where => where.Default == true && where.Enabled == true);
            lang = await connection.SingleOrDefaultAsync<GXLanguage>(args);
            if (lang != null)
            {
                return lang.Id;
            }
            return null;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXLanguage>> GetInstalledCulturesAsync(bool activeOnly, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXUserPolicies.View);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (activeOnly)
            {
                GXSelectArgs args = GXSelectArgs.SelectAll<GXLanguage>(where => where.Enabled == true);
                return [.. (await connection.SelectAsync<GXLanguage>(args))];
            }
            return [.. (await connection.SelectAllAsync<GXLanguage>(cancellationToken: cancellationToken))];
        }

        /// <inheritdoc />
        public async Task UpdateCulturesAsync(IEnumerable<GXLanguage> languages, CancellationToken cancellationToken)
        {
            languages = languages.ToArray();
            await GXHistoryMutation.RunAsync<GXLanguage>(_dbFactory, User, _serviceProvider, languages,
        async () =>
        {
            if (!User.IsInRole(GXRoles.Admin))
            {
                throw new UnauthorizedAccessException();
            }
            if (languages.Where(w => w.Default == true && w.Enabled == true).Count() != 1)
            {
                throw new ArgumentException("A default language must be selected.");
            }
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            foreach (var it in languages)
            {
                if (it.Enabled != true)
                {
                    it.Default = false;
                    //Remove localized resources when language is removed.
                    GXSelectArgs s = GXSelectArgs.Select<GXLocalizedResource>(s => s.Id);
                    s.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(j => j.Language!, j => j.Id);
                    s.Where.And<GXLanguage>(w => w.Id == it.Id);
                    List<Guid> ids = [.. connection.Select<GXLocalizedResource>(s).Select(s => s.Id)];
                    if (ids.Count != 0)
                    {
                        GXDeleteArgs d = GXDeleteArgs.Delete<GXLocalizedResource>(w => ids.Contains(w.Id));
                        await connection.DeleteAsync(d, cancellationToken);
                    }
                }
                GXUpdateArgs args = GXUpdateArgs.Update(it);
                args.Exclude<GXLanguage>(e => e.EnglishName);
                await connection.UpdateAsync(args, cancellationToken);
            }

            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                IEnumTypeRepository enumTypeRepository = scope.ServiceProvider.GetRequiredService<IEnumTypeRepository>();
                var con = _serviceProvider.GetRequiredService<ApplicationDbContext>();
                List<GXLanguage> updated = await UpdateLanguageResourcesAsync(connection,
                            enumTypeRepository,
                            languages, creator);
            }

            var users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            _notificationCache.Add(users, new GXTarget(GXLanguagePolicies.Add));
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<string?> GetLocalizedStringAsync(string language, string hash, CancellationToken cancellationToken)
        {
            if (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.Authenticated))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs args = GXSelectArgs.Select<GXLocalizedResource>(s => s.Value!, w => w.Hash == hash);
            args.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(j => j.Language!, j => j.Id);
            args.Where.And<GXLanguage>(where => where.Enabled == true && where.Id == language);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            string? ret = await connection.SingleOrDefaultAsync<string>(args);
            if (string.IsNullOrEmpty(ret))
            {
                return null;
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task RefreshLocalizationsAsync(IEnumerable<GXLanguage>? languages, CancellationToken cancellationToken)
        {
            await GXHistoryMutation.RunAllAsync<GXLanguage>(_dbFactory, User, _serviceProvider,
        async () =>
        {
            if (!User.IsInRole(GXRoles.Admin))
            {
                throw new UnauthorizedAccessException();
            }
            GXUser user = new()
            {
                Id = GetUserId(User, true)
            };
            using IServiceScope scope = _serviceProvider.CreateScope();
            IEnumTypeRepository enumTypeRepository = scope.ServiceProvider.GetRequiredService<IEnumTypeRepository>();
            var con = _serviceProvider.GetRequiredService<IDbConnectionFactory>();
            GXDbConnection connection = await con.GetHistoryConnectionAsync();
            List<GXLanguage> updated = await UpdateLanguageResourcesAsync(connection,
                        enumTypeRepository,
                        languages, user);
            if (updated.Count != 0)
            {
                var users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
                _notificationCache.Add(users, new GXTarget(GXLanguagePolicies.Edit));
            }
        }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GXLanguage>> ListAsync(
            ListLanguages? request,
            ListLanguagesResponse? response,
            CancellationToken cancellationToken = default)
        {
            //Access rights are not check because this method is used to get localized resources
            //for login page where user information is not available.
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXLanguage>();
            if (request != null && request.Filter != null)
            {
                //Get localized resources for the wanted language.
                if (request.Filter.Resources != null && request.Filter.Resources.Any())
                {
                    if (string.IsNullOrEmpty(request.Filter.Id))
                    {
                        //Get user default language if language is not given.
                        request.Filter.Id = await GetUserLanguageAsync(cancellationToken) ?? string.Empty;
                    }
                }
                arg.Where.FilterBy(request.Filter);
            }
            arg.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXLanguage>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = connection.SingleOrDefault<long>(total);
                    if (response.Count == 0)
                    {
                        //No need to retrieve data when none is available.
                        return [];
                    }
                }
                arg.Index = request.Index;
                arg.Count = request.Count;
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

            var list = (await connection.SelectAsync<GXLanguage>(arg)).ToList();
            if (request != null && request.Filter != null && !string.IsNullOrEmpty(request.Filter.Id) &&
                list.Count != 0)
            {
                //Get localized resources for the wanted language.
                if (request.Filter.Resources != null && request.Filter.Resources.Any())
                {
                    List<GXLocalizedResource> resources = [];
                    //Get hash for the asked resources.
                    foreach (var it in request.Filter.Resources)
                    {
                        string hash = ClientHelpers.GetHashCode(it.Value ?? string.Empty);
                        arg = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Hash == hash);
                        arg.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(j => j.Language!, j => j.Id);
                        arg.Where.And<GXLanguage>(w => w.Id == request.Filter.Id);
                        GXLocalizedResource? res = await connection.SingleOrDefaultAsync<GXLocalizedResource>(arg);
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
                    list[0].Resources = [.. resources];
                }
            }
            //The default language is the first language.
            var defs = list.Where(w => w.Default == true).ToList();
            list.RemoveAll(defs.Contains);
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

        /// <summary>Performs the read operation.</summary>
        /// <param name="id">The item identifier.</param>
        /// <param name="columns">The columns to select or update.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        public async Task<GXLanguage> ReadAsync(Guid id, Expression<Func<GXLanguage, object>>? columns = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
