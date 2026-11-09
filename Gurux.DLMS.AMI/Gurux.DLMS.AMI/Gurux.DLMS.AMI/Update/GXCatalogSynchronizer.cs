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

using Gurux.Updater.Enums;
using Gurux.Updater.Model;
using Gurux.Updater.Services;
using System.Text.Json;
namespace Gurux.DLMS.AMI.Update;

/// <summary>Provides catalog synchronizer.</summary>
/// <param name="store">The store.</param>
/// <param name="options">The options.</param>
/// <param name="logger">The logger.</param>
/// <param name="scripts">Verified script release storage.</param>
public sealed class GXCatalogSynchronizer(GXCatalogStore store, CatalogUpdaterOptions options, ILogger<GXCatalogSynchronizer> logger, GXScriptCatalogStore scripts)
{
    /// <summary>Performs the synchronize operation.</summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="type">The type.</param>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task SynchronizeAsync(GXUpdateCatalog catalog, CatalogProductType? type, CancellationToken token)
    {
        var failures = new List<string>();
        foreach (var item in catalog.Items.Where(i => i.Type is CatalogProductType.Module or CatalogProductType.Agent && (type == null || i.Type == type)))
        {
            token.ThrowIfCancellationRequested();
            try
            {
                string pattern = options.AssetPatterns.GetValueOrDefault(item.Id) ?? (item.Type == CatalogProductType.Module ? options.DefaultModuleAssetPattern : options.DefaultAgentAssetPattern);
                var releases = await GXCatalogReleaseSelector.SelectAsync(item, pattern);
                if (item.Type == CatalogProductType.Module)
                {
                    await store.UpsertModuleAsync(item, releases, token);
                }
                else
                {
                    await store.UpsertAgentAsync(item, releases, token);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error)
            {
                logger.LogError(error, "Catalog product {Id} could not be synchronized", item.Id);
                failures.Add(item.Id + ": " + error.Message);
            }
        }
        if (failures.Count != 0)
        {
            throw new InvalidDataException(string.Join(Environment.NewLine, failures));
        }

        if (type == null || type == CatalogProductType.Module)
        {
            using var client = new HttpClient();
            var source = options.LocalizationCatalogSource;
            if (string.IsNullOrWhiteSpace(source) && catalog.Items.Any(item => item.Id == "Gurux.AMI.Localization"))
            {
                var releases = await new GXCatalogUpdateService(client).GetReleasesAsync(catalog, "Gurux.AMI.Localization", "localization-index.json", count: 1, includePrereleases: options.IncludePrereleases, cancellationToken: token);
                source = (releases.Count == 0 ? null : releases[0]?.Asset?.DownloadUrl);
            }
        
            if (!string.IsNullOrWhiteSpace(source))
            {
                if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || !GXCatalogUpdateService.IsSupportedUri(uri))
                {
                    throw new InvalidDataException("The localization index must use HTTPS or loopback HTTP.");
                }
        
                var localizations = await new GXLocalizationService(client, allowLoopbackHttp: true).GetCatalogAsync(source, token);
                await store.UpsertLocalizationsAsync(localizations, source, token);
            }
        }

        if (type == null)
        {
            using var http = new HttpClient();
            await GXScriptCatalogDiscovery.SynchronizeAsync(http, catalog, options, scripts, token);
        }

        if (type == null || type == CatalogProductType.Module)
        {
            await store.MarkCheckedAsync(Gurux.DLMS.AMI.Client.Shared.GXConfigurations.Modules, token);
        }

        if (type == null || type == CatalogProductType.Agent)
        {
            await store.MarkCheckedAsync(Gurux.DLMS.AMI.Client.Shared.GXConfigurations.Agent, token);
        }
    }
}
/// <summary>Provides catalog client.</summary>
/// <param name="http">The http.</param>
/// <param name="options">The options.</param>
public sealed class GXCatalogClient(HttpClient http, CatalogUpdaterOptions options)
{
    /// <summary>Gets or sets the manufacturer product id.</summary>
    public string ManufacturerProductId => options.ManufacturerProductId;
    /// <summary>Reads the RSS feed published beside the configured catalog JSON.</summary>
    /// <param name="token">Token used to cancel the operation.</param>
    public Task<string> GetFeedAsync(CancellationToken token)
        => http.GetStringAsync(new Uri(GetCatalogUri(), "rss.xml"), token);

    private Uri GetCatalogUri()
    {
        if (!Uri.TryCreate(options.CatalogUrl, UriKind.Absolute, out var uri) || !GXCatalogUpdateService.IsSupportedUri(uri))
        {
            throw new InvalidOperationException("Updater:CatalogUrl must be an absolute HTTPS catalog URL or an HTTP loopback URL.");
        }

        return uri;
    }
    /// <summary>Performs the get operation.</summary>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task<GXUpdateCatalog> GetAsync(CancellationToken token)
    {
        var uri = GetCatalogUri();

        using var response = await http.GetAsync(uri, token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        var catalog = await JsonSerializer.DeserializeAsync<GXUpdateCatalog>(stream, GXCatalogCompatibility.JsonOptions(), token)
            ?? throw new InvalidDataException("Catalog response was empty.");
        if (catalog.SchemaVersion != 1 || catalog.Items.Any(item => string.IsNullOrWhiteSpace(item.Id)) || catalog.Items.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != catalog.Items.Count)
        {
            throw new InvalidDataException("Invalid update catalog schema or product IDs.");
        }
        var service = new GXCatalogUpdateService(http);
        // Retain the updater library's validation of every product and release.
        foreach (var item in catalog.Items)
        {
            await service.GetReleasesAsync(catalog, item.Id, count: 1, includePrereleases: true, cancellationToken: token);
        }

        return catalog;
    }
}


