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
using System.Globalization;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using MenuKind = Gurux.DLMS.AMI.Shared.DTOs.Enums.MenuKind;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Db;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gurux.DLMS.AMI.Server.Midlewares;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Provides resource API endpoints.</summary>
/// <param name="menus">Reads menus after applying the current user's resource policies.</param>
/// <param name="appearances">Reads permitted themes, icon packs, and images.</param>
/// <param name="texts">Resolves localized resource values and their cache versions.</param>
/// <param name="localization">Resolves the requesting user's resource language.</param>
/// <param name="logger">Records resource failures that leave other startup groups available.</param>
/// <param name="database">Reads server-configured preload selections without loading unrelated resource data.</param>
/// <param name="blocks">The blocks visible to the requesting user.</param>
/// <param name="languageService">Resolves the profile or browser culture independently of available translations.</param>
[ApiController]
[Route("api/Resource")]
public sealed class ResourceController(IMenuRepository menus, IAppearanceRepository appearances,
    ILocalizedResourceRepository texts, ILocalizationRepository localization, ILogger<ResourceController> logger,
    IDbConnectionFactory? database = null, IBlockRepository? blocks = null,
    GXLanguageService? languageService = null) : ControllerBase
{
    /// <summary>Reads fresh dependency versions, falling back to authorized full reads if metadata is unavailable.</summary>
    /// <param name="menuNames">Menu names whose dependencies must be checked together.</param>
    /// <param name="cancellationToken">Cancels the metadata reads.</param>
    /// <returns>Current menu fingerprints, or an empty map that prevents cache reuse on failure.</returns>
    /// <exception cref="OperationCanceledException">The startup request was canceled.</exception>
    private async Task<IReadOnlyDictionary<string, string>> ReadMenuVersionsAsync(
        IEnumerable<string> menuNames, CancellationToken cancellationToken)
    {
        if (database == null)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
        try
        {
            return await GXInitialMenuSnapshot.ReadAsync(database, menuNames, User, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "Initial menu dependency versions unavailable; reading authorized menus directly.");
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    /// <summary>Loads startup resources, batch-checking menu dependencies before fully reading changed menus.</summary>
    /// <param name="request">Resource keys, cached versions, and preload selections for the initial view.</param>
    /// <param name="cancellationToken">Cancels database reads when the client abandons startup.</param>
    /// <returns>The authorized resource groups and their cache versions, or a validation error for invalid input.</returns>
    [HttpPost("Initial")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<GXInitialResourcesResponse>> Initial(GXInitialResources request, CancellationToken cancellationToken = default)
    {
        if (request.Language?.Length > 32 || request.MenuNames == null || request.MenuNames.Length > 8 || request.MenuNames.Any(n => string.IsNullOrWhiteSpace(n) || n.Length > 128) ||
            request.Texts == null || request.Texts.Length > 64 || request.Texts.Any(n => string.IsNullOrWhiteSpace(n) || n.Length > 512) ||
            request.Images == null || request.Images.Length > 64 || request.Images.Any(n => string.IsNullOrWhiteSpace(n) || n.Length > 128) ||
            request.Versions == null || request.Keys == null || request.Versions.Count > 512 || request.Keys.Count > 512 ||
            request.Versions.Any(p => p.Key.Length > 256 || p.Value == null || p.Value.Length > 128) ||
            request.Keys.Any(p => p.Key.Length > 256 || p.Value == null || p.Value.Length > 8192 || p.Value.Any(k => k == null || k.Length > 256)))
        {
            return BadRequest("Invalid initial resource request.");
        }

        string language;
        try
        {
            language = CultureInfo.GetCultureInfo(request.Language ?? await localization.GetUserLanguageAsync(cancellationToken) ?? "en").Name;
        }
        catch (CultureNotFoundException)
        {
            return BadRequest("Invalid language.");
        }
        var formattingCulture = languageService == null ? language : await languageService.GetUserLanguageAsync(User);
        var response = new GXInitialResourcesResponse { Language = language, Culture = formattingCulture };
        var imageNames = new HashSet<string>(request.Images, StringComparer.Ordinal);
        var names = new HashSet<string>(request.Texts, StringComparer.Ordinal);
        var menuNames = new HashSet<string>(request.MenuNames, StringComparer.Ordinal);
        var preloadHashes = new HashSet<string>(StringComparer.Ordinal);
        bool Needs(string group, string version, string key) => request.Versions.GetValueOrDefault(group) != version ||
            !request.Keys.TryGetValue(group, out var known) || !known.Contains(key, StringComparer.Ordinal);
        async Task Read(string group, Func<Task> read)
        {
            try
            {
                await read();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Initial resource {Group} unavailable.", group);
                response.Unavailable.Add(group);
            }
        }
        if (request.IncludeDefaults)
        {
            await Read("block:preload", async () =>
            {
                if (blocks == null)
                {
                    return;
                }
                // Keep permission and user-specific closed filtering in the repository.
                var selected = (await blocks.ListAsync(new ListBlocks
                {
                    Filter = new GXBlock { Preload = true, Enabled = true },
                    ExcludeClosed = true
                }, null, cancellationToken))
                    .Where(block => block.Preload == true && block.Enabled == true && block.Removed == null)
                    .OrderBy(block => block.Location).ThenBy(block => block.LocationOrder).ThenBy(block => block.Id)
                    .ToArray();
                var result = new ListBlocksResponse { Blocks = selected, Count = selected.Length };
                var group = new GXInitialResourceGroup { Version = GXBlockHttpCache.ETag(result) };
                if (Needs("block:preload", group.Version, "current"))
                {
                    group.Values["current"] = JsonSerializer.Serialize(result, JsonSerializerOptions.Web);
                }
                response.Resources["block:preload"] = group;
            });
            await Read("preload-menus", async () =>
            {
                if (database != null)
                {
                    await using var connection = await database.GetConnectionAsync(cancellationToken);
                    var selected = await connection.SelectAsync<GXMenu>(GXSelectArgs.Select<GXMenu>(m => m.Name!, m => m.Preload == true && m.Enabled == true && m.Removed == null), cancellationToken);
                    foreach (var menu in selected)
                    {
                        if (!string.IsNullOrWhiteSpace(menu.Name))
                        {
                            menuNames.Add(menu.Name);
                        }
                    }
                    var linked = GXSelectArgs.Select<GXMenu>(m => m.Name!, m => m.Enabled == true && m.Removed == null);
                    linked.Joins.AddInnerJoin<GXMenu, GXMenuLink>(m => m.Id, l => l.Menu!);
                    linked.Where.And<GXMenuLink>(l => l.Preload == true && l.Enabled == true && l.Removed == null);
                    foreach (var menu in await connection.SelectAsync<GXMenu>(linked, cancellationToken))
                    {
                        if (!string.IsNullOrWhiteSpace(menu.Name))
                        {
                            menuNames.Add(menu.Name);
                        }
                    }
                }
                else
                {
                    // ReadAsync below applies link policies before deciding whether
                    // any individual link qualifies this menu for preloading.
                    foreach (var menu in await menus.ListAsync(new ListMenus { Filter = new GXMenu { Kind = MenuKind.All, Enabled = true } }, null, cancellationToken))
                    {
                        if (menu.Enabled == true && menu.Removed == null && !string.IsNullOrWhiteSpace(menu.Name))
                        {
                            menuNames.Add(menu.Name);
                        }
                    }
                }
            });
            await Read("preload-appearances", async () =>
            {
                foreach (var image in await appearances.ListAsync(new ListAppearances { Filter = new GXAppearance { Preload = true, Enabled = true, ResourceType = (byte)ResourceType.Image } }, null, cancellationToken))
                {
                    if (image.Preload == true && image.Enabled == true && image.ResourceType == (byte)ResourceType.Image)
                    {
                        imageNames.Add(image.Name);
                    }
                }
            });
            await Read("preload-texts", async () =>
            {
                var codes = new[] { CultureInfo.GetCultureInfo(language).TwoLetterISOLanguageName, "en" }.Distinct().ToArray();
                if (database != null)
                {
                    await using var connection = await database.GetConnectionAsync(cancellationToken);
                    var selected = GXSelectArgs.Select<GXLocalizedResource>(r => r.Hash!, r => r.Preload == true);
                    selected.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(r => r.Language!, l => l.Id);
                    selected.Where.And<GXLanguage>(l => codes.Contains(l.Id));
                    foreach (var resource in await connection.SelectAsync<GXLocalizedResource>(selected, cancellationToken))
                    {
                        if (!string.IsNullOrEmpty(resource.Hash))
                        {
                            preloadHashes.Add(resource.Hash);
                        }
                    }
                }
                else
                {
                    foreach (var resource in await texts.ListAsync(new ListLocalizedResources { Filter = new GXLocalizedResource { Preload = true } }, null, cancellationToken))
                    {
                        if (resource.Preload == true && resource.Language != null && codes.Contains(resource.Language.Id) && !string.IsNullOrEmpty(resource.Hash))
                        {
                            preloadHashes.Add(resource.Hash);
                        }
                    }
                }
            });
        }
        var scopes = HttpContext?.RequestServices?.GetService<IServiceScopeFactory>();
        var versions = scopes != null && database != null && menuNames.Count != 0 ? await ReadMenuVersionsAsync(menuNames, cancellationToken) : null;
        var loadedMenus = scopes != null && menuNames.Count != 0
            ? await GXInitialMenuLoader.LoadAsync(menuNames, scopes, cancellationToken, versions, database, () => ReadMenuVersionsAsync(menuNames, cancellationToken)) : null;
        foreach (var name in menuNames)
        {
            await Read("menu:" + name, async () =>
        {
            GXMenu menu;
            if (loadedMenus != null && loadedMenus.TryGetValue(name, out var loaded))
            {
                if (loaded.Error != null)
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(loaded.Error).Throw();
                }
                menu = loaded.Menu!;
            }
            else
            {
                menu = await menus.ReadAsync(name, null, cancellationToken);
            }
            if (string.IsNullOrWhiteSpace(menu.Name))
            {
                return;
            }

            var requested = request.MenuNames.Contains(name, StringComparer.Ordinal);
            var links = (menu.Links ?? []).Where(link => link.Enabled == true && link.Removed == null).ToArray();
            if (!requested && (menu.Enabled != true || menu.Removed != null || menu.PublishTime > DateTimeOffset.Now ||
                menu.Preload != true && !links.Any(link => link.Preload == true)))
            {
                return;
            }

            var group = new GXInitialResourceGroup { Version = GXMenuHttpCache.ETag(menu) };
            if (Needs("menu:" + name, group.Version, "current"))
            {
                group.Values["current"] = JsonSerializer.Serialize(new GetMenuResponse { Item = menu }, JsonSerializerOptions.Web);
            }

            foreach (var link in links.Where(link => requested || menu.Preload == true || link.Preload == true))
            {
                if (!string.IsNullOrWhiteSpace(link.Name))
                {
                    names.Add(link.Name);
                }

                if (!string.IsNullOrWhiteSpace(link.Icon))
                {
                    imageNames.Add(link.Icon);
                }
            }
            response.Resources["menu:" + name] = group;
        });
        }

        foreach (var (name, type) in request.IncludeDefaults ? new[] { ("theme", ResourceType.Theme), ("iconpack", ResourceType.Iconpack) } : [])
        {
            await Read(name, async () =>
        {
            var version = (await appearances.LastChanged((byte)type, cancellationToken) ?? DateTimeOffset.MinValue).ToString("O", CultureInfo.InvariantCulture);
            var group = new GXInitialResourceGroup { Version = version };
            var items = (await appearances.ListAsync(new ListAppearances { Filter = new GXAppearance { ResourceType = (byte)type, Enabled = true } }, null, cancellationToken)).ToArray();
            if (type == ResourceType.Theme)
            {
                var selected = items.OrderByDescending(a => a.Priority).ThenBy(a => a.Name, StringComparer.Ordinal).FirstOrDefault();
                if (selected?.Preload != true)
                {
                    return;
                }

                if (Needs(name, version, "current"))
                {
                    group.Values["current"] = selected.Value;
                }
            }
            else
            {
                if (!items.All(i => i.Preload == true))
                {
                    var individual = new GXInitialResourceGroup { Version = version };
                    foreach (var item in items.Where(i => i.Preload == true))
                    {
                        if (Needs("appearance:iconpack", version, item.Name))
                        {
                            individual.Values[item.Name] = JsonSerializer.Serialize(item);
                        }
                    }

                    response.Resources["appearance:iconpack"] = individual;
                    return;
                }
                if (Needs(name, version, "current"))
                {
                    group.Values["current"] = JsonSerializer.Serialize(items.Select(i => i.Value).Where(v => !string.IsNullOrEmpty(v)));
                }
            }
            response.Resources[name] = group;
        });
        }

        if (imageNames.Count != 0)
        {
            imageNames.Add("Unknown");
        }

        await Read("image", async () =>
                {
                    var version = (await appearances.LastChanged((byte)ResourceType.Image, cancellationToken) ?? DateTimeOffset.MinValue).ToString("O", CultureInfo.InvariantCulture);
                    var group = new GXInitialResourceGroup { Version = version };
                    foreach (var name in imageNames)
                    {
                        if (Needs("image", version, name))
                        {
                            try
                            {
                                group.Values[name] = (await appearances.ReadByNameAsync((int)ResourceType.Image, name, null, cancellationToken))?.Value;
                            }
                            catch (Gurux.DLMS.AMI.Shared.GXAmiForbiddenException)
                            {
                                group.Values[name] = null;
                            }
                            catch (UnauthorizedAccessException)
                            {
                                group.Values[name] = null;
                            }
                        }
                    }

                    response.Resources["image"] = group;
                });
        var languageId = CultureInfo.GetCultureInfo(language).TwoLetterISOLanguageName;
        var textGroup = "text:" + languageId;
        await Read(textGroup, async () =>
        {
            var version = (await texts.LastChanged() ?? DateTimeOffset.MinValue).ToString("O", CultureInfo.InvariantCulture);
            var group = new GXInitialResourceGroup { Version = version };
            foreach (var name in names)
            {
                var hash = ClientHelpers.GetHashCode(name);
                if (Needs(textGroup, version, hash))
                {
                    group.Values[hash] = (await texts.ReadAsync(languageId, hash, null, cancellationToken))?.Value ?? name;
                }
            }
            foreach (var hash in preloadHashes)
            {
                if (Needs(textGroup, version, hash))
                {
                    var value = (await texts.ReadAsync(languageId, hash, null, cancellationToken))?.Value;
                    if (string.IsNullOrEmpty(value) && languageId != "en")
                    {
                        value = (await texts.ReadAsync("en", hash, null, cancellationToken))?.Value;
                    }

                    if (value != null)
                    {
                        group.Values[hash] = value;
                    }
                }
            }

            response.Resources[textGroup] = group;
        });
        return response;
    }
}
