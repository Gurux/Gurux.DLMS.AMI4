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

using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Checks visibility of non-device event targets under the actual execution account.</summary>
internal static class GXWorkflowEventAccess
{
    internal static async Task EnsureAsync(GXDbConnection connection, IAuthorizationService authorization,
        ClaimsPrincipal user, GXWorkflowEventContext context, CancellationToken token = default)
    {
        Type resource;
        string view;
        Guid id;
        if (context.EventCode.StartsWith("agent.", StringComparison.Ordinal))
        {
            resource = typeof(GXAgent); view = GXAgentPolicies.View;
            if (!Guid.TryParse(context.TargetId, out id)) {
                throw new UnauthorizedAccessException("The agent event target is invalid.");
            }
        }
        else if (context.EventCode.StartsWith("schedule.", StringComparison.Ordinal))
        {
            resource = typeof(GXSchedule); view = GXSchedulePolicies.View;
            if (context.ScheduleId is { } scheduleId) {
                id = scheduleId;
            }
            else if (!Guid.TryParse(context.TargetId, out id)) {
                throw new UnauthorizedAccessException("The schedule event target is invalid.");
            }
        }
        else {
            return;
        }
        await authorization.EnsureAnyPolicyAsync(user, view);
        if (!user.IsInRole(GXRoles.Admin))
        {
            var query = GXRepositoryUsers.Query(resource, id);
            var userId = GXWorkflowIdentityAccess.ActorId(user);
            query.Where.And<GXUser>(u => u.Id == userId);
            if ((await connection.SelectAsync<GXUser>(query, token)).Count == 0)
                {
                throw new UnauthorizedAccessException("The workflow execution account cannot access the event target.");
            }
        }
    }
}