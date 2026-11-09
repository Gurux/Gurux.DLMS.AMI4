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

namespace Gurux.DLMS.AMI.Client.Shared;

/// <summary>Automatic update checks and email notification preferences.</summary>
public sealed class UpdateSettings
{
    /// <summary>Frequency of automatic update checks.</summary>
    public UpdateCheckInterval CheckForUpdates { get; set; } = UpdateCheckInterval.Daily;
    /// <summary>Recipients; an empty list disables update emails.</summary>
    public string[] EmailAddresses { get; set; } = [];
    /// <summary>Releases that trigger an email.</summary>
    public UpdateEmailThreshold EmailNotificationThreshold { get; set; } = UpdateEmailThreshold.AllNewerVersions;
    /// <summary>Subject used for update notification emails.</summary>
    public string EmailHeader { get; set; } = "Gurux.DLMS.AMI security update available";
    /// <summary>Plain text email template; {Url} is replaced by the site address.</summary>
    public string EmailText { get; set; } = "There is a security update available for your version of Gurux.DLMS.AMI. To ensure the security of your server, you should update immediately!\n\nSee the available updates page for more information:\nhttps://{Url}/updates\n\nYour site is currently configured to send these emails when any updates are available. To get notified only for security updates, https://{Url}/updates/settings.";
}
