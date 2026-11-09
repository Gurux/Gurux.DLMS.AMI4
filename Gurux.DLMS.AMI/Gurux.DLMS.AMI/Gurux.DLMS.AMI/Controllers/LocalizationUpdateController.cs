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

using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;

namespace Gurux.DLMS.AMI.Controllers;

/// <summary>Provides localization update API endpoints.</summary>
/// <param name="options">The options.</param>
/// <param name="database">The database.</param>
[ApiController]
[Route("api/Update/Localization")]
[Authorize(Policy = GXUpdatePolicies.Admin)]
public sealed class LocalizationUpdateController(CatalogUpdaterOptions options, IDbConnectionFactory database) : ControllerBase
{
    /// <summary>Performs the get operation.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<GXLocalizationUpdatesResponse> Get(CancellationToken cancellationToken)
    {
        await using var connection = await database.GetConnectionAsync(cancellationToken);
        if (!new GXSchemaManager(connection).TableExist<GXLanguage>())
        {
            return new GXLocalizationUpdatesResponse
            {
                Configured = true
            };
        }
        var activeLanguages = (await connection.SelectAsync<GXLanguage>(
            GXSelectArgs.Select<GXLanguage>(language => language.Id, language => language.Enabled == true), cancellationToken))
            .Select(language => language.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (activeLanguages.Count == 0)
        {
            return new GXLocalizationUpdatesResponse
            {
                Configured = true
            };
        }
        if (!new GXSchemaManager(connection).TableExist<GXLocalization>())
        {
            return new GXLocalizationUpdatesResponse
            {
                Configured = true
            };
        }
        var rows = await connection.SelectAsync<GXLocalization>(
            GXSelectArgs.SelectAll<GXLocalization>(row => row.Enabled == true), cancellationToken);
        var modules = new GXSchemaManager(connection).TableExist<GXModule>()
            ? await connection.SelectAsync<GXModule>(
                GXSelectArgs.Select<GXModule>(module => new { module.Id, module.Status }), cancellationToken)
            : [];
        return new GXLocalizationUpdatesResponse
        {
            Configured = true,
            Items = [.. GXLocalizationOwnerFilter.Filter(rows, modules)
                .Where(row => options.IncludePrereleases || !Gurux.DLMS.AMI.Shared.GXReleaseVersionComparer.IsPrerelease(row.Version))
                .GroupBy(row => (row.Owner, row.OwnerType))
                .Select(group =>
                {
                    var versions = group.OrderByDescending(row => row.Version, Gurux.DLMS.AMI.Shared.GXReleaseVersionComparer.Instance)
                        .Select(row => new GXLocalizationVersion
                        {
                            Id = row.Id, Version = row.Version, OwnerVersionRange = row.OwnerVersionRange,
                            Cultures = [.. row.Cultures.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Where(culture => activeLanguages.Any(language => language.Equals(culture, StringComparison.OrdinalIgnoreCase)
                                    || language.Split('-')[0].Equals(culture, StringComparison.OrdinalIgnoreCase)))]
                        }).Where(version => version.Cultures.Count != 0).ToList();
                    return new GXLocalizationUpdate
                    {
                        Owner = group.Key.Owner, OwnerType = group.Key.OwnerType, Versions = versions,
                        Version = versions.FirstOrDefault()?.Version ?? "",
                        Cultures = versions.FirstOrDefault()?.Cultures ?? [],
                        OwnerVersionRange = versions.FirstOrDefault()?.OwnerVersionRange
                    };
                }).Where(item => item.Versions.Count != 0)]
        };
    }
}
