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

using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using NuGet.Versioning;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Verified source preview and explicit transactional template installation. Never invokes downloaded code.</summary>
public sealed class GXScriptCatalogService(GXScriptCatalogStore catalog, IDbConnectionFactory database,
    IScriptRepository scripts, IScriptGroupRepository groups, IAuthorizationService authorization,
    IHttpContextAccessor accessor, IHttpClientFactory clients, NotificationCache notifications,
    IWorkflowHandler workflow, ILogger<GXScriptCatalogService> logger)
{
    private System.Security.Claims.ClaimsPrincipal User => accessor.HttpContext?.User ?? throw new UnauthorizedAccessException();

    private async Task<GXScript?> ReadTargetAsync(Guid? id, string catalogId, CancellationToken token)
    {
        if (!(await authorization.AuthorizeAsync(User, null, id == null ? GXScriptPolicies.Add : GXScriptPolicies.Edit)).Succeeded)
        {
            throw new UnauthorizedAccessException();
        }
        if (id == null)
        {
            return null;
        }
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Invalid script identifier.");
        }
        var script = await scripts.ReadAsync(id.Value, null, token);
        if (script.Removed != null || !string.Equals(script.CatalogId, catalogId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The script is not an installed instance of this catalog template.");
        }
        if (script.Language != ScriptLanguage.CSharp)
        {
            throw new InvalidOperationException("Catalog templates require C#.");
        }
        return script;
    }

    private async Task<(Gurux.Updater.Model.GXScriptManifest Manifest, string Source)> DownloadAsync(GXScriptTemplateRelease release, CancellationToken token)
    {
        var manifest = JsonSerializer.Deserialize<Gurux.Updater.Model.GXScriptManifest>(release.ManifestJson)
            ?? throw new InvalidDataException("Missing script manifest.");
        Gurux.Updater.Services.GXScriptService.Validate(manifest);
        using var http = clients.CreateClient("ScriptCatalog");
        var reader = new Gurux.Updater.Services.GXCatalogSourceReader(http, allowLoopbackHttp: true);
        var bytes = await reader.ReadAsync(reader.ResolveReference(release.CatalogSource, manifest.Path), token);
        if (bytes.LongLength != manifest.Size ||
            !Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).Equals(manifest.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Script size or SHA-256 mismatch.");
        }
        try
        {
            var source = new System.Text.UTF8Encoding(false, true).GetString(bytes);
            if (source.Contains('\0'))
            {
                throw new InvalidDataException("Script contains NUL bytes.");
            }
            return (manifest, source);
        }
        catch (System.Text.DecoderFallbackException error)
        {
            throw new InvalidDataException("Script must be UTF-8 text.", error);
        }
    }

    /// <summary>Downloads and validates an exact release and returns original/local/new source.</summary>
    public async Task<ScriptTemplatePreview> PreviewAsync(PreviewScriptTemplate request, CancellationToken token = default)
    {
        var script = await ReadTargetAsync(request.ScriptId, request.CatalogId, token);
        var release = await catalog.GetAsync(request.CatalogId, request.Version, token);
        var (manifest, source) = await DownloadAsync(release, token);
        return new ScriptTemplatePreview {
            ScriptId = script?.Id, CatalogId = release.CatalogId, Version = release.Version, Sha256 = manifest.Sha256,
            ConcurrencyStamp = script?.ConcurrencyStamp, OriginalSource = script?.CatalogSourceCode,
            CurrentSource = script?.SourceCode, NewSource = source, Customized = script != null && script.SourceCode != script.CatalogSourceCode,
            Description = manifest.Description, Author = manifest.Author, License = manifest.License
        };
    }

    /// <summary>Compiles verified source, checks the preview token and commits code, provenance, methods and backup together.</summary>
    public async Task<Guid> ApplyAsync(ApplyScriptTemplate request, CancellationToken token = default)
    {
        var script = await ReadTargetAsync(request.ScriptId, request.CatalogId, token);
        var release = await catalog.GetAsync(request.CatalogId, request.Version, token);
        var (manifest, source) = await DownloadAsync(release, token);
        if (!string.Equals(request.ExpectedSha256, manifest.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The selected release differs from the preview. Compare again.");
        }
        if (script != null)
        {
            if (script.ConcurrencyStamp != request.ExpectedConcurrencyStamp || string.IsNullOrEmpty(request.ExpectedConcurrencyStamp))
            {
                throw new InvalidOperationException("The script changed after the preview. Compare again.");
            }
            if (script.SourceCode != script.CatalogSourceCode && !request.OverwriteLocalChanges)
            {
                throw new InvalidOperationException("The script has local edits. Explicitly accept replacing them after comparison.");
            }
            if (!NuGetVersion.TryParse(script.CatalogVersion, out var installed) || NuGetVersion.Parse(release.Version) < installed)
            {
                throw new InvalidOperationException("Select the installed template version or a newer template version.");
            }
        }
        var selectedGroups = new List<GXScriptGroup>();
        if (script == null)
        {
            var ids = request.GroupIds?.Distinct().ToArray();
            selectedGroups = [.. (await groups.ListAsync(new ListScriptGroups {
                Included = ids?.Length > 0 ? ids : null,
                Filter = ids?.Length > 0 ? null : new GXScriptGroup { Default = true }
            }, null, token))];
            if (selectedGroups.Count == 0 || ids?.Length > 0 && selectedGroups.Count != ids.Length)
            {
                throw new UnauthorizedAccessException("Select accessible script groups or configure a default group.");
            }
        }
        var localId = script?.Id ?? Guid.NewGuid();
        var methods = new List<GXScriptMethod>();
        var assembly = scripts.Compile(localId.ToString(), source, script?.Namespaces, methods, out var errors, out _, token);
        if (assembly == null || errors != null || methods.Count == 0)
        {
            throw new InvalidDataException("Template compilation failed or it exposes no public instance methods. " + errors);
        }
        if (methods.Select(m => m.Name).Distinct(StringComparer.Ordinal).Count() != methods.Count)
        {
            throw new InvalidDataException("Template method names must be unique.");
        }

        await using var db = await database.GetConnectionAsync(token);
        using var transaction = db.BeginTransaction(System.Data.IsolationLevel.Serializable);
        if (script != null)
        {
            var current = await db.SingleOrDefaultAsync<GXScript>(transaction, GXSelectArgs.Select<GXScript>(s => new { s.Id, s.ConcurrencyStamp, s.Removed, s.CatalogId }, s => s.Id == localId), token);
            if (current == null || current.Removed != null || current.ConcurrencyStamp != request.ExpectedConcurrencyStamp || current.CatalogId != request.CatalogId)
            {
                throw new InvalidOperationException("The script changed during preparation. Compare again.");
            }
        }
        // Recheck withdrawal/content identity inside the write transaction after the download and compilation.
        var retained = await db.SingleOrDefaultAsync<GXScriptTemplateRelease>(transaction, GXSelectArgs.SelectAll<GXScriptTemplateRelease>(r => r.Id == release.Id), token);
        if (retained?.Enabled != true || retained.ManifestJson != release.ManifestJson)
        {
            throw new InvalidOperationException("The release was withdrawn during preparation.");
        }

        var parent = new GXScript { Id = localId };
        var existingMethods = script == null ? [] : await db.SelectAsync<GXScriptMethod>(transaction,
            GXSelectArgs.SelectAll<GXScriptMethod>(m => m.Script == parent), token);
        if (existingMethods.Any(old => !methods.Any(next => next.Name == old.Name)))
        {
            throw new InvalidOperationException("The new template removes an existing entry method. Import it as a separate script instead.");
        }

        var candidate = script ?? new GXScript(request.Name ?? (manifest.Id.Length <= 64 ? manifest.Id : manifest.Id[..64])) {
            Id = localId, Creator = new GXUser { Id = ServerHelpers.GetUserId(User) }, Enabled = false,
            Description = manifest.Description,
            CreationTime = DateTimeOffset.UtcNow, Language = ScriptLanguage.CSharp
        };
        if (string.IsNullOrWhiteSpace(candidate.Name) || candidate.Name.Length > 64)
        {
            throw new ArgumentException("Script name must contain 1–64 characters.");
        }
        if (script != null)
        {
            await db.InsertAsync(transaction, GXInsertArgs.Insert(new GXScriptTemplateHistory {
                    Id = Guid.NewGuid(), ScriptId = localId, CatalogVersion = script.CatalogVersion,
                    SourceCode = script.SourceCode ?? "", Namespaces = script.Namespaces,
                    UserId = ServerHelpers.GetUserId(User), CreationTime = DateTimeOffset.UtcNow
                }), token);
        }
        candidate.SourceCode = source; candidate.ByteAssembly = assembly;
        candidate.CatalogId = release.CatalogId; candidate.CatalogVersion = release.Version;
        candidate.CatalogSha256 = manifest.Sha256; candidate.CatalogSourceCode = source;
        candidate.ConcurrencyStamp = Guid.NewGuid().ToString(); candidate.Updated = DateTimeOffset.UtcNow;
        if (script == null)
        {
            var insert = GXInsertArgs.Insert(candidate);
            insert.Exclude<GXScript>(s => new { s.Methods, s.ScriptGroups, s.Workflows, s.Logs, s.CompilationDependencies });
            await db.InsertAsync(transaction, insert, token);
            foreach (var group in selectedGroups)
            {
                await db.InsertAsync(transaction, GXInsertArgs.Insert(new GXScriptGroupScript { ScriptId = localId, ScriptGroupId = group.Id, CreationTime = DateTimeOffset.UtcNow }), token);
            }
        }
        else
        {
            await db.UpdateAsync(transaction, GXUpdateArgs.Update(candidate, s => new {
                s.SourceCode, s.ByteAssembly, s.CatalogId, s.CatalogVersion, s.CatalogSha256, s.CatalogSourceCode, s.Updated, s.ConcurrencyStamp
            }), token);
        }

        foreach (var method in methods)
        {
            method.Script = new GXScript { Id = localId };
            var previous = existingMethods.SingleOrDefault(m => m.Name == method.Name);
            if (previous == null)
            {
                method.Id = Guid.NewGuid();
                var insert = GXInsertArgs.Insert(method);
                insert.Exclude<GXScriptMethod>(m => new { m.Blocks, m.Agents, m.Notifications });
                await db.InsertAsync(transaction, insert, token);
            }
            else
            {
                method.Id = previous.Id;
                await db.UpdateAsync(transaction, GXUpdateArgs.Update(method, m => new { m.Description, m.IsFunction, m.IsAsynchronous }), token);
            }
        }
        candidate.CompilationDependencies = Gurux.DLMS.AMI.Script.GXScriptCompiler.ReadDependencies(assembly);
        GXScriptDependencyStorage.Replace(db, transaction, localId, candidate.CompilationDependencies);
        transaction.Commit();
        candidate.Methods = methods;
        try
        {
            workflow.UnloadScript(localId);
            workflow.Update(candidate);
        }
        catch (Exception error)
        {
            logger.LogError(error, "Committed script {Id} could not refresh its server cache", localId);
        }
        try
        {
            notifications.Add(await scripts.GetUsersAsync(localId, CancellationToken.None), new GXTarget(script == null ? GXScriptPolicies.Add : GXScriptPolicies.Edit, localId, candidate.Name) { ConcurrencyStamp = candidate.ConcurrencyStamp });
        }
        catch (Exception error)
        {
            logger.LogError(error, "Committed script {Id} could not publish its notification", localId);
        }
        return localId;
    }

    /// <summary>Reads saved pre-update sources only after normal script/group authorization.</summary>
    public async Task<List<GXScriptTemplateHistory>> HistoryAsync(Guid scriptId, CancellationToken token = default)
    {
        _ = await scripts.ReadAsync(scriptId, null, token);
        await using var db = await database.GetConnectionAsync(token);
        return [.. (await db.SelectAsync<GXScriptTemplateHistory>(GXSelectArgs.SelectAll<GXScriptTemplateHistory>(r => r.ScriptId == scriptId), token)).OrderByDescending(r => r.CreationTime)];
    }
}
