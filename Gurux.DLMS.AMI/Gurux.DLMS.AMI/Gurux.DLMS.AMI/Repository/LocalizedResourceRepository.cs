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
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class LocalizedResourceRepository : ILocalizedResourceRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly ISystemLogRepository _systemLogRepository;
        private readonly IConfigurationRepository _configurationRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LocalizedResourceRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IUserRepository userRepository,
            IGXEventsNotifier eventsNotifier,
            IEnumTypeRepository enumTypeRepository,
            ISystemLogRepository systemLogRepository,
            IConfigurationRepository configurationRepository,
            GXPerformanceSettings performanceSettings)
        {
            var user = contextAccessor?.User;
            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _performanceSettings = performanceSettings;
            _enumTypeRepository = enumTypeRepository;
            _systemLogRepository = systemLogRepository;
            _configurationRepository = configurationRepository;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> resources)
        {
            if (!User.IsInRole(GXRoles.Admin))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXLocalizedResource>(a => a.Id, q => resources.Contains(q.Id));
            List<GXLocalizedResource> list = _host.Connection.Select<GXLocalizedResource>(arg);
            DateTime now = DateTime.Now;
            var users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
            users = NotificationRepository.GetNotifiedUsers(_host.Connection, _performanceSettings,
                users, TargetType.LocalizedResource, NotificationAction.Remove);
            if (users != null)
            {
                await _eventsNotifier.LocalizedResourceDelete(users, list);
            }
        }

        /// <inheritdoc />
        public async Task<GXLocalizedResource[]> ListAsync(
            ListLocalizedResources? request,
            ListLocalizedResourcesResponse? response,
            CancellationToken cancellationToken)
        {
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXLocalizedResource>();
            arg.Columns.Add<GXLanguage>(s => new { s.Id });
            arg.Columns.Exclude<GXLanguage>(s => new { s.Resources });
            arg.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(j => j.Language, j => j.Id);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            arg.OrderBy.Add<GXLanguage>(q => q.Id);
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXLocalizedResource>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXLocalizedResource>(q => q.Value);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXLocalizedResource>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            List<GXLocalizedResource> resources = (await _host.Connection.SelectAsync<GXLocalizedResource>(arg)).ToList();
            if (request?.Select?.Contains(TargetType.Language) == true && resources.Any())
            {
                //Get localized texts.
                var ids = resources.Select(x => x.Id).ToList();
                var hashs = resources.Select(x => x.Hash).ToList();
                arg = GXSelectArgs.SelectAll<GXLocalizedResource>();
                arg.Columns.Add<GXLanguage>(s => new { s.Id });
                arg.Columns.Exclude<GXLanguage>(s => new { s.Resources });
                arg.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(j => j.Language, j => j.Id);
                arg.Where.And<GXLanguage>(w => w.Active == true);
                arg.Where.And<GXLocalizedResource>(w => hashs.Contains(w.Hash) && !ids.Contains(w.Id));
                resources.AddRange((await _host.Connection.SelectAsync<GXLocalizedResource>(arg)));
            }
            if (response != null)
            {
                response.LocalizedResources = resources.ToArray();
                if (response.Count == 0)
                {
                    response.Count = resources.Count;
                }
            }
            return resources.ToArray();
        }

        /// <inheritdoc />
        public async Task<GXLocalizedResource> ReadAsync(Guid id)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Id == id);
            arg.Columns.Add<GXLanguage>(s => new { s.Id, s.NativeName, s.EnglishName });
            arg.Joins.AddLeftJoin<GXLocalizedResource, GXLanguage>(j => j.Language, j => j.Id);
            arg.Distinct = true;
            GXLocalizedResource resource = await _host.Connection.SingleOrDefaultAsync<GXLocalizedResource>(arg);
            if (resource == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return resource;
        }

        /// <inheritdoc />
        public async Task<GXLocalizedResource> ReadAsync(string lang, string hash, string? text)
        {
            if (string.IsNullOrEmpty(lang))
            {
                throw new ArgumentNullException("Language information is missing.");
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Hash == hash);
            arg.Where.And<GXLanguage>(w => w.Id == lang);
            arg.Columns.Add<GXLanguage>(s => new { s.Id, s.NativeName, s.EnglishName });
            arg.Columns.Exclude<GXLanguage>(e => e.Resources);
            arg.Joins.AddLeftJoin<GXLocalizedResource, GXLanguage>(j => j.Language, j => j.Id);
            arg.Distinct = true;
            GXLocalizedResource resource = await _host.Connection.SingleOrDefaultAsync<GXLocalizedResource>(arg);
            if (resource == null)
            {
                arg = GXSelectArgs.SelectAll<GXLanguage>(w => w.Id == lang);
                var l = await _host.Connection.SingleOrDefaultAsync<GXLanguage>(arg);
                if (l?.Active != true)
                {
                    //Return value for the default default if language is not installed.
                    arg = GXSelectArgs.SelectAll<GXLanguage>(w => w.Default == true);
                    l = await _host.Connection.SingleOrDefaultAsync<GXLanguage>(arg);
                    return await ReadAsync(l.Id, hash, text);
                }
                if (!string.IsNullOrEmpty(text))
                {
                    //If the resource is Unknown.
                    GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
                    GXSystemLog log = new GXSystemLog(TraceLevel.Error);
                    log.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.SystemLog, TargetType.LocalizedResource);
                    log.Message = string.Format("Localized resource '{0}' is missing for {1}.", text, lang);
                    await _systemLogRepository.AddAsync(TargetType.LocalizedResource, [log]);
                    //Create Unknown resource.                    
                    resource = new GXLocalizedResource()
                    {
                        Status = await _enumTypeRepository.GetLogTypeAsync(TargetType.LocalizedResource, "Unknown"),
                        Language = l,
                        Hash = hash,
                        CreationTime = DateTime.Now,
                        Creator = creator,
                        Value = text,
                    };
                    await UpdateAsync([resource]);
                }
            }
            return resource;
        }

        /// <summary>
        /// Set the new update time.
        /// </summary>
        private async Task Update()
        {
            ListConfiguration req = new ListConfiguration()
            {
                Filter = new GXConfiguration()
                {
                    Name = GXConfigurations.LocalizedResource
                }
            };
            GXConfiguration[] confs = await _configurationRepository.ListAsync(req, null, CancellationToken.None);
            foreach (GXConfiguration conf in confs)
            {
                if (conf.Name == GXConfigurations.LocalizedResource && !string.IsNullOrEmpty(conf.Settings))
                {
                    LanguageSettings? s = JsonSerializer.Deserialize<LanguageSettings>(conf.Settings);
                    if (s != null)
                    {
                        s.Updated = DateTime.Now;
                        conf.Settings = JsonSerializer.Serialize(s);
                        await _configurationRepository.UpdateAsync([conf], false);
                    }
                    break;
                }
            }            
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(IEnumerable<GXLocalizedResource> resources)
        {
            if (!User.IsInRole(GXRoles.Admin))
            {
                throw new UnauthorizedAccessException();
            }
            DateTime now = DateTime.Now;
            List<Guid> list = new();
            List<GXLocalizedResource> updates = new();
            foreach (GXLocalizedResource resource in resources)
            {
                if (string.IsNullOrEmpty(resource.Value))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                if (string.IsNullOrEmpty(resource.Language?.Id))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                //Get resource ID.
                var arg = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Id == resource.Id);
                GXLocalizedResource? tmp = await _host.Connection.SingleOrDefaultAsync<GXLocalizedResource>(arg);
                if (tmp?.Id != null)
                {
                    resource.Id = tmp.Id;
                }
                if (resource.Id == Guid.Empty)
                {
                    GXInsertArgs args = GXInsertArgs.Insert(resource);
                    resource.Hash = ClientHelpers.GetHashCode(resource.Value);
                    //Check that hash doesn't alredy exists.
                    GXSelectArgs m = GXSelectArgs.Select<GXLocalizedResource>(q => q.Id, where => where.Hash == resource.Hash);
                    m.Joins.AddRightJoin<GXLocalizedResource, GXLanguage>(j => j.Language, j => j.Id);
                    m.Where.And<GXLanguage>(w => w.Id == resource.Language.Id);
                    GXLocalizedResource? exists = _host.Connection.SingleOrDefault<GXLocalizedResource>(m);
                    if (exists != null)
                    {
                        throw new ArgumentException("Localized string already exists.");
                    }
                    _host.Connection.Insert(args);
                    list.Add(resource.Id);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXLocalizedResource>(q => new { q.ConcurrencyStamp }, where => where.Id == resource.Id);
                    GXLocalizedResource? old = _host.Connection.SingleOrDefault<GXLocalizedResource>(m);
                    if (old == null || !string.IsNullOrEmpty(old.ConcurrencyStamp) && old.ConcurrencyStamp != resource.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    resource.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(resource);
                    //Hash is not updated after the creation.
                    if (User.IsInRole(GXRoles.Admin) && resource.Creator?.Id != null)
                    {
                        args.Exclude<GXLocalizedResource>(e => new { e.CreationTime, e.Hash });
                    }
                    else
                    {
                        args.Exclude<GXLocalizedResource>(e => new { e.CreationTime, e.Creator, e.Hash });
                    }
                    _host.Connection.Update(args);
                    updates.Add(new GXLocalizedResource()
                    {
                        Id = resource.Id,
                        Hash = resource.Hash,
                        Value = resource.Value,
                    });
                }
            }
            if (updates.Any())
            {
                await Update();
                var users = await _userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
                await _eventsNotifier.LocalizedResourceUpdate(users, updates);
            }
            return list.ToArray();
        }

        /// <inheritdoc />
        public async Task<DateTimeOffset?> LastChanged()
        {
            ListConfiguration req = new ListConfiguration()
            {
                Filter = new GXConfiguration()
                {
                    Name = GXConfigurations.LocalizedResource
                }
            };
            GXConfiguration[] confs = await _configurationRepository.ListAsync(req, null, CancellationToken.None);
            foreach (GXConfiguration conf in confs)
            {
                if (conf.Name == GXConfigurations.LocalizedResource && !string.IsNullOrEmpty(conf.Settings))
                {
                    LanguageSettings? s = JsonSerializer.Deserialize<LanguageSettings>(conf.Settings);
                    if (s != null)
                    {
                        return s.Updated;
                    }
                    break;
                }
            }
            return null;
        }
    }
}
