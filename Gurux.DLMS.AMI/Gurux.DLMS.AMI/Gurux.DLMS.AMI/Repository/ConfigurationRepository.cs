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
using Gurux.DLMS.AMI.Server.Midlewares;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ConfigurationRepository : IConfigurationRepository
    {
        private ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<IdentityOptions> _identityOptions;

        /// <summary>
        /// Configuration has been modified.
        /// </summary>
        private ConfigurationModifiedEventHandler? _modified;

        /// <summary>
        /// Configuration has been modified.
        /// </summary>
        public event ConfigurationModifiedEventHandler? Updated
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
            IGXAmiContextAccessor contextAccessor,
            IServiceProvider serviceProvider,
            IOptions<IdentityOptions> identityOptions,
            IGXEventsNotifier eventsNotifier)
        {
            User = contextAccessor?.User;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _serviceProvider = serviceProvider;
            _identityOptions = identityOptions;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> tasks)
        {
            //TODO: Implement delete.
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<GXConfiguration[]> ListAsync(
            ListConfiguration? request,
            ListConfigurationResponse? response,
            CancellationToken cancellationToken)
        {
            if (User == null)
            {
                //User is null when server is started at the first time.
                User = ServerSettings.GetDefaultAdminUser(_host);
                if (User == null)
                {
                    return [];
                }
            }
            //Only admin can access configurations.
            if ((!User.IsInRole(GXRoles.Admin)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXConfiguration>();
            if (request?.Exclude != null && request.Exclude.Any())
            {
                arg.Where.And<GXConfiguration>(w => !request.Exclude.Contains(w.Id));
            }
            if (request?.Included != null && request.Included.Any())
            {
                arg.Where.And<GXConfiguration>(w => request.Included.Contains(w.Id));
            }
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
            return configurations;
        }

        /// <inheritdoc />
        public async Task<GXConfiguration> ReadAsync(
            Guid id,
            string? culture)
        {
            if (User == null)
            {
                //User is null when server is started at the first time.
                User = ServerSettings.GetDefaultAdminUser(_host);
            }
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXConfiguration>(where => where.Id == id);
            GXConfiguration conf = await _host.Connection.SingleOrDefaultAsync<GXConfiguration>(arg);
            if (conf == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return conf;
        }

        /// <inheritdoc />
        public async Task UpdateAsync(
            IEnumerable<GXConfiguration> configurations,
            bool notification)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin)))
            {
                throw new UnauthorizedAccessException();
            }
            GXUpdateArgs update = GXUpdateArgs.UpdateRange(configurations);
            update.Exclude<GXConfiguration>(q => new { q.Name, q.Description, q.CreationTime, q.Path });
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
            }

            if (notification)
            {
                if (notification && _modified != null)
                {
                    _modified(configurations);
                }
                List<string> users;
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                    if (User.IsInRole(GXRoles.Admin))
                    {
                        users = await userRepository.GetUserIdsInRoleAsync([GXRoles.Admin]);
                    }
                    else
                    {
                        users = new List<string>();
                    }
                }
                List<GXConfiguration> tmp = new List<GXConfiguration>();
                foreach (var it in configurations)
                {
                    tmp.Add(new GXConfiguration()
                    {
                        Id = it.Id,
                        Name = it.Name,
                        Settings = it.Settings,
                        Updated = it.Updated,
                    });
                }
                //Only IDs are notified for security reasons.
                await _eventsNotifier.ConfigurationSave(users, tmp);
            }
        }
    }
}