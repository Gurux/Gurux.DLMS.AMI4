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

using Gurux.DLMS.AMI.Client.Pages.User;
using Gurux.DLMS.AMI.Components.Account;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Identity;
using System.Net;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc/>
    public class BlockService
    {
        private readonly IGXHost _host;
        private readonly List<GXIpAddress> _ipAddresses = new List<GXIpAddress>();
        private readonly IEmailSender<ApplicationUser> _emailSender;

        /// <summary>        
        /// Update blocked items in constructor.
        /// </summary>
        public BlockService(IGXHost host,
            IEmailSender<ApplicationUser> emailSender)
        {
            _host = host;
            _emailSender = emailSender;
            try
            {
                Refresh();
            }
            catch (Exception)
            {
            }
        }

        private Guid[] Refresh()
        {
            GXSelectArgs args = GXSelectArgs.Select<GXIpAddress>(s => s.Id);
            args.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            args.Joins.AddLeftJoin<GXIpAddress, GXUser>(j => j.User, j => j.Id);
            var items = _host.Connection.Select<GXIpAddress>(args);
            lock (_ipAddresses)
            {
                _ipAddresses.Clear();
                _ipAddresses.AddRange(items);
            }
            return items.Select(x => x.Id).ToArray();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(IEnumerable<Guid> list)
        {
            lock (_ipAddresses)
            {
                _ipAddresses.RemoveAll(q => list.Any(w => w == q.Id));
            }
            //Remove from the database.
            GXDeleteArgs arg = GXDeleteArgs.Delete<GXIpAddress>(q => list.Any(w => w == q.Id));
            await _host.Connection.DeleteAsync(arg);
        }

        /// <inheritdoc/>
        public async Task DeleteByUserAsync(IEnumerable<string> list)
        {
            lock (_ipAddresses)
            {
                _ipAddresses.RemoveAll(q => list.Any(w => w == q.User.Id));
            }
            //Remove from the database.
            GXDeleteArgs arg = GXDeleteArgs.Delete<GXIpAddress>(q => list.Any(w => w == q.User.Id));
            await _host.Connection.DeleteAsync(arg);
        }

        /// <inheritdoc/>
        public async Task<bool> IsBlockedAsync(IPAddress? address, string? userId)
        {
            if (address == null)
            {
                return true;
            }
            bool blocked = false;
            var ip = address.ToString();
            GXIpAddress? target = null;
            //Check is IP address blocked.
            lock (_ipAddresses)
            {
                //If IP is white listed.
                var list = _ipAddresses.Where(w => w.Status == Shared.DTOs.Enums.IpAddressListStatus.Allow);
                if (list.Any())
                {
                    target = list.Where(w => w.IPAddress == ip && w.User.Id == userId).SingleOrDefault();
                    if (target == null)
                    {
                        //If user is not white listed.
                        blocked = true;
                    }
                }
                else if (_ipAddresses.Where(w => w.IPAddress == ip && w.Status == Shared.DTOs.Enums.IpAddressListStatus.Block).Any())
                {
                    //If IP is black listed.
                    blocked = true;
                }
            }
            //Update detected time when user logs in.
            if (!blocked && target == null)
            {
                if (userId != null)
                {
                    //Check if user has login from unknow IP address.
                    GXSelectArgs args = GXSelectArgs.Select<GXIpAddress>(s => new { s.Id, s.User }, w => w.IPAddress == ip);
                    args.Where.And<GXUser>(w => w.Id == userId);
                    args.Joins.AddInnerJoin<GXIpAddress, GXUser>(j => j.User, j => j.Id);
                    var addr = await _host.Connection.SingleOrDefaultAsync<GXIpAddress>(args);
                    if (addr == null)
                    {
                        //User is login from the new IP address.
                        args = GXSelectArgs.Select<GXUser>(s => new { s.Id, s.Notification }, w => w.Id == userId);
                        var user = await _host.Connection.SingleOrDefaultAsync<GXUser>(args);
                        if (user == null)
                        {
                            return blocked;
                        }
                        user.LastLogin = DateTime.Now;
                        user.ConnectionInfo = address.ToString();
                        addr = new GXIpAddress()
                        {
                            IPAddress = user.ConnectionInfo,
                            User = user,
                            CreationTime = user.LastLogin,
                            Detected = user.LastLogin,
                        };
                        await _host.Connection.InsertAsync(GXInsertArgs.Insert(addr));
                        if ((user.Notification & Shared.DTOs.Enums.UserNotification.NewIp) != 0
                            && !(_emailSender is IdentityNoOpEmailSender))
                        {
                            //TODO: Add notification email task.
                            string message = string.Format(Properties.Resources.NewLogin, address.ToString(), user.Email);
                            GXTask task = new GXTask()
                            {
                                TaskType = TaskType.Email,
                                Creator = user,
                                Data = message
                            };
                            //TODO: await _taskRepository.AddAsync([task]);
                        }
                        //TODO: _workflowHandler.Execute(typeof(UserTrigger), UserTrigger.NewLogin, user);
                    }
                }
            }
            else if (target != null)
            {
                target.Detected = DateTime.Now;
            }
            //Update last login IP address.
            if (userId != null)
            {
                var user = new GXUser
                {
                    Id = userId,
                    LastLogin = DateTime.Now,
                    ConnectionInfo = ip.ToString()
                };
                await _host.Connection.UpdateAsync(GXUpdateArgs.Update(user, u => new { u.LastLogin, u.ConnectionInfo }));
            }
            return blocked;
        }
    }
}