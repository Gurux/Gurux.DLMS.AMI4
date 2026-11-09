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

using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Internal;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Module;
using Gurux.DLMS.AMI.Services;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Gurux.DLMS.AMI.Hubs;

/// <summary>Delivers user-selected WebPush events independently of connected SignalR clients.</summary>
public sealed class GXWebPushNotifier(IServiceScopeFactory scopes, ILogger<GXWebPushNotifier> logger)
{
    /// <summary>Matches the same full permission or resource prefix selections used by Toaster.</summary>
    public static bool Matches(IEnumerable<string>? sources, GXTarget target)
    {
        var prefix = target.Type.Split('.')[0];
        return (sources ?? []).Any(s => s == target.Type || s == prefix);
    }

    /// <summary>Sends matching events only after checking the recipient's current permissions and resource policies.</summary>
    public async Task NotifyAsync(string userId, IReadOnlyList<GXTarget> events, CancellationToken token)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var sender = services.GetService<IGXNotification>();
            if (sender == null || sender is GXModuleNotificationSender && services.GetService<GXModuleRuntime>()?.GetStartupProviders().Any(p => p.Services.Contains(typeof(IGXNotification).FullName!)) != true)
            {
                return;
            }

            var users = services.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId);
            if (user == null || !user.IsApproved || await users.IsLockedOutAsync(user))
            {
                return;
            }

            var principal = await services.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>().CreateAsync(user);
            var authorization = services.GetRequiredService<IAuthorizationService>();
            await using var db = await services.GetRequiredService<IDbConnectionFactory>().GetConnectionAsync(token);
            var query = GXQuery.GetNotificationsByUser(n => "*", userId);
            query.Joins.AddInnerJoin<GXNotification, GXNotificationType>(n => n.Type!, t => t.Id);
            query.Where.And<GXNotification>(n => n.Enabled == true && n.Removed == null);
            query.Where.And<GXNotificationType>(t => t.Code == "WEBPUSH" && t.Enabled == true && t.Removed == null);
            await GXNotificationTypeAccess.RestrictAsync(db, query, authorization, principal, token);
            var selected = await db.SelectAsync<GXNotification>(query, token);
            await GXNotificationSourceStorage.LoadAsync(db, selected, token);
            foreach (var item in events)
            {
                if (!selected.Any(n => Matches(n.Sources, item)) || !(await authorization.AuthorizeAsync(principal, null, item.Type)).Succeeded)
                {
                    continue;
                }

                await sender.ShowNotificationAsync(userId, item.Title ?? item.Type, item.ToString(), token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (AmiModuleUnavailableException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WebPush delivery failed for user {UserId}.", userId);
        }
    }
}
