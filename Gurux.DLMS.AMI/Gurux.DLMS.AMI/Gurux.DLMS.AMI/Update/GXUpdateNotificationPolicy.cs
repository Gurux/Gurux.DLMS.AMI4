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

using System.Net;
using System.Net.Mail;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared;

namespace Gurux.DLMS.AMI.Update;

internal static class GXUpdateNotificationPolicy
{
    internal static bool IsDue(UpdateSettings settings, DateTimeOffset? lastCheck, DateTimeOffset now)
        => lastCheck == null || now.UtcDateTime.Date - lastCheck.Value.UtcDateTime.Date >= TimeSpan.FromDays((int)settings.CheckForUpdates);

    internal static IEnumerable<GXAvailableRelease> Releases(GXAvailableUpdate core, UpdateSettings settings)
        => core.Releases.Where(r => !r.Prerelease && GXReleaseVersionComparer.TryNormalize(core.InstalledVersion, out var installed)
            && GXReleaseVersionComparer.TryNormalize(r.Version, out var available)
            && !GXReleaseVersionComparer.IsPrerelease(available)
            && GXReleaseVersionComparer.Instance.Compare(available, installed) > 0
            && (settings.EmailNotificationThreshold == UpdateEmailThreshold.AllNewerVersions || r.IsSecurityUpdate));

    internal static void Validate(UpdateSettings settings)
    {
        if (!Enum.IsDefined(settings.CheckForUpdates) || !Enum.IsDefined(settings.EmailNotificationThreshold))
        {
            throw new ArgumentException("Select a valid update interval and email notification threshold.");
        }
        if (string.IsNullOrWhiteSpace(settings.EmailHeader) || settings.EmailHeader.Length > 200 || settings.EmailHeader.Any(char.IsControl))
        {
            throw new ArgumentException("Email header is required, must be a single line and must not exceed 200 characters.");
        }
        if (string.IsNullOrWhiteSpace(settings.EmailText) || settings.EmailText.Length > 20000)
        {
            throw new ArgumentException("Email text is required and must not exceed 20000 characters.");
        }
        if (settings.EmailAddresses == null || settings.EmailAddresses.Length > 100)
        {
            throw new ArgumentException("Specify at most 100 email addresses.");
        }
        var recipients = new List<string>();
        foreach (var value in settings.EmailAddresses)
        {
            var address = value?.Trim();
            if (string.IsNullOrEmpty(address) || !MailAddress.TryCreate(address, out var parsed) || parsed.Address != address)
            {
                throw new ArgumentException("Invalid email address: " + address);
            }
            recipients.Add(address);
        }
        settings.EmailAddresses = [.. recipients.Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    internal static string HtmlMessage(UpdateSettings settings, string siteAddress)
    {
        var site = new Uri(siteAddress, UriKind.Absolute);
        var address = site.GetLeftPart(UriPartial.Authority) + site.AbsolutePath.TrimEnd('/');
        var text = settings.EmailText.Replace("https://{Url}", address, StringComparison.Ordinal)
            .Replace("http://{Url}", address, StringComparison.Ordinal)
            .Replace("{Url}", address[(address.IndexOf("://", StringComparison.Ordinal) + 3)..], StringComparison.Ordinal);
        return WebUtility.HtmlEncode(text).Replace("\r\n", "\n").Replace("\n", "<br />\n");
    }
}
