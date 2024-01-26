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
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FavoriteRepository(
            IGXHost host,
            IGXEventsNotifier eventsNotifier)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> userGrouprs)
        {
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg = GXSelectArgs.Select<GXFavorite>(a => new { a.Id, a.Name, a.Path },
                q => userGrouprs.Contains(q.Id));
            List<GXFavorite> list = _host.Connection.Select<GXFavorite>(arg);
            DateTime now = DateTime.Now;
            foreach (GXFavorite it in list)
            {
                await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXFavorite>(it.Id));
            }
            await _eventsNotifier.FavoriteDelete(new string[] { userId }, list);
        }

        /// <inheritdoc />
        public async Task<GXFavorite[]> ListAsync(
            ClaimsPrincipal User,
            ListFavorites? request,
            ListFavoritesResponse? response,
            CancellationToken cancellationToken)
        {
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXFavorite>();
            arg.Joins.AddInnerJoin<GXFavorite, GXUser>(j => j.User, j => j.Id);
            arg.Where.And<GXUser>(w => w.Id == userId);
            if (request != null && request.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXFavorite>(q => GXSql.DistinctCount(q.Id));
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
                arg.OrderBy.Add<GXFavorite>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXFavorite>(q => q.CreationTime);
            }
            GXFavorite[] favorites = (await _host.Connection.SelectAsync<GXFavorite>(arg)).ToArray();
            if (response != null)
            {
                response.Favorites = favorites;
                if (response.Count == 0)
                {
                    response.Count = favorites.Length;
                }
            }
            return favorites;
        }

        /// <inheritdoc />
        public async Task<GXFavorite> ReadAsync(
         ClaimsPrincipal user,
         Guid id)
        {
            string userId = ServerHelpers.GetUserId(user);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXFavorite>();
            arg.Joins.AddInnerJoin<GXFavorite, GXUser>(j => j.User, j => j.Id);
            arg.Where.And<GXUser>(w => w.Id == userId);
            arg.Distinct = true;
            var favorite = await _host.Connection.SingleOrDefaultAsync<GXFavorite>(arg);
            if (favorite == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            return favorite;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXFavorite> favorites,
            Expression<Func<GXFavorite, object?>>? columns)
        {
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(user) };

            DateTime now = DateTime.Now;
            List<Guid> list = new List<Guid>();
            foreach (GXFavorite it in favorites)
            {
                if (string.IsNullOrEmpty(it.Path))
                {
                    throw new ArgumentException(Properties.Resources.InvalidName);
                }
                GXSelectArgs? args = null;
                string[] tmp = it.Path.Split('/');
                string? action = null;
                string? id = null;
                if (tmp.Length == 1)
                {
                    it.Type = it.Path;
                }
                else if (tmp.Length == 2)
                {
                    if (string.Compare(tmp[0], "Config", true) == 0)
                    {
                        it.Type = "Configuration";
                        it.Name = tmp[1];
                    }
                    else
                    if (string.Compare(tmp[0], "Logs", true) == 0)
                    {
                        it.Type = "Log";
                        it.Name = tmp[1];
                    }
                }
                else if (tmp.Length == 4)
                {
                    if (string.Compare(tmp[0], "Config", true) == 0)
                    {
                        it.Type = tmp[1];
                        action = tmp[2];
                        id = tmp[3];
                        if (string.Compare(it.Type, "Module") != 0)
                        {
                            Guid Id = Guid.Parse(id);
                            switch (it.Type.ToLower())
                            {
                                case "modulegroup":
                                    args = GXSelectArgs.Select<GXModuleGroup>(s => s.Name, w => w.Id == Id);
                                    break;
                                case "blockgroup":
                                    args = GXSelectArgs.Select<GXBlockGroup>(s => s.Name, w => w.Id == Id);
                                    break;
                                case "component":
                                    args = GXSelectArgs.Select<GXComponentView>(s => s.Name, w => w.Id == Id);
                                    break;
                                case "componentgroup":
                                    args = GXSelectArgs.Select<GXComponentViewGroup>(s => s.Name, w => w.Id == Id);
                                    break;
                                case "workflowgroup":
                                    args = GXSelectArgs.Select<GXWorkflowGroup>(s => s.Name, w => w.Id == Id);
                                    break;
                                case "scriptgroup":
                                    args = GXSelectArgs.Select<GXScriptGroup>(s => s.Name, w => w.Id == Id);
                                    break;
                                case "triggergroup":
                                    args = GXSelectArgs.Select<GXTriggerGroup>(s => s.Name, w => w.Id == Id);
                                    break;
                                case "manufacturergroup":
                                    args = GXSelectArgs.Select<GXManufacturerGroup>(s => s.Name, w => w.Id == Id);
                                    break;
                                case "keymanagement":
                                    args = GXSelectArgs.Select<GXKeyManagement>(s => s.Name, w => w.Id == Id);
                                    break;
                                case "gateway":
                                    args = GXSelectArgs.Select<GXGateway>(s => s.Name, w => w.Id == Id);
                                    break;
                            }
                        }
                    }
                }
                else if (tmp.Length > 2)
                {
                    it.Type = tmp[0];
                    action = tmp[1];
                    id = tmp[2];
                }
                string? target = null;
                if (it.Name != null && ClientHelpers.GetNotifications(true).Contains(it.Name.ToLower()))
                {
                    target = it.Name;
                }
                if (id != null && target != null)
                {
                    Guid? Id = null;
                    if (target != TargetType.User &&
                        target != TargetType.Module)
                    {
                        Id = Guid.Parse(id);
                    }
                    switch (target)
                    {
                        case TargetType.Device:
                            args = GXSelectArgs.Select<GXDevice>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.Object:
                            args = GXSelectArgs.Select<GXObjectTemplate>(s => s.Name);
                            args.Joins.AddInnerJoin<GXObjectTemplate, GXObject>(j => j.Id, j => j.Template);
                            args.Where.And<GXObject>(w => w.Id == Id);
                            break;
                        case TargetType.Attribute:
                            args = GXSelectArgs.Select<GXAttributeTemplate>(s => s.Name);
                            args.Joins.AddInnerJoin<GXAttributeTemplate, GXAttribute>(j => j.Id, j => j.Template);
                            args.Where.And<GXAttribute>(w => w.Id == Id);
                            break;
                        case TargetType.Value:
                            break;
                        case TargetType.Task:
                            break;
                        case TargetType.DeviceError:
                            break;
                        case TargetType.SystemLog:
                            break;
                        case TargetType.UserError:
                        case TargetType.ScheduleLog:
                            break;
                        case TargetType.WorkflowLog:
                            break;
                        case TargetType.AgentLog:
                            break;
                        case TargetType.ScriptLog:
                            break;
                        case TargetType.ModuleLog:
                            break;
                        case TargetType.DeviceAction:
                            break;
                        case TargetType.Token:
                            break;
                        case TargetType.UserAction:
                            break;
                        case TargetType.DeviceLog:
                            break;
                        case TargetType.Schedule:
                            args = GXSelectArgs.Select<GXSchedule>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.Agent:
                            args = GXSelectArgs.Select<GXAgent>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.DeviceTemplate:
                            args = GXSelectArgs.Select<GXDeviceTemplate>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.ObjectTemplate:
                            args = GXSelectArgs.Select<GXObjectTemplate>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.AttributeTemplate:
                            args = GXSelectArgs.Select<GXAttributeTemplate>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.UserGroup:
                            args = GXSelectArgs.Select<GXUserGroup>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.User:
                            args = GXSelectArgs.Select<GXUser>(s => s.UserName, w => w.Id == id);
                            break;
                        case TargetType.Configuration:
                            args = GXSelectArgs.Select<GXConfiguration>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.Module:
                            it.Name = id;
                            break;
                        case TargetType.Workflow:
                            args = GXSelectArgs.Select<GXWorkflow>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.Script:
                            args = GXSelectArgs.Select<GXScript>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.Block:
                            args = GXSelectArgs.Select<GXBlock>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.Trigger:
                            args = GXSelectArgs.Select<GXTrigger>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.TriggerGroup:
                            args = GXSelectArgs.Select<GXTriggerGroup>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.DeviceGroup:
                            args = GXSelectArgs.Select<GXDeviceGroup>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.ScheduleGroup:
                            args = GXSelectArgs.Select<GXScheduleGroup>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.AgentGroup:
                            args = GXSelectArgs.Select<GXAgentGroup>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.ScriptGroup:
                            args = GXSelectArgs.Select<GXScriptGroup>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.DeviceTemplateGroup:
                            args = GXSelectArgs.Select<GXDeviceTemplateGroup>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.ComponentView:
                            args = GXSelectArgs.Select<GXComponentView>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.ComponentViewGroup:
                            args = GXSelectArgs.Select<GXComponentViewGroup>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.Role:
                            args = GXSelectArgs.Select<GXRole>(s => s.Name, w => w.Id == id);
                            break;
                        case TargetType.Manufacturer:
                            args = GXSelectArgs.Select<GXManufacturer>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.ManufacturerGroup:
                            args = GXSelectArgs.Select<GXManufacturerGroup>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.Gateway:
                            args = GXSelectArgs.Select<GXGateway>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.GatewayGroup:
                            args = GXSelectArgs.Select<GXGatewayGroup>(s => s.Name, w => w.Id == Id);
                            break;
                        case TargetType.Performance:
                            args = GXSelectArgs.Select<GXPerformance>(s => new { s.Start, s.Target }, w => w.Id == Id);
                            break;
                    }
                }
                if (args != null)
                {
                    it.Name = await _host.Connection.SingleOrDefaultAsync<string>(args);
                    if (it.Name == null)
                    {
                        if (target == TargetType.Object)
                        {
                            //Object is not created yet.
                            return new Guid[0];
                        }
                        throw new Exception(Properties.Resources.InvalidName);
                    }
                }
                if (it.Id == Guid.Empty)
                {
                    it.CreationTime = now;
                    it.User = creator;
                    GXInsertArgs i = GXInsertArgs.Insert(it);
                    i.Exclude<GXFavorite>(q => q.Updated);
                    _host.Connection.Insert(i);
                    list.Add(it.Id);
                    //Remove user so it's not sent as part of the notify.
                    it.User = null;
                }
                else
                {
                    it.Updated = now;
                    GXUpdateArgs u = GXUpdateArgs.Update(it, columns);
                    u.Exclude<GXFavorite>(q => new { q.CreationTime, q.User });
                    _host.Connection.Update(u);
                }
            }
            await _eventsNotifier.FavoriteUpdate(new string[] { creator.Id }, favorites);
            return list.ToArray();
        }
    }
}