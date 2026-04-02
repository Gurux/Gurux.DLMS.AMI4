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
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class AppearanceRepository : IAppearanceRepository
    {
        private readonly ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly ISystemLogRepository _systemLogRepository;
        private readonly IEnumTypeRepository _enumTypeRepository;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IWebHostEnvironment _env;
        private IReadOnlyList<GXAppearance>? _cache;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AppearanceRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            ISystemLogRepository systemLogRepository,
            IEnumTypeRepository enumTypeRepository,
            IGXEventsNotifier eventsNotifier,
            IConfigurationRepository configurationRepository,
            IWebHostEnvironment env,
            GXPerformanceSettings performanceSettings)
        {
            User = contextAccessor.User;
            _host = host;
            _eventsNotifier = eventsNotifier;
            _systemLogRepository = systemLogRepository;
            _enumTypeRepository = enumTypeRepository;
            _configurationRepository = configurationRepository;
            _env = env;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<string> appearances, bool delete)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.AppearanceManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXAppearance>(a => a.Id, q => appearances.Contains(q.Id));
            List<GXAppearance> list = _host.Connection.Select<GXAppearance>(arg);
            DateTime now = DateTime.Now;
            foreach (GXAppearance it in list)
            {
                it.Removed = now;
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXAppearance>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
            }
            await _eventsNotifier.AppearanceDelete(null, list);
        }

        /// <inheritdoc />
        public async Task<GXAppearance[]> ListAsync(
                ListAppearances? request,
                ListAppearancesResponse? response,
                CancellationToken cancellationToken)
        {
            //All appearances are available can see all the appearances.
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAppearance>();
            arg.Joins.AddLeftJoin<GXAppearance, GXUser>(j => j.Creator, j => j.Id);
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            if (request?.Exclude != null && request.Exclude.Any())
            {
                arg.Where.And<GXAppearance>(w => !request.Exclude.Contains(w.Id));
            }
            if (request?.Included != null && request.Included.Any())
            {
                arg.Where.And<GXAppearance>(w => request.Included.Contains(w.Id));
            }
            if (request?.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (!string.IsNullOrEmpty(request?.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXAppearance>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXAppearance>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXAppearance>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            List<GXAppearance> appearances = (await _host.Connection.SelectAsync<GXAppearance>(arg)).ToList();
            if (response != null)
            {
                response.Appearances = appearances.ToArray();
                if (response.Count == 0)
                {
                    response.Count = appearances.Count;
                }
            }
            return appearances.ToArray();
        }

        private static string UnknownImage = "<svg width=\"16\" height=\"16\" viewBox=\"0 0 16 16\" xmlns=\"http://www.w3.org/2000/svg\">\r\n  <!-- Box -->\r\n  <rect x=\"1\" y=\"1\" width=\"14\" height=\"14\" stroke=\"black\" stroke-width=\"1\" fill=\"none\" />\r\n  \r\n  <!-- Cross -->\r\n  <line x1=\"1\" y1=\"1\" x2=\"15\" y2=\"15\" stroke=\"black\" stroke-width=\"1\" />\r\n  <line x1=\"15\" y1=\"1\" x2=\"1\" y2=\"15\" stroke=\"black\" stroke-width=\"1\" />\r\n</svg>\r\n";

        /// <inheritdoc />
        public async Task<GXAppearance> ReadAsync(int type, string id)
        {
            if (type < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAppearance>(w => w.ResourceType == type && w.Id == id);
            arg.Joins.AddLeftJoin<GXAppearance, GXUser>(j => j.Creator, j => j.Id);
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            GXAppearance appearance = await _host.Connection.SingleOrDefaultAsync<GXAppearance>(arg);
            if (appearance == null && type == (byte)ResourceType.Image)
            {
                if (User == null || ServerHelpers.GetUserId(User, false) == null)
                {
                    //Create Unknown icon.                    
                    appearance = new GXAppearance()
                    {
                        Id = "Unknown",
                        CreationTime = DateTime.Now,
                        ResourceType = (byte)ResourceType.Image,
                        Value = UnknownImage,
                    };
                    return appearance;
                }
                var adminId = ServerSettings.GetDefaultAdminUser(_host);
                GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(adminId) };
                GXSystemLog log = new GXSystemLog(TraceLevel.Error);
                log.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.SystemLog, TargetType.Appearance);
                log.Message = string.Format("Image '{0}' is missing.", id);
                await _systemLogRepository.AddAsync("Image", [log]);
                arg = GXSelectArgs.SelectAll<GXAppearance>(w => w.Id == "Unknown" &&
                                        (w.ResourceType == (UInt32)ResourceType.Image));
                appearance = await _host.Connection.SingleOrDefaultAsync<GXAppearance>(arg);
                if (appearance == null)
                {
                    //If Unknown image is missing.
                    log = new GXSystemLog(TraceLevel.Error);
                    log.Type = await _enumTypeRepository.GetLogTypeAsync(TargetType.SystemLog, TargetType.Appearance);
                    log.Message = string.Format("Image '{0}' is missing.", id);
                    await _systemLogRepository.AddAsync("Image", [log]);
                    //Create Unknown icon.                    
                    appearance = new GXAppearance()
                    {
                        Id = "Unknown",
                        CreationTime = DateTime.Now,
                        Creator = creator,
                        ResourceType = (byte)ResourceType.Image,
                        Value = UnknownImage,
                    };
                    await _host.Connection.InsertAsync(GXInsertArgs.Insert(appearance));
                }
                else
                {
                    //Create missing icon.
                    appearance = new GXAppearance()
                    {
                        Id = id,
                        CreationTime = DateTime.Now,
                        Creator = creator,
                        ResourceType = (byte)ResourceType.Image,
                        Value = UnknownImage,
                    };
                    await _host.Connection.InsertAsync(GXInsertArgs.Insert(appearance));
                }
            }
            return appearance;
        }

        /// <inheritdoc />
        public async Task<bool> RefreshAsync(bool force, GXAppearance? filter)
        {
            //TODO:
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<string[]> UpdateAsync(
            IEnumerable<GXAppearance> appearances,
            Expression<Func<GXAppearance, object?>>? columns)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.AppearanceManager)))
            {
                throw new UnauthorizedAccessException();
            }
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<string> list = new();
            List<GXAppearance> list2 = new();
            foreach (GXAppearance appearance in appearances)
            {
                if (string.IsNullOrEmpty(appearance.Id))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                list2.Add(new GXAppearance()
                {
                    Id = appearance.Id,
                    Active = appearance.Active,
                    ResourceType = appearance.ResourceType
                });
                if (string.IsNullOrEmpty(appearance.Id))
                {
                    appearance.Creator = creator;
                    appearance.CreationTime = now;
                    GXInsertArgs args = GXInsertArgs.Insert(appearance);
                    args.Exclude<GXAppearance>(q => new
                    {
                        q.Updated,
                        q.Removed
                    });
                    _host.Connection.Insert(args);
                    list.Add(appearance.Id);
                }
                else
                {
                    GXSelectArgs m = GXSelectArgs.Select<GXAppearance>(q => q.ConcurrencyStamp, where => where.Id == appearance.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != appearance.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    appearance.Updated = now;
                    appearance.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs args = GXUpdateArgs.Update(appearance, columns);
                    args.Exclude<GXAppearance>(q => new
                    {
                        q.CreationTime,
                    });
                    if (!User.IsInRole(GXRoles.Admin) ||
                        appearance.Creator == null ||
                        string.IsNullOrEmpty(appearance.Creator.Id))
                    {
                        //Only admin can update the creator.
                        args.Exclude<GXAppearance>(q => q.Creator);
                    }
                    if (appearance.ResourceType == (byte)ResourceType.Theme && appearance.Active == true)
                    {
                        //Only one theme can be active at the time. Disable other themes.
                        m = GXSelectArgs.Select<GXAppearance>(q => q.Id, w => w.ResourceType == (byte)ResourceType.Theme && w.Active == true);
                        var actives = _host.Connection.Select<GXAppearance>(m);
                        foreach (var it in actives)
                        {
                            it.Active = false;
                            GXUpdateArgs u = GXUpdateArgs.Update(it, u => u.Active);
                            _host.Connection.Update(u);
                        }
                    }
                    _host.Connection.Update(args);
                }
                await Update(appearance.ResourceType.Value);
            }
            await _eventsNotifier.AppearanceUpdate(null, list2);
            return list.ToArray();
        }

        /// <summary>
        /// Set the new update time.
        /// </summary>
        private async Task Update(byte type)
        {
            ListConfiguration req = new ListConfiguration()
            {
                Filter = new GXConfiguration()
                {
                    Name = GXConfigurations.Appearance
                }
            };
            GXConfiguration[] confs = _configurationRepository.ListAsync(req, null, CancellationToken.None).Result;
            foreach (GXConfiguration conf in confs)
            {
                if (conf.Name == GXConfigurations.Appearance && !string.IsNullOrEmpty(conf.Settings))
                {
                    AppearanceSettings? s = JsonSerializer.Deserialize<AppearanceSettings>(conf.Settings);
                    if (s != null)
                    {
                        s.Updated = DateTime.Now;
                        if (type == (byte)ResourceType.Theme)
                        {
                            s.ThemeUpdated = s.Updated;
                        }
                        else if (type == (byte)ResourceType.Iconpack)
                        {
                            s.IconpackUpdated = s.Updated;
                        }
                        conf.Settings = JsonSerializer.Serialize(s);
                        await _configurationRepository.UpdateAsync([conf], false);
                    }
                    break;
                }
            }
        }

        /// <inheritdoc />
        public async Task<DateTimeOffset?> LastChanged(byte type)
        {
            ListConfiguration req = new ListConfiguration()
            {
                Filter = new GXConfiguration()
                {
                    Name = GXConfigurations.Appearance
                }
            };
            GXConfiguration[] confs = await _configurationRepository.ListAsync(req, null, CancellationToken.None);
            foreach (GXConfiguration conf in confs)
            {
                if (conf.Name == GXConfigurations.Appearance && !string.IsNullOrEmpty(conf.Settings))
                {
                    AppearanceSettings? s = JsonSerializer.Deserialize<AppearanceSettings>(conf.Settings);
                    if (s != null)
                    {
                        if (type == (byte)ResourceType.Theme)
                        {
                            return s.ThemeUpdated;
                        }
                        if (type == (byte)ResourceType.Iconpack)
                        {
                            return s.IconpackUpdated;
                        }
                        return s.Updated;
                    }
                    break;
                }
            }
            return null;
        }
    }
}
