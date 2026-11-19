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

using System.Data;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server;
using ResourceType = Gurux.DLMS.AMI.Client.Shared.Enums.ResourceType;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;
using Gurux.UI.Components.Enums;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Applies packaged application data updates before accepting requests.</summary>
internal static class GXApplicationUpdates
{
    /// <summary>Resolves localized user interface labels from the client resource assembly.</summary>
    private static readonly System.Resources.ResourceManager _labels = new("Gurux.DLMS.AMI.Client.Properties.Resources", typeof(Client.Pages.Script.Scripts).Assembly);
    /// <summary>Persistent state record name used by application update processing.</summary>
    private const string StateName = "application-updates";
    /// <summary>Serializes operations that mutate shared state in this component.</summary>
    private static readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Deserialized application release metadata containing navigation and appearance contributions.</summary>
    internal sealed class Definition
    {
        /// <summary>Application release version associated with these default contributions.</summary>
        public string Version { get; set; } = "";
        /// <summary>Navigation links contributed by this application release.</summary>
        public List<Link> Links { get; set; } = [];
        /// <summary>Appearance resources contributed by this application release.</summary>
        public List<Appearance> Appearances { get; set; } = [];
    }

    /// <summary>Deserialized appearance resource associated with an application release.</summary>
    internal sealed class Appearance
    {
        /// <summary>Identifier assigned to the contributed appearance resource.</summary>
        public string Name { get; set; } = "";
        /// <summary>Classification used to select the resource storage and rendering behavior.</summary>
        public ResourceType ResourceType { get; set; }
        /// <summary>Serialized appearance resource value retained in application storage.</summary>
        public string Value { get; set; } = "";
    }

    /// <summary>Deserialized navigation link contributed by an application release.</summary>
    internal sealed class Link
    {
        /// <summary>Stable identifier of the navigation menu to create or update.</summary>
        public string Menu { get; set; } = "";
        /// <summary>Destination URL associated with the navigation entry.</summary>
        public string Url { get; set; } = "";
        /// <summary>Resource key used to localize the navigation entry title.</summary>
        public string TitleResource { get; set; } = "";
        /// <summary>Icon identifier displayed beside the navigation title.</summary>
        public string Icon { get; set; } = "";
        /// <summary>Optional sibling menu identifier before which this entry is inserted.</summary>
        public string? Before { get; set; }
        /// <summary>Authorization policy required to display the navigation entry.</summary>
        public string? Policy { get; set; }
        /// <summary>Optional component type associated with the navigation entry.</summary>
        public string? Component { get; set; }
    }

    /// <summary>Loads, validates, and orders the application-update definitions embedded in the server assembly.</summary>
    /// <returns>The validated application-update definitions ordered by version.</returns>
    /// <exception cref="InvalidDataException">An embedded update definition is invalid or duplicates another version.</exception>
    internal static IReadOnlyList<Definition> ReadDefinitions()
    {
        var assembly = typeof(GXApplicationUpdates).Assembly;
        var parser = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithDuplicateKeyChecking().Build();
        var definitions = new List<Definition>();
        foreach (var name in assembly.GetManifestResourceNames().Where(name => name.Contains(".Updates.", StringComparison.Ordinal) && name.EndsWith(".yaml", StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var text = new StreamReader(stream);
            var definition = parser.Deserialize<Definition>(text);
            if (definition == null || !Version.TryParse(definition.Version, out var version) || version.Revision < 0 ||
                !name.EndsWith("." + definition.Version + ".yaml", StringComparison.Ordinal) || (definition.Links.Count == 0 && definition.Appearances.Count == 0))
            {
                throw new InvalidDataException("Invalid application update: " + name);
            }
            foreach (var link in definition.Links)
            {
                if (string.IsNullOrWhiteSpace(link.Menu) || string.IsNullOrWhiteSpace(link.Url) ||
                    string.IsNullOrWhiteSpace(link.TitleResource) ||
                    _labels.GetString(link.TitleResource, System.Globalization.CultureInfo.InvariantCulture) == null ||
                    link.Component != null && typeof(Client.Pages.Script.ScriptTemplates).Assembly.GetType(link.Component) == null)
                {
                    throw new InvalidDataException("Invalid menu link in " + name);
                }
            }
            foreach (var appearance in definition.Appearances)
            {
                if (string.IsNullOrWhiteSpace(appearance.Name) || string.IsNullOrWhiteSpace(appearance.Value) ||
                    !Enum.IsDefined(appearance.ResourceType))
                {
                    throw new InvalidDataException("Invalid appearance in " + name);
                }
            }
            definitions.Add(definition);
        }
        if (definitions.Select(item => item.Version).Distinct().Count() != definitions.Count)
        {
            throw new InvalidDataException("Duplicate application update version.");
        }
        return [.. definitions.OrderBy(item => Version.Parse(item.Version))];
    }

    /// <summary>Applies unapplied packaged application updates supported by the installed version inside serialized transactions.</summary>
    /// <param name="connection">The open database connection used for the operation.</param>
    /// <param name="installedVersion">The installed application version limiting which packaged updates may run.</param>
    /// <param name="token">Cancels the database or remote operation.</param>
    /// <returns>A task that completes after applicable updates have been committed or deferred until initialization is ready.</returns>
    internal static async Task ApplyAsync(GXDbConnection connection, Version installedVersion,
        CancellationToken token = default)
    {
        var definitions = ReadDefinitions().Where(item => Version.Parse(item.Version) <= installedVersion).ToArray();
        if (definitions.Length == 0)
        {
            return;
        }
        var schema = new GXSchemaManager(connection);
        if (!schema.TableExist<GXMenu>() || !schema.TableExist<GXMenuLink>() || !schema.TableExist<GXConfiguration>() ||
            definitions.Any(item => item.Appearances.Count != 0) && !schema.TableExist<GXAppearance>())
        {
            // Initial installation creates these tables and default menus separately.
            return;
        }
        await _gate.WaitAsync(token);
        try
        {
            foreach (var definition in definitions)
            {
                using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                var state = await connection.SingleOrDefaultAsync<GXConfiguration>(transaction,
                    GXSelectArgs.SelectAll<GXConfiguration>(item => item.Name == StateName), token);
                HashSet<string> applied = string.IsNullOrWhiteSpace(state?.Settings) ? [] :
                    JsonSerializer.Deserialize<HashSet<string>>(state.Settings) ?? throw new InvalidDataException("Invalid application update state.");
                if (applied.Contains(definition.Version))
                {
                    transaction.Commit();
                    continue;
                }
                var menus = await connection.SelectAsync<GXMenu>(transaction,
                    GXSelectArgs.SelectAll<GXMenu>(menu => menu.Removed == null), token);
                if (definition.Links.Any(link => !menus.Any(menu => menu.Name == link.Menu)))
                {
                    // Do not record completion before the installation has created its menus.
                    return;
                }
                foreach (var appearance in definition.Appearances)
                {
                    await AddAppearanceAsync(connection, transaction, appearance, token);
                }
                foreach (var link in definition.Links)
                {
                    foreach (var menu in menus.Where(menu => menu.Name == link.Menu))
                    {
                        await AddLinkAsync(connection, transaction, menu, link, token);
                    }
                }
                applied.Add(definition.Version);
                var added = state == null;
                state ??= new GXConfiguration { Id = Guid.NewGuid(), Name = StateName, Enabled = false };
                state.Settings = JsonSerializer.Serialize(applied.Order(StringComparer.Ordinal));
                if (added)
                {
                    await connection.InsertAsync(transaction, GXInsertArgs.Insert(state), token);
                }
                else
                {
                    await connection.UpdateAsync(transaction, GXUpdateArgs.Update(state, item => item.Settings!), token);
                }
                transaction.Commit();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Adds a packaged appearance under the default administrator when no appearance with that name exists.</summary>
    /// <param name="connection">The open database connection used for the operation.</param>
    /// <param name="transaction">The transaction that owns the database changes.</param>
    /// <param name="definition">The validated packaged appearance or menu-link definition to apply.</param>
    /// <param name="token">Cancels the database or remote operation.</param>
    /// <returns>A task that completes after the missing appearance has been inserted or an existing one is retained.</returns>
    /// <exception cref="InvalidOperationException">A default administrator is unavailable to own the new appearance.</exception>
    private static async Task AddAppearanceAsync(GXDbConnection connection, IDbTransaction transaction,
        Appearance definition, CancellationToken token)
    {
        var appearances = await connection.SelectAsync<GXAppearance>(transaction,
            GXSelectArgs.Select<GXAppearance>(item => new { item.Id, item.Name }), token);
        if (appearances.Any(item => string.Equals(item.Name, definition.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }
        var creator = await ServerSettings.GetDefaultAdmin(connection, transaction, token)
            ?? throw new InvalidOperationException("A default administrator is required to create update appearances.");
        var appearance = new GXAppearance
        {
            Id = Guid.NewGuid(),
            Name = definition.Name,
            ResourceType = (byte)definition.ResourceType,
            Value = definition.Value,
            Enabled = true,
            Preload = false,
            AccessVisibility = ResourceVisibility.Public,
            Creator = creator,
            CreationTime = DateTimeOffset.UtcNow,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        await connection.InsertAsync(transaction, GXInsertArgs.Insert(appearance), token);
    }

    /// <summary>Adds a missing packaged menu link while preserving existing links and administrator customizations.</summary>
    /// <param name="connection">The open database connection used for the operation.</param>
    /// <param name="transaction">The transaction that owns the database changes.</param>
    /// <param name="menu">The existing menu that receives the packaged link.</param>
    /// <param name="definition">The validated packaged appearance or menu-link definition to apply.</param>
    /// <param name="token">Cancels the database or remote operation.</param>
    /// <returns>A task that completes after the missing menu link and any ordering changes have been persisted.</returns>
    private static async Task AddLinkAsync(GXDbConnection connection, IDbTransaction transaction, GXMenu menu, Link definition, CancellationToken token)
    {
        var links = await connection.SelectAsync<GXMenuLink>(transaction,
            GXSelectArgs.SelectAll<GXMenuLink>(link => link.Menu == menu && link.Removed == null), token);
        static string Normalize(string? url) => (url ?? "").Trim('/');
        if (links.Any(link => string.Equals(Normalize(link.Url), Normalize(definition.Url), StringComparison.OrdinalIgnoreCase)))
        {
            // Preserve existing links, including administrator customizations.
            return;
        }
        var before = definition.Before == null ? null : links.FirstOrDefault(link =>
            string.Equals(Normalize(link.Url), Normalize(definition.Before), StringComparison.OrdinalIgnoreCase));
        var order = before?.Order ?? (links.Count == 0 ? 1 : checked(links.Max(link => link.Order) + 1));
        if (before != null)
        {
            foreach (var link in links.Where(link => link.Order >= order))
            {
                link.Order = checked(link.Order + 1);
                await connection.UpdateAsync(transaction, GXUpdateArgs.Update(link, item => item.Order), token);
            }
        }
        var item = new GXMenuLink
        {
            Id = Guid.NewGuid(),
            Menu = menu,
            Name = _labels.GetString(definition.TitleResource, System.Globalization.CultureInfo.InvariantCulture),
            Url = definition.Url,
            Icon = definition.Icon,
            Order = order,
            Enabled = true,
            Visibility = (byte)Visibility.All,
            CreationTime = DateTimeOffset.UtcNow,
            MenuLinkType = definition.Component == null ? MenuLinkType.Link : MenuLinkType.Content,
            TargetType = definition.Component
        };
        await connection.InsertAsync(transaction, GXInsertArgs.Insert(item), token);
        await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXMenuLink), item.Id,
            definition.Policy == null ? [] : [definition.Policy], token, transaction);
    }
}
