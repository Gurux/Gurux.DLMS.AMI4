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

using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Server.Internal;

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Hubs
{
    /// <summary>
    /// GuruxAMI Hub sends notification events.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Authorize]
    public class GuruxAMiHub(IDbConnectionFactory dbFactory,
        IGXConnectionManager connectionManager, IServiceProvider services, IAuthorizationService authorization) : Hub<IGXHubEvents>
    {
        internal static readonly GXAgentConnectionRegistry AgentConnections = new();
        /// <summary>Returns the private notification group for a registered agent.</summary>
        public static string AgentGroup(Guid agentId) => "agent:" + agentId.ToString("D");
        private readonly IDbConnectionFactory _dbFactory = dbFactory;
        private readonly IGXConnectionManager _connectionManager = connectionManager;

        /// <summary>
        /// Save occurred exception to the database.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                await AgentConnections.DisconnectAsync(Context.ConnectionId, UpdateAgentPresenceAsync);
                if (exception != null)
                {
                    await using var connection = await _dbFactory.GetConnectionAsync();
                    GXLog log = new() { Message = exception.Message };
                    await GXLogStore.InsertAsync(connection, "system", [log]);
                }
            }
            finally
            {
                _connectionManager.RemoveConnection(Context.ConnectionId);
                await base.OnDisconnectedAsync(exception);
            }
        }
        /// <summary>Associates this authenticated connection with an agent the caller can edit and view.</summary>
        [Authorize]
        public async Task RegisterAgent(Guid agentId)
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated != true || agentId == Guid.Empty || user.HasClaim("banned", "true"))
                {
                throw new HubException("Agent registration is not authorized.");
            }
            if (!(await authorization.AuthorizeAsync(user, null, GXAgentPolicies.View)).Succeeded ||
                !(await authorization.AuthorizeAsync(user, null, GXAgentPolicies.Edit)).Succeeded)
                {
                throw new HubException("Agent registration is not authorized.");
            }
            await using var connection = await _dbFactory.GetConnectionAsync(Context.ConnectionAborted);
            var userId = ServerHelpers.GetUserId(user);
            var membership = GXRepositoryUsers.Query(typeof(GXAgent), agentId);
            membership.Where.And<GXUser>(u => u.Id == userId);
            if (await connection.SingleOrDefaultAsync<GXUser>(membership, Context.ConnectionAborted) == null)
                {
                throw new HubException("Agent registration is not authorized.");
            }
            var agent = GXSelectArgs.Select<GXAgent>(a => a.Id,
                a => a.Id == agentId && a.Removed == null && a.Enabled == true);
            if (await connection.SingleOrDefaultAsync<GXAgent>(agent, Context.ConnectionAborted) == null)
                {
                throw new HubException("Agent registration is not authorized.");
            }
            Context.ConnectionAborted.ThrowIfCancellationRequested();
            try
            {
                await AgentConnections.RegisterAsync(agentId, Context.ConnectionId, UpdateAgentPresenceAsync,
                    user.FindFirst("jti")?.Value);
            }
            catch (GXAgentConnectionInUseException error)
            {
                throw new HubException(error.Message);
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, AgentGroup(agentId), Context.ConnectionAborted);
        }

        private Task UpdateAgentPresenceAsync(Guid agentId, bool connected)
            => services.RunAsUserAsync(Context.User ?? new ClaimsPrincipal(), async scoped =>
            {
                var repository = (AgentRepository)scoped.GetRequiredService<IAgentRepository>();
                await repository.UpdateConnectionStatusAsync(agentId, Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString(), connected);
            });
        /// <summary>
        /// Client has establish a connection to the hub.
        /// </summary>
        public override Task OnConnectedAsync()
        {
            //If user is banned.
            var isBanned = Context.User?.HasClaim("banned", "true") == true;
            if (isBanned)
            {
                Context.Abort();
                return Task.CompletedTask;
            }
            var http = Context.GetHttpContext();
            var userId =
                Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                Context.User?.FindFirst("sub")?.Value;

            var email =
                Context.User?.FindFirst(ClaimTypes.Name)?.Value;

            var ip = http?.Connection.RemoteIpAddress?.ToString();
            var userAgent = http?.Request.Headers.UserAgent.ToString();

            var info = new GXConnectionInfo
            {
                ConnectionId = Context.ConnectionId,
                UserId = userId,
                IpAddress = ip,
                UserAgent = userAgent,
                Connected = DateTime.Now,
                Email = email
            };
            _connectionManager.AddConnection(info);
            return base.OnConnectedAsync();
        }
    }
}
