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

using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;

namespace Gurux.DLMS.AMI.Server.Repository;

// Trusted lifecycle contracts owned by the corresponding repositories. They are
// bound to one module and one transaction, and are not exposed to HTTP clients.
internal interface IModulePolicyRepository
{
    Task<GXPolicy?> EnsureCaptureAsync(string name, CancellationToken token);
}
internal interface IModuleSettingsRepository
{
    Task<Guid?> ReadNotificationTypeIdAsync(CancellationToken token);
    Task SetNotificationTypeIdAsync(Guid? id, CancellationToken token);
    Task<Guid?> ReadLogMenuLinkIdAsync(CancellationToken token);
    Task SetLogMenuLinkIdAsync(Guid? id, CancellationToken token);
}
internal interface IModuleLogTypeRepository
{
    Task<GXLogType> EnsureAsync(GXPolicy policy, CancellationToken token);
    Task<GXLogType?> ReadAsync(CancellationToken token);
}
internal interface IModuleMenuRepository
{
    Task EnsureLogLinkAsync(AmiModuleCapture definition, GXPolicy policy, CancellationToken token);
    Task RemoveLogLinkAsync(CancellationToken token);
}
internal interface IModuleEnumTypeRepository
{
    Task<int> EnsureLogCategoryAsync(string name, CancellationToken token);
}
internal interface IModuleLogRepository
{
    Task<int> AddAsync(IReadOnlyList<AmiModuleLog> logs, IReadOnlyList<string> recipients, CancellationToken token);
    Task RemoveAsync(CancellationToken token);
}
internal interface IModuleNotificationTypeRepository
{
    Task<GXNotificationType> EnsureAsync(CancellationToken token);
    Task RemoveAsync(CancellationToken token);
}
internal interface IModuleNotificationRepository
{
    Task EnsureAdministratorsAsync(GXNotificationType type, CancellationToken token);
    Task RemoveAsync(GXNotificationType type, CancellationToken token);
}
