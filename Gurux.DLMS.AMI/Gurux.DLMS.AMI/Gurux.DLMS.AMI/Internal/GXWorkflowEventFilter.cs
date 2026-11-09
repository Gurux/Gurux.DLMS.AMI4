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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Internal;

internal static class GXWorkflowEventFilter
{
    internal static async Task<bool> MatchesAsync(GXDbConnection connection, GXWorkflow workflow,
        GXWorkflowEventContext context, CancellationToken token = default)
    {
        var device = GetId(workflow, "event.device");
        var group = GetId(workflow, "event.device-group");
        if (device == null && group == null) {
            return true;
        }
        if (context.DeviceId == null || context.DeviceId == Guid.Empty) {
            return false;
        }
        if (device != null && device != context.DeviceId) {
            return false;
        }
        if (group == null) {
            return true;
        }
        if (context.EventCode == "device.delete" && context.DeviceGroupIds?.Contains(group.Value) == true)
            {
            return (await connection.SelectAsync<GXDeviceGroup>(GXSelectArgs.Select<GXDeviceGroup>(g => g.Id,
                g => g.Id == group && g.Removed == null), token)).Any();
        }
        var query = GXSelectArgs.Select<GXDeviceGroupDevice>(m => m.DeviceId,
            m => m.DeviceId == context.DeviceId && m.DeviceGroupId == group && m.Removed == null);
        query.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDeviceGroup>(m => m.DeviceGroupId, g => g.Id);
        query.Where.And<GXDeviceGroup>(g => g.Removed == null);
        return (await connection.SelectAsync<GXDeviceGroupDevice>(query, token)).Any();
    }

    internal static async Task EnsureEventAccessAsync(GXDbConnection connection, IAuthorizationService authorization,
        ClaimsPrincipal user, GXWorkflowEventContext context, CancellationToken token = default)
    {
        if (context.DeviceId == null || context.DeviceId == Guid.Empty) {
            return;
        }
        if (context.EventCode != "device.delete")
        {
            await ValidateAsync(connection, authorization, user,
                new GXWorkflow { Properties = [new() { Name = "event.device", Value = context.DeviceId.ToString() }] }, token);
            return;
        }
        // A successful deletion no longer has a device or membership row. Only the host publisher
        // supplies this pre-deletion group snapshot; it must never come from a client request.
        if (!(await authorization.AuthorizeAsync(user, null, GXDevicePolicies.View)).Succeeded
            || !(await authorization.AuthorizeAsync(user, null, GXDeviceGroupPolicies.View)).Succeeded)
            {
            throw new UnauthorizedAccessException("The event device is unavailable.");
        }
        var userId = GXWorkflowIdentityAccess.ActorId(user);
        if (string.IsNullOrEmpty(userId)) {
            throw new UnauthorizedAccessException("The event device is unavailable.");
        }
        if (user.IsInRole(GXRoles.Admin)) {
            return;
        }
        var groups = context.DeviceGroupIds;
        if (groups == null || groups.Count == 0) {
            throw new UnauthorizedAccessException("The event device is unavailable.");
        }
        foreach (var group in groups)
            {
            if (await HasActiveMembershipAsync(connection, typeof(GXDeviceGroup), group, userId, token)) {
                return;
            }
        }
        throw new UnauthorizedAccessException("The event device is unavailable.");
    }

    internal static async Task ValidateAsync(GXDbConnection connection, IAuthorizationService authorization,
        ClaimsPrincipal user, GXWorkflow workflow, CancellationToken token = default)
    {
        var device = GetId(workflow, "event.device");
        var group = GetId(workflow, "event.device-group");
        if (device == null && group == null) {
            return;
        }
        var userId = GXWorkflowIdentityAccess.ActorId(user);
        if (string.IsNullOrEmpty(userId)) {
            throw new UnauthorizedAccessException("A device event filter requires an authenticated user.");
        }
        if (device != null)
        {
            if (!(await authorization.AuthorizeAsync(user, null, GXDevicePolicies.View)).Succeeded)
                {
                throw new UnauthorizedAccessException("The selected event device is unavailable.");
            }
            bool visible = user.IsInRole(GXRoles.Admin)
                ? (await connection.SelectAsync<GXDevice>(GXSelectArgs.Select<GXDevice>(d => d.Id,
                    d => d.Id == device && d.Removed == null), token)).Any()
                : await HasActiveMembershipAsync(connection, typeof(GXDevice), device.Value, userId, token);
            if (!visible)
                {
                throw new UnauthorizedAccessException("The selected event device is unavailable.");
            }
        }
        if (group != null)
        {
            if (!(await authorization.AuthorizeAsync(user, null, GXDeviceGroupPolicies.View)).Succeeded)
                {
                throw new UnauthorizedAccessException("The selected event device group is unavailable.");
            }
            bool visible = user.IsInRole(GXRoles.Admin)
                ? (await connection.SelectAsync<GXDeviceGroup>(GXSelectArgs.Select<GXDeviceGroup>(g => g.Id,
                    g => g.Id == group && g.Removed == null), token)).Any()
                : await HasActiveMembershipAsync(connection, typeof(GXDeviceGroup), group.Value, userId, token);
            if (!visible)
                {
                throw new UnauthorizedAccessException("The selected event device group is unavailable.");
            }
        }
    }

    internal static bool HasFilters(GXWorkflow workflow) => workflow.Properties?.Any(p =>
        p.Module == null && p.Removed == null && p.Name?.StartsWith("event.", StringComparison.Ordinal) == true) == true;

    internal static Guid? GetId(GXWorkflow workflow, string key)
    {
        var properties = workflow.Properties?.Where(p => p.Module == null && p.Removed == null &&
            p.Name?.StartsWith("event.", StringComparison.Ordinal) == true).ToArray() ?? [];
        if (properties.Any(p => p.Name is not ("event.device" or "event.device-group")))
            {
            throw new ArgumentException("Unknown workflow event filter property.");
        }
        var matches = properties.Where(p => p.Name == key).ToArray();
        if (matches.Length == 0) {
            return null;
        }
        if (matches.Length != 1 || !Guid.TryParse(matches[0].Value, out var id) || id == Guid.Empty)
            {
            throw new ArgumentException("Workflow event filters require one nonempty GUID per property.");
        }
        return id;
    }

    private static async Task<bool> HasActiveMembershipAsync(GXDbConnection connection, Type resourceType,
        Guid resourceId, string userId, CancellationToken token)
    {
        var query = GXRepositoryUsers.Query(resourceType, resourceId);
        query.Where.And<GXUser>(u => u.Id == userId);
        return (await connection.SelectAsync<GXUser>(query, token)).Any();
    }
}

