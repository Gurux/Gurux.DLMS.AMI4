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

using System.Security.Cryptography;
using System.Text;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Server.Module;
using Gurux.DLMS.AMI.Server;
using Gurux.DLMS.AMI.Services;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Sends update emails through an available email provider.</summary>
public sealed class GXUpdateEmailNotifier(IServiceProvider services, GXUpdateSettingsStore store, ILogger<GXUpdateEmailNotifier> logger)
{
    /// <summary>Whether a direct sender or an active module email provider is available.</summary>
    public bool EmailSenderAvailable => services.GetService<IGXAmiEmailSender>() is { } sender &&
        (sender is not GXModuleEmailSender || services.GetService<GXModuleRuntime>()?.GetStartupProviders()
            .Any(p => p.Services.Contains(typeof(IGXAmiEmailSender).FullName!)) == true);

    /// <summary>Notifies each recipient once per qualifying core release.</summary>
    public async Task NotifyAsync(GXAvailableUpdatesResponse snapshot, CancellationToken token)
    {
        if (!EmailSenderAvailable)
        {
            return;
        }
        var settings = await store.ReadAsync(token);
        var releases = GXUpdateNotificationPolicy.Releases(snapshot.Core, settings).ToArray();
        if (releases.Length == 0 || settings.EmailAddresses.Length == 0)
        {
            return;
        }
        var sender = services.GetRequiredService<IGXAmiEmailSender>();
        foreach (var recipient in settings.EmailAddresses)
        {
            var pending = new List<string>();
            foreach (var release in releases)
            {
                var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                    $"{recipient.ToLowerInvariant()}|{snapshot.Core.Id}|{release.Version}|{release.IsSecurityUpdate}")));
                if (!await store.WasDeliveredAsync(key, token))
                {
                    pending.Add(key);
                }
            }
            if (pending.Count == 0)
            {
                continue;
            }
            try
            {
                await sender.SendEmailAsync(recipient, settings.EmailHeader, GXUpdateNotificationPolicy.HtmlMessage(settings, ServerSettings.ServerAddress), token);
                foreach (var key in pending)
                {
                    await store.MarkDeliveredAsync(key, token);
                }
            }
            catch (OperationCanceledException)when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to send an update notification. The next update check will retry delivery.");
            }
        }
    }
}
