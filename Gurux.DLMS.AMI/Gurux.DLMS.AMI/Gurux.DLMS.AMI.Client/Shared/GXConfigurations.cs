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

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Available configuration names.
    /// </summary>
    /// <remarks>
    /// Configuration names are used in the database.
    /// Don't modify or rename.
    /// </remarks>
    public static class GXConfigurations
    {
        /// <summary>
        /// System configuration.
        /// </summary>
        public const string System = "system";
        public const string Status = "status";
        public const string Security = "security";
        public const string Language = "language";
        public const string Modules = "Modules";
        public const string Agent = "Agent";
        public const string Components = "components";
        public const string Block = "blocks";
        public const string Menu = "menus";
        public const string Tab = "tabs";
        public const string Log = "logs";
        public const string View = "views";
        public const string Content = "contents";
        public const string ContentType = "content-types";
        public const string Trigger = "triggers";
        public const string Workflow = "workflows";
        public const string Cron = "cron";
        public const string History = "history";
        public const string Statistic = "statistic";
        public const string Maintenance = "maintenance";
        public const string Scripts = "scripts";
        public const string AccessControl = "access-control";
        public const string Performance = "performance";
        public const string Manufacturer = "Manufacturer";
        public const string KeyManagement = "key-management";
        public const string Subtotal = "subtotal";
        public const string Report = "report";
        public const string Notification = "notification";
        public const string Prune = "prune";
        public const string Appearance = "appearance";
        public const string Account = "account";
        public const string LocalizedResource = "localized-resource";
        /// <summary>Outbound HTTP proxy configuration.</summary>
        public const string HttpProxy = "http-proxy";
    }
}
