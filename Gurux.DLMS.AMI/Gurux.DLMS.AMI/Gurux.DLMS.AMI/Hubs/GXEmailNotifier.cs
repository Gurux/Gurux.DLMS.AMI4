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

using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
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

/// <summary>Delivers subscribed resource events through the active email provider.</summary>
public sealed class GXEmailNotifier(IServiceScopeFactory scopes, ILogger<GXEmailNotifier> logger)
{
    /// <summary>Checks the active provider without retaining a module implementation.</summary>
    public static bool IsAvailable(IServiceProvider services, IGXAmiEmailSender? sender)
        => sender != null && (sender is not GXModuleEmailSender ||
            services.GetService<IGXAmiModuleServices>()?.IsAvailable<IGXAmiEmailSender>() == true);

    /// <summary>Delivers only the user's active subscriptions after checking current access.</summary>
    public async Task NotifyAsync(string userId, IReadOnlyList<GXTarget> events, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            await using var scope = scopes.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var sender = services.GetService<IGXAmiEmailSender>();
            if (!IsAvailable(services, sender))
            {
                return;
            }

            var providerNames = sender is GXModuleEmailSender ? services.GetRequiredService<GXModuleRuntime>().GetStartupProviders().Where(p => p.Services.Contains(typeof(IGXAmiEmailSender).FullName!)).Select(p => p.Id.ToUpperInvariant()).ToArray() : new[]
            {
                "EMAIL"
            };
            var users = services.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId);
            if (user == null || !user.IsApproved || await users.IsLockedOutAsync(user))
            {
                return;
            }

            var principal = await services.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>().CreateAsync(user);
            var authorization = services.GetRequiredService<IAuthorizationService>();
            await using var db = await services.GetRequiredService<IDbConnectionFactory>().GetConnectionAsync(token);
            foreach (var providerName in providerNames)
            {
                var query = GXQuery.GetNotificationsByUser(n => "*", userId);
                query.Joins.AddInnerJoin<GXNotification, GXNotificationType>(n => n.Type!, t => t.Id);
                query.Where.And<GXNotification>(n => n.Enabled == true && n.Removed == null);
                query.Where.And<GXNotificationType>(t => t.Code == providerName && t.Enabled == true && t.Removed == null);
                await GXNotificationTypeAccess.RestrictAsync(db, query, authorization, principal, token);
                var selected = await db.SelectAsync<GXNotification>(query, token);
                await GXNotificationSourceStorage.LoadAsync(db, selected, token);
                await DeliverAsync(sender!, selected, events, user.EmailConfirmed ? user.Email : null, async (item, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return (await authorization.AuthorizeAsync(principal, null, item.Type)).Succeeded;
                }, token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (AmiModuleUnavailableException)
        {
        }
        catch (Exception error)
        {
            logger.LogError(error, "Email delivery failed for user {UserId}.", userId);
        }
    }

    /// <summary>Matches selected permissions and the configured maximum trace verbosity.</summary>
    public static bool Matches(GXNotification subscription, GXTarget item)
        => subscription.Enabled == true && subscription.Removed == null &&
            GXWebPushNotifier.Matches(subscription.Sources, item) &&
            subscription.TraceLevel != TraceLevel.Off &&
            (!item.Level.HasValue || !subscription.TraceLevel.HasValue || item.Level.Value <= (int)subscription.TraceLevel.Value);

    /// <summary>Parses individual destinations, rejecting header characters and invalid addresses.</summary>
    public static IEnumerable<string> Destinations(string? destination, string? confirmedEmail)
    {
        foreach (var value in (string.IsNullOrWhiteSpace(destination) ? confirmedEmail ?? "" : destination)
            .Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (value.Contains('\r') || value.Contains('\n'))
            {
                continue;
            }

            if (MailAddress.TryCreate(value, out var address) && address.Address.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                yield return address.Address;
            }
        }
    }

    /// <summary>Delivers authorized events, isolating failures for each destination.</summary>
    public async Task DeliverAsync(IGXAmiEmailSender sender, IEnumerable<GXNotification> subscriptions,
        IReadOnlyList<GXTarget> events, string? confirmedEmail,
        Func<GXTarget, CancellationToken, Task<bool>> authorize, CancellationToken token)
    {
        foreach (var item in events)
        {
            token.ThrowIfCancellationRequested();
            var selected = subscriptions.Where(n => Matches(n, item)).ToArray();
            if (selected.Length == 0 || !await authorize(item, token))
            {
                continue;
            }

            var subject = (item.Title ?? item.Type).Replace('\r', ' ').Replace('\n', ' ');
            var html = "<h1>" + WebUtility.HtmlEncode(subject) + "</h1><p>" + WebUtility.HtmlEncode(item.ToString()) + "</p>";
            foreach (var destination in selected.SelectMany(n => Destinations(n.Properties?.Where(w => w.Name == "destination").Select(p => p.Value).FirstOrDefault(), confirmedEmail)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    await sender.SendEmailAsync(destination, subject, html, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error)
                {
                    logger.LogError(error, "Unable to deliver an email notification.");
                }
            }
        }
    }
}

