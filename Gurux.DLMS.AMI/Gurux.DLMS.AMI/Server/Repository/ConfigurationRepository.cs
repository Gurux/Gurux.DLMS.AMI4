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

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Client.Shared;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserRepository _userRepository;
        private readonly IOptions<IdentityOptions> _identityOptions;

        /// <summary>
        /// Configuration has been modified.
        /// </summary>
        private ConfigurationModifiedEventHandler? _modified;

        /// <summary>
        /// Configuration has been modified.
        /// </summary>
        public event ConfigurationModifiedEventHandler Updated
        {
            add
            {
                _modified += value;
            }
            remove
            {
                _modified -= value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ConfigurationRepository(
            IGXHost host,
               IServiceProvider serviceProvider,
               IUserRepository userRepository,
               IOptions<IdentityOptions> identityOptions,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _serviceProvider = serviceProvider;
            _userRepository = userRepository;
            _identityOptions = identityOptions;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> tasks)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<GXConfiguration[]> ListAsync(
            ClaimsPrincipal User,
            ListConfiguration? request,
            ListConfigurationResponse? response,
            CancellationToken cancellationToken)
        {
            //Only admin can access configurations.
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXConfiguration>();
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0 && response != null)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXConfiguration>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                response.Count = _host.Connection.SingleOrDefault<int>(total);
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXConfiguration>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXConfiguration>(q => q.Order);
            }
            GXConfiguration[] configurations = (await _host.Connection.SelectAsync<GXConfiguration>(arg)).ToArray();
            if (response != null)
            {
                response.Configurations = configurations;
                if (response.Count == 0)
                {
                    response.Count = configurations.Length;
                }
            }
            if (User != null)
            {

                var user = await _userRepository.ReadAsync(User, ServerHelpers.GetUserId(User));
                if (!string.IsNullOrEmpty(user.Language))
                {
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        ILocalizationRepository localizationRepository = scope.ServiceProvider.GetRequiredService<ILocalizationRepository>();
                        //Get localized strings.
                        foreach (var configuration in configurations)
                        {
                            string? value = await localizationRepository.GetLocalizedStringAsync(User, user.Language, ServerHelpers.GetHashCode(configuration.Name));
                            if (value != null)
                            {
                                configuration.Name = value;
                            }
                            value = await localizationRepository.GetLocalizedStringAsync(User, user.Language, ServerHelpers.GetHashCode(configuration.Description));
                            if (value != null)
                            {
                                configuration.Description = value;
                            }
                        }
                    }
                }
            }
            return configurations;
        }

        /// <inheritdoc />
        public async Task<GXConfiguration> ReadAsync(ClaimsPrincipal User, Guid id, string? culture)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXConfiguration>(where => where.Id == id);
            GXConfiguration conf = await _host.Connection.SingleOrDefaultAsync<GXConfiguration>(arg);
            if (conf == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get localized strings.
            arg = GXSelectArgs.Select<GXLanguage>(c => new { c.Id });
            arg.Columns.Add<GXLocalizedResource>();
            arg.Columns.Exclude<GXLocalizedResource>(e => e.Language);
            arg.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(y => y.Language, x => x.Id);
            arg.Where.And<GXLocalizedResource>(w => w.Configuration == conf);
            conf.Languages = (await _host.Connection.SelectAsync<GXLanguage>(arg)).ToArray();
            return conf;
        }

        /// <inheritdoc />
        public async Task UpdateAsync(ClaimsPrincipal User, IEnumerable<GXConfiguration> configurations, bool notify)
        {
            GXUpdateArgs update = GXUpdateArgs.UpdateRange(configurations);
            update.Exclude<GXConfiguration>(q => new { q.Name, q.Description, q.CreationTime, q.Path, q.Resources });
            await _host.Connection.UpdateAsync(update);
            MaintenanceSettings? settings = null;
            foreach (var conf in configurations)
            {
                //Update security settings.
                if (conf.Name == GXConfigurations.Security && conf.Settings != null)
                {
                    SecuritySettings? o = JsonSerializer.Deserialize<SecuritySettings>(conf.Settings);
                    if (o != null)
                    {
                        ServerSettings.UpdateSecuritySettings(_identityOptions.Value, o);
                    }
                    continue;
                }

                //Update resource language parent.
                if (conf.Languages != null)
                {
                    foreach (var language in conf.Languages)
                    {
                        if (language.Resources != null)
                        {
                            foreach (var it in language.Resources)
                            {
                                it.Language = language;
                            }
                        }
                    }
                }
                //Add localized configuration strings.
                if (conf.Name == GXConfigurations.Maintenance && conf.Settings != null)
                {
                    settings = JsonSerializer.Deserialize<MaintenanceSettings>(conf.Settings);
                }
                GXSelectArgs l = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Configuration == conf);
                List<GXLocalizedResource> resources = await _host.Connection.SelectAsync<GXLocalizedResource>(l);
                List<GXLocalizedResource> tmp;
                if (conf.Languages != null)
                {
                    tmp = conf.Languages.SelectMany(s => s.Resources).ToList();
                }
                else
                {
                    tmp = new List<GXLocalizedResource>();
                }
                var comparer2 = new LocalizationHashComparer();
                List<GXLocalizedResource> removedResources = resources.Except(tmp, comparer2).ToList();
                List<GXLocalizedResource> addedResources = tmp.Except(resources, comparer2).ToList();
                if (removedResources.Any())
                {
                    await RemoveLocalizationStringsFromConfigurationAsync(removedResources);
                }
                if (settings != null && addedResources.Any())
                {
                    await AddLocalizationStringsToConfigurationAsync(conf, settings, addedResources);
                }
                //Get updated resource strings.                   
                var comparer3 = new LocalizationStringComparer();
                List<GXLocalizedResource> updatedResources = resources.Except(tmp, comparer3).ToList();
                if (updatedResources.Any())
                {
                    foreach (var it in updatedResources)
                    {
                        foreach (var it2 in tmp)
                        {
                            if (it.Hash == it2.Hash)
                            {
                                it.Value = it2.Value;
                                break;
                            }
                        }
                    }
                    await UpdateLocalizationStrings(updatedResources);
                }
                //reset resource language parent.
                if (conf.Languages != null)
                {
                    foreach (var language in conf.Languages)
                    {
                        if (language.Resources != null)
                        {
                            foreach (var it in language.Resources)
                            {
                                it.Language = null;
                            }
                        }
                    }
                }
            }

            if (notify)
            {
                if (notify && _modified != null)
                {
                    _modified(configurations);
                }
                var users = await _userRepository.GetUserIdsInRoleAsync(User, new string[] { GXRoles.Admin });
                await _eventsNotifier.ConfigurationSave(users, configurations);
            }
        }

        /// <summary>
        /// Add localization strings for the block.
        /// </summary>
        private async Task AddLocalizationStringsToConfigurationAsync(
            GXConfiguration configuration,
            MaintenanceSettings? settings,
            IEnumerable<GXLocalizedResource>? resources)
        {
            if (resources != null)
            {
                foreach (GXLocalizedResource it in resources)
                {
                    //Update hash.
                    if (settings.Message != null && it.Hash == ServerHelpers.GetHashCode(settings.Message))
                    {
                        it.Hash = ServerHelpers.GetHashCode(settings.Message);
                    }
                    else
                    {
                        throw new ArgumentException("Invalid hash.");
                    }
                    it.Configuration = configuration;
                }
                await _host.Connection.InsertAsync(GXInsertArgs.InsertRange(resources));
            }
        }

        /// <summary>
        /// Remove localization strings from the block.
        /// </summary>
        private async Task RemoveLocalizationStringsFromConfigurationAsync(IEnumerable<GXLocalizedResource>? resources)
        {
            if (resources != null)
            {
                await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteRange(resources));
            }
        }

        //Update block localization strings.
        private async Task UpdateLocalizationStrings(IEnumerable<GXLocalizedResource> resources)
        {
            if (resources != null)
            {
                foreach (GXLocalizedResource it in resources)
                {
                    await _host.Connection.UpdateAsync(GXUpdateArgs.Update(it, c => new { c.Value, c.Id }));
                }
            }
        }

    }
}