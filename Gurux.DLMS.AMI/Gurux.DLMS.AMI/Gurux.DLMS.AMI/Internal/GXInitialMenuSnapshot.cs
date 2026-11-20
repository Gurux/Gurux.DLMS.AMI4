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
using System.Security.Cryptography;
using System.Text.Json;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Collects startup menu and authorization dependencies in bounded batch queries.</summary>
internal sealed class GXInitialMenuSnapshot
{
    /// <summary>Menu identities and content change stamps, without link bodies.</summary>
    internal List<GXMenu> Menus { get; init; } = [];
    /// <summary>Link identities, owner references and content change stamps.</summary>
    internal List<GXMenuLink> Links { get; init; } = [];
    /// <summary>Alternative policies assigned directly to menus.</summary>
    internal List<GXMenuPolicy> MenuPolicies { get; init; } = [];
    /// <summary>Alternative policies assigned to individual links.</summary>
    internal List<GXMenuLinkPolicy> LinkPolicies { get; init; } = [];
    /// <summary>Definitions of policies referenced by the selected resources.</summary>
    internal List<GXPolicy> Policies { get; init; } = [];
    /// <summary>Current requirements, including additions and deletions independently of policy stamps.</summary>
    internal List<GXPolicyRequirement> Requirements { get; init; } = [];
    /// <summary>Memberships contributing menu group data to a menu response.</summary>
    internal List<GXMenuGroupMenu> Memberships { get; init; } = [];
    /// <summary>Group metadata included in menu responses.</summary>
    internal List<GXMenuGroup> Groups { get; init; } = [];
    /// <summary>Configuration names and enabled states affecting menu link representation and visibility.</summary>
    internal List<GXConfiguration> Configurations { get; init; } = [];
    /// <summary>Creator identifiers and display names included in menu responses.</summary>
    internal List<GXUser> Creators { get; init; } = [];

    /// <summary>Reads metadata for every selected menu together, without per-menu repository or authorization queries.</summary>
    /// <param name="database">Database containing the current menu and policy definitions.</param>
    /// <param name="names">Distinct menu names selected for startup.</param>
    /// <param name="user">Current principal, including the permission claims used by authorization handlers.</param>
    /// <param name="token">Cancels the batch metadata reads.</param>
    /// <returns>Dependency fingerprints indexed by menu name; missing menus have no reusable version.</returns>
    /// <exception cref="OperationCanceledException">The request was canceled.</exception>
    internal static async Task<IReadOnlyDictionary<string, string>> ReadAsync(IDbConnectionFactory database,
        IEnumerable<string> names, ClaimsPrincipal user, CancellationToken token)
    {
        var selected = names.ToArray();
        if (selected.Length == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
        await using var connection = await database.GetConnectionAsync(token);
        var menus = await connection.SelectAsync<GXMenu>(GXSelectArgs.Select<GXMenu>(m => new
        {
            m.Id, m.Name, m.ConcurrencyStamp, m.Updated, m.Removed, m.Creator, m.Parent
        }, m => selected.Contains(m.Name!)), token);
        var ids = menus.Select(m => m.Id).ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
        var linkQuery = GXSelectArgs.Select<GXMenuLink>(l => new
        {
            l.Id, l.Menu, l.Configuration, l.ConcurrencyStamp, l.Updated, l.Removed, l.Parent
        });
        linkQuery.Joins.AddInnerJoin<GXMenuLink, GXMenu>(l => l.Menu!, m => m.Id);
        linkQuery.Where.And<GXMenu>(m => ids.Contains(m.Id));
        var links = await connection.SelectAsync<GXMenuLink>(linkQuery, token);
        var linkIds = links.Select(l => l.Id).ToArray();
        var menuPolicies = await connection.SelectAsync<GXMenuPolicy>(GXSelectArgs.SelectAll<GXMenuPolicy>(p => ids.Contains(p.MenuId)), token);
        var linkPolicies = linkIds.Length == 0 ? [] : await connection.SelectAsync<GXMenuLinkPolicy>(
            GXSelectArgs.SelectAll<GXMenuLinkPolicy>(p => linkIds.Contains(p.MenuLinkId)), token);
        var policyIds = menuPolicies.Select(p => p.PolicyId).Concat(linkPolicies.Select(p => p.PolicyId)).Distinct().ToArray();
        var policies = policyIds.Length == 0 ? [] : await connection.SelectAsync<GXPolicy>(
            GXSelectArgs.Select<GXPolicy>(p => new { p.Id, p.Name, p.NormalizedName, p.Removed, p.ConcurrencyStamp,
                p.Updated, p.RequirementOperator, p.RequireAuthenticatedUser }, p => policyIds.Contains(p.Id)), token);
        var requirementQuery = GXSelectArgs.SelectAll<GXPolicyRequirement>();
        requirementQuery.Joins.AddInnerJoin<GXPolicyRequirement, GXPolicy>(r => r.Policy!, p => p.Id);
        requirementQuery.Where.And<GXPolicy>(p => policyIds.Contains(p.Id));
        var requirements = policyIds.Length == 0 ? [] : await connection.SelectAsync<GXPolicyRequirement>(requirementQuery, token);
        var memberships = await connection.SelectAsync<GXMenuGroupMenu>(GXSelectArgs.SelectAll<GXMenuGroupMenu>(m => ids.Contains(m.MenuId)), token);
        var groupIds = memberships.Select(m => m.MenuGroupId).Distinct().ToArray();
        var groupQuery = GXSelectArgs.SelectAll<GXMenuGroup>(g => groupIds.Contains(g.Id));
        groupQuery.Columns.Exclude<GXMenuGroup>(g => new { g.Menus, g.UserGroups });
        var groups = groupIds.Length == 0 ? [] : await connection.SelectAsync<GXMenuGroup>(groupQuery, token);
        var configIds = links.Where(l => l.Configuration != null).Select(l => l.Configuration!.Id).Distinct().ToArray();
        var configurations = configIds.Length == 0 ? [] : await connection.SelectAsync<GXConfiguration>(
            GXSelectArgs.Select<GXConfiguration>(c => new { c.Id, c.Name, c.Enabled }, c => configIds.Contains(c.Id)), token);
        var creatorIds = menus.Where(m => m.Creator != null).Select(m => m.Creator!.Id).Distinct().ToArray();
        var creators = creatorIds.Length == 0 ? [] : await connection.SelectAsync<GXUser>(
            GXSelectArgs.Select<GXUser>(u => new { u.Id, u.UserName }, u => creatorIds.Contains(u.Id)), token);
        return new GXInitialMenuSnapshot
        {
            Menus = menus, Links = links, MenuPolicies = menuPolicies, LinkPolicies = linkPolicies,
            Policies = policies, Requirements = requirements, Memberships = memberships, Groups = groups,
            Configurations = configurations, Creators = creators
        }.Versions(user);
    }

    /// <summary>Hashes each menu's dependencies and the principal that authorized its cached representation.</summary>
    /// <param name="user">Principal whose identity, roles and permission claims partition cached menu results.</param>
    /// <returns>Independent versions; an unrelated menu or policy edit does not invalidate other menus.</returns>
    internal IReadOnlyDictionary<string, string> Versions(ClaimsPrincipal user)
    {
        var principal = user.Identities.Select(identity => JsonSerializer.Serialize(new
        {
            identity.AuthenticationType, identity.NameClaimType, identity.RoleClaimType,
            Claims = identity.Claims.Select(c => JsonSerializer.Serialize(new { c.Type, c.Value, c.ValueType, c.Issuer, c.OriginalIssuer }))
                .Order(StringComparer.Ordinal).ToArray()
        })).ToArray();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var menu in Menus)
        {
            if (string.IsNullOrWhiteSpace(menu.Name) || menu.Removed != null)
            {
                continue;
            }
            var links = Links.Where(l => l.Menu?.Id == menu.Id).OrderBy(l => l.Id).ToArray();
            var linkIds = links.Select(l => l.Id).ToHashSet();
            var menuPolicies = MenuPolicies.Where(p => p.MenuId == menu.Id).OrderBy(p => p.PolicyId).ToArray();
            var linkPolicies = LinkPolicies.Where(p => linkIds.Contains(p.MenuLinkId)).OrderBy(p => p.MenuLinkId).ThenBy(p => p.PolicyId).ToArray();
            var policyIds = menuPolicies.Select(p => p.PolicyId).Concat(linkPolicies.Select(p => p.PolicyId)).ToHashSet();
            var memberships = Memberships.Where(m => m.MenuId == menu.Id).OrderBy(m => m.MenuGroupId).ToArray();
            var groupIds = memberships.Select(m => m.MenuGroupId).ToHashSet();
            var configIds = links.Select(l => l.Configuration?.Id).ToHashSet();
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new
            {
                Principal = principal,
                Menu = new { menu.Id, menu.Name, menu.ConcurrencyStamp, menu.Updated, menu.Removed, CreatorId = menu.Creator?.Id, ParentId = menu.Parent?.Id },
                Links = links.Select(l => new { l.Id, l.ConcurrencyStamp, l.Updated, l.Removed, ConfigurationId = l.Configuration?.Id, ParentId = l.Parent?.Id }),
                MenuPolicies = menuPolicies, LinkPolicies = linkPolicies,
                Policies = Policies.Where(p => policyIds.Contains(p.Id)).OrderBy(p => p.Id),
                Requirements = Requirements.Where(r => r.Policy != null && policyIds.Contains(r.Policy.Id)).OrderBy(r => r.Id),
                Memberships = memberships, Groups = Groups.Where(g => groupIds.Contains(g.Id)).OrderBy(g => g.Id),
                Configurations = Configurations.Where(c => configIds.Contains(c.Id)).OrderBy(c => c.Id),
                Creator = Creators.Where(c => c.Id == menu.Creator?.Id).Select(c => new { c.Id, c.UserName }).ToArray()
            });
            result[menu.Name] = Convert.ToHexString(SHA256.HashData(bytes));
        }
        return result;
    }
}
