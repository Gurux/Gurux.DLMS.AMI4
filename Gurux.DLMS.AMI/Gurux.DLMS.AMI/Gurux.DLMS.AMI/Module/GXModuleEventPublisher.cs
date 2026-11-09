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

using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Provides module event publisher.</summary>
/// <param name="notifications">The notifications.</param>
/// <param name="database">Stores source ownership before publishing module events.</param>
public sealed class GXModuleEventPublisher(NotificationCache notifications, IDbConnectionFactory database) : IGXAmiModuleEventPublisher
{
    /// <summary>Performs the publish operation.</summary>
    /// <param name="recipients">The recipients.</param>
    /// <param name="moduleId">The module id.</param>
    /// <param name="target">The target.</param>
    /// <param name="targetId">The target id.</param>
    /// <param name="action">The action.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task PublishAsync(IReadOnlyList<string> recipients, string moduleId, string target, string targetId, string action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        await using var connection = await database.GetConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        await GXNotificationSourceStorage.RegisterAsync(connection, transaction, target.Split('.')[0], moduleId, cancellationToken);
        await GXNotificationSourceStorage.RegisterAsync(connection, transaction, target + "." + action, moduleId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        notifications.Add(recipients, new GXTarget(target + "." + action, targetId) { Source = moduleId });
    }
}
