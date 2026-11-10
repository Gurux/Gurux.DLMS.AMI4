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

using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Gurux.UI.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using Gurux.DLMS.AMI.Server.Services;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public partial class AppearanceRepository : IAppearanceRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IAuthorizationService _authorizationService;
        private readonly NotificationCache _notificationCache;
        private readonly GXImageStorage _imageStorage;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AppearanceRepository(
            IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IHttpContextAccessor httpContextAccessor,
            ILogRepository systemLogRepository,
            IEnumTypeRepository enumTypeRepository,
            IConfigurationRepository configurationRepository,
            NotificationCache notificationCache,
            GXImageStorage imageStorage)
        {
            _ = configurationRepository;
            _ = systemLogRepository;
            _ = enumTypeRepository;
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _dbFactory = dbFactory;
            _authorizationService = authorizationService;
            _notificationCache = notificationCache;
            _imageStorage = imageStorage;
        }

        private async Task<List<string>> GetNotifiedUsers(Guid id, NotificationAction action)
        {
            _ = id;
            GXSelectArgs args = GXQuery.GetUsersByPermission(action, GXAppearancePolicies.View);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            List<GXUser> users = connection.Select<GXUser>(args);
            return [.. users.Select(s => s.Id!)];
        }

        /// <inheritdoc />
        public async Task DeleteAsync(IEnumerable<Guid> appearances, bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            appearances = appearances.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXAppearance, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, appearances,
        async () =>
        {
            GXAppearanceAccess.RequireAuthenticated(User);
            appearances.ValidateIds();
            var arg = GXSelectArgs.Select<GXAppearance>(a => new { a.Id, a.AccessVisibility, a.Creator, a.ResourceType, a.StorageKey }, q => appearances.Contains(q.Id));
            arg.Joins.AddLeftJoin<GXAppearance, GXUser>(a => a.Creator!, u => u.Id!);
            arg.Columns.Add<GXUser>(u => u.Id!);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            try
            {
                List<GXAppearance> list = connection.Select<GXAppearance>(arg);
                foreach (var image in list)
                {
                    if (GXAppearanceAccess.IsOwnerOnly(image))
                    {
                        GXAppearanceAccess.RequirePrivateOwner(User, image);
                    }
                    else
                    {
                        await _authorizationService.EnsureAnyPolicyAsync(User, GXAppearancePolicies.Delete);
                    }
                }
                foreach (var image in list)
                {
                    await GXProfilePictures.EnsureNotReferencedAsync(connection, image.Id, cancellationToken);
                }

                DateTime now = DateTime.Now;
                foreach (GXAppearance it in list)
                {
                    it.Removed = now;
                    if (delete)
                    {
                        await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXAppearance>(it.Id), cancellationToken);
                        if (it.StorageKey != null)
                        {
                            _imageStorage.Delete(it.StorageKey);
                        }
                    }
                    else
                    {
                        connection.Update(GXUpdateArgs.Update(it, q => q.Removed!));
                    }
                }
                foreach (var type in list.Select(a => a.ResourceType).OfType<byte>().Distinct())
                {
                    await Update(type, cancellationToken);
                }

                _notificationCache.Add(await GetNotifiedUsers(Guid.Empty, NotificationAction.Remove), new GXTarget(GXAppearancePolicies.Delete));
            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        await DeleteAsync(appearances, delete, notify, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXAppearance>> ListAsync(
                ListAppearances? request,
                ListAppearancesResponse? response,
                CancellationToken cancellationToken = default)
        {
            //All appearances are available can see all the appearances.
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAppearance>(a => a.Removed == null);
            GXAppearanceAccess.Restrict(arg, User);
            arg.Joins.AddLeftJoin<GXAppearance, GXUser>(j => j.Creator!, j => j.Id!);
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            if (request?.Exclude != null && request.Exclude.Any())
            {
                arg.Where.And<GXAppearance>(w => !request.Exclude.Contains(w.Id));
            }
            if (request?.Included?.Any() == true)
            {
                arg.Where.And<GXAppearance>(w => request.Included.Contains(w.Id));
            }
            if (!string.IsNullOrEmpty(request?.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXAppearance>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXAppearance>(a => a.Priority, true);
                arg.OrderBy.Add<GXAppearance>(a => a.Name, false);
            }
            arg.Distinct = true;
            GXDateTimeFilters.Apply<GXAppearance>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            if (request?.AllUsers == true)
            {
                if (!User.IsInRole(GXRoles.Admin))
                {
                    throw new UnauthorizedAccessException();
                }

                await _authorizationService.EnsureAnyPolicyAsync(User, GXAppearancePolicies.View);
            }
            else if (!await GXAppearancePolicyQuery.RestrictAsync(connection, arg, _authorizationService, User, cancellationToken))
            {
                if (response != null)
                {
                    response.Count = 0;
                    response.Appearances = [];
                }
                return [];
            }
            // Keep joined identifier restrictions before text filters: the ORM's LIKE formatting
            // otherwise leaves subsequent collection IDs unqualified (ambiguous with GXUser.ID).
            if (request?.Filter != null)
            {
                arg.Where.FilterBy(request.Filter);
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXAppearance>(q => GXSql.Count(1));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = connection.SingleOrDefault<long>(total);
                    if (response.Count == 0)
                    {
                        //No need to retrieve data when none is available.
                        return [];
                    }
                }
                arg.Index = request.Index;
                arg.Count = request.Count;
            }
            List<GXAppearance> appearances = [.. (await connection.SelectAsync<GXAppearance>(arg))];
            foreach (var appearance in appearances)
            {
                appearance.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXAppearance), appearance.Id, cancellationToken);
                await HydrateImageAsync(appearance, cancellationToken);
            }

            if (response != null)
            {
                response.Appearances = [.. appearances];
                if (response.Count == 0)
                {
                    response.Count = appearances.Count;
                }
            }
            return [.. appearances];
        }

        private static readonly string UnknownImage = "<svg width=\"16\" height=\"16\" viewBox=\"0 0 16 16\" xmlns=\"http://www.w3.org/2000/svg\">\r\n  <!-- Box -->\r\n  <rect x=\"1\" y=\"1\" width=\"14\" height=\"14\" stroke=\"black\" stroke-width=\"1\" fill=\"none\" />\r\n  \r\n  <!-- Cross -->\r\n  <line x1=\"1\" y1=\"1\" x2=\"15\" y2=\"15\" stroke=\"black\" stroke-width=\"1\" />\r\n  <line x1=\"15\" y1=\"1\" x2=\"1\" y2=\"15\" stroke=\"black\" stroke-width=\"1\" />\r\n</svg>\r\n";

        /// <summary>Gets or sets the user1.</summary>
        public ClaimsPrincipal User1 => User;

        /// <inheritdoc />
        public Task<GXAppearance?> ReadAsync(int type, Guid id, Expression<Func<GXAppearance, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Appearance identifier is required.", nameof(id));
            }
            return ReadCoreAsync(type, id, null, columns, cancellationToken);
        }

        /// <inheritdoc />
        public Task<GXAppearance?> ReadByNameAsync(int type, string name, Expression<Func<GXAppearance, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (name.Length > 64)
            {
                throw new ArgumentException("Appearance name is too long.", nameof(name));
            }
            return ReadCoreAsync(type, null, name, columns, cancellationToken);
        }

        private async Task<GXAppearance?> ReadCoreAsync(int type, Guid? id, string? name,
            Expression<Func<GXAppearance, object>>? columns, CancellationToken cancellationToken)
        {
            _ = columns;
            ArgumentOutOfRangeException.ThrowIfNegative(type);
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAppearance>(w => w.ResourceType == type && w.Removed == null);
            if (id.HasValue)
            {
                arg.Where.And<GXAppearance>(w => w.Id == id.Value);
            }
            else
            {
                arg.Where.And<GXAppearance>(w => w.Name == name);
            }
            GXAppearanceAccess.Restrict(arg, User);
            arg.Joins.AddLeftJoin<GXAppearance, GXUser>(j => j.Creator!, j => j.Id!);
            arg.Columns.Add<GXUser>(s => new { s.Id, s.UserName });
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            GXAppearance? appearance = await connection.SingleOrDefaultAsync<GXAppearance>(arg, cancellationToken);
            if (appearance == null && name != null)
            {
                // Preserve symbolic resource casing consistently on providers with case-sensitive collations.
                var names = await connection.SelectAsync<GXAppearance>(
                    GXSelectArgs.Select<GXAppearance>(a => new { a.Id, a.Name }, a => a.ResourceType == type && a.Removed == null), cancellationToken);
                var named = names.SingleOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
                if (named != null)
                {
                    return await ReadCoreAsync(type, named.Id, null, columns, cancellationToken);
                }
            }
            if (appearance != null)
            {
                appearance.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXAppearance), appearance.Id, cancellationToken);
                if (GXAppearanceAccess.IsOwnerOnly(appearance) ? !GXAppearanceAccess.CanReadPrivate(User, appearance) :
                    !await GXResourcePolicyAccess.AllowsAsync(connection, _authorizationService, User, appearance.Policies, cancellationToken))
                {
                    throw new UnauthorizedAccessException();
                }
                await HydrateImageAsync(appearance, cancellationToken);
            }
            else if (name != null && type == (byte)ResourceType.Image)
            {
                // A missing symbolic image is rendered as a placeholder without creating a database identity.
                appearance = new GXAppearance((byte)ResourceType.Image, name, "", UnknownImage);
            }
            return appearance;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Guid>> UpsertAsync(IEnumerable<GXAppearance> appearances,
            Expression<Func<GXAppearance, object>>? columns, CancellationToken cancellationToken = default)
        {
            appearances = appearances.ToArray();
            return await GXHistoryMutation.RunAsync<GXAppearance, IEnumerable<Guid>>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, appearances,
        async () =>
        {
            try
            {

                GXAppearanceAccess.RequireAuthenticated(User);
                appearances = [.. appearances];
                if (appearances.GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1) ||
                    appearances.Where(a => a.Id != Guid.Empty).GroupBy(a => a.Id).Any(g => g.Count() > 1))
                {
                    throw new ArgumentException("Appearance identities and names must be unique within a batch.", nameof(appearances));
                }
                DateTime now = DateTime.Now;
                GXUser creator = new() { Id = ServerHelpers.GetUserId(User) };
                await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
                foreach (GXAppearance appearance in appearances)
                {
                    if (string.IsNullOrWhiteSpace(appearance.Name) || appearance.Name.Length > 64)
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }

                    var oldQuery = GXSelectArgs.Select<GXAppearance>(a => new { a.Id, a.Name, a.AccessVisibility, a.Creator, a.ConcurrencyStamp, a.ResourceType, a.Removed, a.StorageKey, a.ContentType, a.OriginalFileName }, a => a.Id == appearance.Id);
                    oldQuery.Joins.AddLeftJoin<GXAppearance, GXUser>(a => a.Creator!, u => u.Id!);
                    oldQuery.Columns.Add<GXUser>(u => u.Id!);
                    var old = await connection.SingleOrDefaultAsync<GXAppearance>(oldQuery, cancellationToken);
                    var visibility = old != null && (appearance.AccessVisibility == null ||
                        (columns != null && !ServerHelpers.Contains(columns, nameof(GXAppearance.AccessVisibility))))
                        ? old.AccessVisibility : appearance.AccessVisibility ?? ResourceVisibility.Private;
                    if (visibility == null || !Enum.IsDefined(visibility.Value))
                    {
                        throw new ArgumentException("Invalid resource visibility.");
                    }
                    appearance.AccessVisibility = visibility;
                    bool privateAfter = GXAppearanceAccess.IsOwnerOnly(appearance);
                    if (old != null && GXAppearanceAccess.IsOwnerOnly(old))
                    {
                        GXAppearanceAccess.RequirePrivateOwner(User, old);
                        if (!privateAfter)
                        {
                            await _authorizationService.EnsureAnyPolicyAsync(User, GXAppearancePolicies.Edit);
                        }
                    }
                    else if (old != null || !privateAfter)
                    {
                        await _authorizationService.EnsureAnyPolicyAsync(User, old == null ? GXAppearancePolicies.Add : GXAppearancePolicies.Edit);
                    }

                    var names = await connection.SelectAsync<GXAppearance>(
                        GXSelectArgs.Select<GXAppearance>(a => new { a.Id, a.Name }, a => a.Id != appearance.Id), cancellationToken);
                    var duplicate = names.FirstOrDefault(a => string.Equals(a.Name, appearance.Name, StringComparison.OrdinalIgnoreCase));
                    if (duplicate != null)
                    {
                        throw new ArgumentException("An appearance with the same name already exists.", nameof(appearances));
                    }
                    if (appearance.Id == Guid.Empty)
                    {
                        appearance.Id = Guid.NewGuid();
                    }
                    if (old?.Removed != null)
                    {
                        throw new InvalidOperationException("Removed appearances cannot be edited.");
                    }

                    appearance.ResourceType ??= old?.ResourceType;
                    if (appearance.ResourceType == null)
                    {
                        throw new ArgumentException("Appearance resource type is required.");
                    }
                    if (GXAppearanceAccess.IsOwnerOnly(appearance) && !User.IsInRole(GXRoles.Admin) &&
                        !(await _authorizationService.AuthorizeAsync(User, old == null ? GXAppearancePolicies.Add : GXAppearancePolicies.Edit)).Succeeded)
                    {
                        bool contentChanged = columns == null || ServerHelpers.Contains(columns, nameof(GXAppearance.Value)) ||
                            ServerHelpers.Contains(columns, nameof(GXAppearance.ResourceType)) || ServerHelpers.Contains(columns, nameof(GXAppearance.OriginalFileName));
                        if (old == null || contentChanged)
                        {
                            var image = _imageStorage.Decode(appearance.Value, requireImage: true);
                            if (appearance.ResourceType != (byte)ResourceType.Image || image == null || image.Value.ContentType == "image/svg+xml" ||
                                image.Value.Bytes.Length > 1_000_000 || GXImageStorage.OriginalName(appearance.OriginalFileName) == null)
                            {
                                throw new ArgumentException("Select a raster image file no larger than 1 MB.");
                            }
                        }
                    }

                    await GXResourcePolicyAccess.ValidateAsync(connection, appearance.Policies, cancellationToken);
                    if (old?.ResourceType == (byte)ResourceType.Image && (appearance.ResourceType != (byte)ResourceType.Image || appearance.Removed != null))
                    {
                        await GXProfilePictures.EnsureNotReferencedAsync(connection, appearance.Id, cancellationToken);
                    }
                    if (!string.IsNullOrEmpty(old?.ConcurrencyStamp) && old.ConcurrencyStamp != appearance.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }
                    await PersistImageAsync(appearance, old, columns, async () =>
                    {
                        using var transaction = connection.BeginTransaction();
                        if (old == null)
                        {
                            if (GXAppearanceAccess.IsOwnerOnly(appearance) && User.IsInRole(GXRoles.Admin) && !string.IsNullOrEmpty(appearance.Creator?.Id))
                            {
                                var ownerId = appearance.Creator.Id;
                                if (await connection.SingleOrDefaultAsync<GXUser>(transaction, GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Id == ownerId && u.Removed == null), cancellationToken) == null)
                                {
                                    throw new ArgumentException("The image owner is unavailable.");
                                }
                                appearance.Creator = new GXUser { Id = ownerId };
                            }
                            else
                            {
                                appearance.Creator = creator;
                            }
                            appearance.CreationTime = now;
                            appearance.ConcurrencyStamp = Guid.NewGuid().ToString();
                            var insert = GXInsertArgs.Insert(appearance);
                            insert.Exclude<GXAppearance>(a => new { a.Updated, a.Removed });
                            connection.Insert(transaction, insert);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(old.ConcurrencyStamp) && old.ConcurrencyStamp != appearance.ConcurrencyStamp)
                            {
                                throw new ArgumentException(Properties.Resources.ContentEdited);
                            }
                            appearance.Updated = now;
                            appearance.ConcurrencyStamp = Guid.NewGuid().ToString();
                            var update = GXUpdateArgs.Update(appearance, columns);
                            update.Exclude<GXAppearance>(a => a.CreationTime!);
                            if (!User.IsInRole(GXRoles.Admin) || appearance.Creator == null || string.IsNullOrEmpty(appearance.Creator.Id))
                            {
                                update.Exclude<GXAppearance>(a => a.Creator!);
                            }
                            connection.Update(transaction, update);
                        }
                        await GXResourcePolicyStorage.SaveAsync(connection, typeof(GXAppearance), appearance.Id,
                            appearance.Policies, cancellationToken, transaction);
                        transaction.Commit();
                    }, cancellationToken);
                    await Update(appearance.ResourceType.Value, cancellationToken);
                }
                _notificationCache.Add(await GetNotifiedUsers(Guid.Empty, NotificationAction.Edit), new GXTarget(GXAppearancePolicies.Edit));
                return [.. appearances.Select(s => s.Id)];

            }
            catch (GXDatabaseException ex)
            {
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await UpsertAsync(appearances, columns, linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                    {
                        throw ex;
                    }
                }
                else
                {
                    throw;
                }
            }
        }, cancellationToken);
        }

        /// <summary>
        /// Set the new update time.
        /// </summary>
        private Task Update(byte type, CancellationToken cancellationToken) => new GXResourceVersions(_dbFactory).TouchAppearanceAsync(type, cancellationToken);

        /// <inheritdoc />
        public async Task<DateTimeOffset?> LastChanged(byte type, CancellationToken cancellationToken)
        {
            return await new GXResourceVersions(_dbFactory).AppearanceAsync(type, cancellationToken);
        }

        private static GXAppearance CreateTheme(string name,
          string description, string path, bool active = false)
        {
            string value = JsonSerializer.Serialize(new GXThemeInfo()
            {
                Name = name,
                CssFile = path,
            });
            return new GXAppearance((byte)ResourceType.Theme, name, description, path)
            {
                Enabled = active,
                Preload = active,
                Value = value
            };
        }

        private static GXAppearance CreateIconpack(string name,
           string description, string path, bool active = false)
        {
            return new GXAppearance((byte)ResourceType.Iconpack, name, description, path)
            {
                Enabled = active,
                Preload = active,
                Value = path
            };
        }

        private static List<GXAppearance> GetIconpacks()
        {
            return [
            CreateIconpack("Bootstrap", "Bootstrap","lib/bootstrap-icons/bootstrap-icons.min.css", true),
            CreateIconpack("FontAwesome", "FontAwesome", "https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.2/css/all.min.css"),
            CreateIconpack("MaterialIcons", "Material Icons", "https://fonts.googleapis.com/icon?family=Material+Icons"),
            ];
        }

        private static List<GXAppearance> GetThemes()
        {
            return [
            CreateTheme("Default", "", "", true),
        CreateTheme("Cerulean", "","https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/cerulean/bootstrap.min.css" ),
        CreateTheme("Cosmo",   "", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/cosmo/bootstrap.min.css" ),
        CreateTheme("Cyborg", "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/cyborg/bootstrap.min.css" ),
        CreateTheme("Darkly", "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/darkly/bootstrap.min.css" ),
        CreateTheme("Flatly", "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/flatly/bootstrap.min.css" ) ,
        CreateTheme("Journal","",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/journal/bootstrap.min.css" ) ,
        CreateTheme("Litera",  "", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/litera/bootstrap.min.css" ) ,
        CreateTheme("Lumen",  "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/lumen/bootstrap.min.css" ) ,
        CreateTheme("Lux",   "",   "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/lux/bootstrap.min.css" ) ,
        CreateTheme("Materia", "", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/materia/bootstrap.min.css" ),
        CreateTheme("Minty", "",   "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/minty/bootstrap.min.css" ),
        CreateTheme("Pulse",  "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/pulse/bootstrap.min.css" ) ,
        CreateTheme("Sandstone","", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/sandstone/bootstrap.min.css" ) ,
        CreateTheme("Simplex", "", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/simplex/bootstrap.min.css" ) ,
        CreateTheme("Sketchy", "", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/sketchy/bootstrap.min.css" ) ,
        CreateTheme("Slate",  "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/slate/bootstrap.min.css" ) ,
        CreateTheme("Solar",  "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/solar/bootstrap.min.css" ) ,
        CreateTheme("Spacelab","", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/spacelab/bootstrap.min.css" ) ,
        CreateTheme("Superhero", "","https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/superhero/bootstrap.min.css" ) ,
        CreateTheme("United",  "", "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/united/bootstrap.min.css" ) ,
        CreateTheme("Yeti",   "",  "https://cdn.jsdelivr.net/npm/bootswatch@5.3.3/dist/yeti/bootstrap.min.css" ) ];
        }

        private static GXAppearance AddImage(string name, string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                value = null;
            }
            return new GXAppearance()
            {
                Name = ClientHelpers.GetKebabCase(name),
                ResourceType = (byte)ResourceType.Image,
                Value = value,
            };
        }

        private static List<GXAppearance> GetDefaults()
        {
            var defaults = GetThemes();
            defaults.AddRange(GetIconpacks());
            defaults.Add(AddImage("reader", "bi-download"));
            defaults.Add(AddImage("auto-connect", "bi-diagram-3"));
            defaults.Add(AddImage("object-view", "bi-layout-text-window"));
            defaults.Add(AddImage("generic", "bi-box"));
            defaults.Add(AddImage("write", "bi-upload"));
            defaults.Add(AddImage("unknown", "bi-question-circle"));
            defaults.Add(AddImage("search", "bi-search"));
            defaults.Add(AddImage("save", "bi-floppy"));
            defaults.Add(AddImage("ok", "bi-check-lg"));
            defaults.Add(AddImage("text", "bi-body-text"));
            defaults.Add(AddImage("close", "bi-x-lg"));
            defaults.Add(AddImage("cancel", "bi-x-lg"));
            defaults.Add(AddImage("add", "bi-plus-lg"));
            defaults.Add(AddImage("remove", "bi-dash-lg"));
            defaults.Add(AddImage("edit", "bi-pencil-square"));
            defaults.Add(AddImage("update", "bi-arrow-repeat"));
            defaults.Add(AddImage("refresh", "bi-arrow-clockwise"));
            defaults.Add(AddImage("clone", "bi-copy"));
            defaults.Add(AddImage("clear", "bi-trash"));
            defaults.Add(AddImage("home", "bi-house"));
            defaults.Add(AddImage("favorite", "bi-star"));
            defaults.Add(AddImage("configuration", "bi-tools"));
            defaults.Add(AddImage("tabs", "bi-window-stack"));
            defaults.Add(AddImage("history", "bi-clock-history"));
            defaults.Add(AddImage("change-history", "bi-journal-text"));
            defaults.Add(AddImage("log", "bi-journal-text"));
            defaults.Add(AddImage("logs", "bi-journal-text"));
            defaults.Add(AddImage("user", "bi-person"));
            defaults.Add(AddImage("userGroup", "bi-people"));
            defaults.Add(AddImage("device-template", "bi-file-earmark-text"));
            defaults.Add(AddImage("device-templates", "bi-file-earmark-text"));
            defaults.Add(AddImage("device-template-groups", "&#128196;"));
            defaults.Add(AddImage("help", "bi-question-circle"));
            defaults.Add(AddImage("gateway", "bi-router"));
            defaults.Add(AddImage("system-log", "bi-journal-text"));
            defaults.Add(AddImage("agent", "bi-cpu"));
            defaults.Add(AddImage("connections", "bi-diagram-3"));
            defaults.Add(AddImage("favorite", "bi-star"));
            defaults.Add(AddImage("task", "bi-list-task"));
            defaults.Add(AddImage("tasks", "bi-list-task"));
            defaults.Add(AddImage("device", "bi-plug"));
            defaults.Add(AddImage("performance", "bi-graph-up"));
            defaults.Add(AddImage("report-groups", "bi-collection"));
            defaults.Add(AddImage("schedule", "bi-calendar-event"));
            defaults.Add(AddImage("schedules", "bi-calendar-event"));
            defaults.Add(AddImage("roles", "bi-person-badge"));
            defaults.Add(AddImage("menus", "bi-list"));
            defaults.Add(AddImage("prune", "bi-trash"));
            defaults.Add(AddImage("system", "bi-gear"));
            defaults.Add(AddImage("subtotal", "bi-calculator"));
            defaults.Add(AddImage("statistic", "bi-bar-chart"));
            defaults.Add(AddImage("ip-access-control", "bi-shield-lock"));
            defaults.Add(AddImage("appearance", "bi-palette"));
            defaults.Add(AddImage("run", "bi-play-fill"));
            defaults.Add(AddImage("icon", "bi-image"));
            defaults.Add(AddImage("blocks", "bi-grid"));
            defaults.Add(AddImage("account", "bi-person-circle"));
            defaults.Add(AddImage("personal-access-token", "bi-key-fill"));

            defaults.Add(AddImage("module-groups", "bi-collection"));
            defaults.Add(AddImage("modules-manage", "bi-puzzle"));
            defaults.Add(AddImage("module-log", "bi-journal-text"));
            defaults.Add(AddImage("module-logs", "bi-journal-text"));

            defaults.Add(AddImage("theme", "bi-palette"));
            defaults.Add(AddImage("images", "bi-images"));
            defaults.Add(AddImage("icon-pack", "bi-image"));
            defaults.Add(AddImage("text", "bi-body-text"));
            defaults.Add(AddImage("color", "bi-palette"));

            defaults.Add(AddImage("key-management", "bi-key"));
            defaults.Add(AddImage("key-managements", "bi-key"));
            defaults.Add(AddImage("key-management-groups", "bi-key"));
            defaults.Add(AddImage("script", "bi-code-slash"));
            defaults.Add(AddImage("scripts", "bi-code-slash"));
            defaults.Add(AddImage("script-log", "bi-journal-text"));
            defaults.Add(AddImage("proxy", "bi-hdd-network"));
            defaults.Add(AddImage("http-proxy", "bi-globe2"));
            defaults.Add(AddImage("data-exchange", "bi-arrow-left-right"));
            defaults.Add(AddImage("cron", "bi-clock-history"));
            defaults.Add(AddImage("notification", "bi-bell"));
            defaults.Add(AddImage("authentication", "bi-shield-check"));
            defaults.Add(AddImage("maintenance", "bi-tools"));
            defaults.Add(AddImage("report", "bi-file-earmark-bar-graph"));
            defaults.Add(AddImage("manufacturer", "bi-building"));
            defaults.Add(AddImage("modules", "bi-puzzle"));
            defaults.Add(AddImage("contents", "bi-files"));
            defaults.Add(AddImage("triggers", "bi-lightning"));
            defaults.Add(AddImage("language", "bi-translate"));
            defaults.Add(AddImage("content-types", "bi-files"));
            defaults.Add(AddImage("workflows", "bi-diagram-3"));
            defaults.Add(AddImage("components", "bi-grid-3x3-gap"));
            defaults.Add(AddImage("agents", "bi-cpu"));
            defaults.Add(AddImage("localized-resource", "bi-translate"));

            defaults.Add(AddImage("schedule-log", "bi-journal-text"));
            defaults.Add(AddImage("schedule-logs", "bi-journal-text"));
            defaults.Add(AddImage("schedule-groups", "bi-collection"));
            defaults.Add(AddImage("read", "bi-download"));
            defaults.Add(AddImage("device-traces", "bi-activity"));
            defaults.Add(AddImage("device-actions", "bi-lightning"));
            defaults.Add(AddImage("device-logs", "bi-journal-text"));
            defaults.Add(AddImage("devices", "bi-plug"));
            defaults.Add(AddImage("device-groups", "bi-collection"));
            defaults.Add(AddImage("status", "bi-info-circle"));
            defaults.Add(AddImage("security", "bi-shield-lock"));

            defaults.Add(AddImage("agent-log", "bi-journal-text"));
            defaults.Add(AddImage("user-actions", "bi-person-lines-fill"));
            defaults.Add(AddImage("agent-groups", "bi-collection"));
            defaults.Add(AddImage("user-groups", "bi-people"));
            defaults.Add(AddImage("user-errors", "bi-exclamation-triangle"));
            defaults.Add(AddImage("users", "bi-people"));

            defaults.Add(AddImage("schedule-group", "bi-collection"));
            defaults.Add(AddImage("device-trace", "bi-activity"));
            defaults.Add(AddImage("device-action", "bi-lightning"));
            defaults.Add(AddImage("device-log", "bi-journal-text"));
            defaults.Add(AddImage("device-group", "bi-collection"));
            defaults.Add(AddImage("module", "bi-puzzle"));
            defaults.Add(AddImage("device-template-group", "bi-file-earmark-text"));
            defaults.Add(AddImage("user-error", "bi-exclamation-triangle"));
            defaults.Add(AddImage("user-action", "bi-person-lines-fill"));
            defaults.Add(AddImage("agent-group", "bi-collection"));
            defaults.Add(AddImage("gateway-group", "bi-collection"));
            defaults.Add(AddImage("gateway-log", "bi-journal-text"));
            defaults.Add(AddImage("data-exchange", "bi-arrow-left-right"));
            defaults.Add(AddImage("workflow-log", "bi-journal-text"));
            defaults.Add(AddImage("advanced", "bi-sliders"));
            defaults.Add(AddImage("device-template-groups-selector", "bi-file-earmark-text"));
            defaults.Add(AddImage("profiles", "bi-list-columns"));
            defaults.Add(AddImage("object-templates", "bi-file-earmark-text"));
            defaults.Add(AddImage("ciphering", "bi-key"));
            defaults.Add(AddImage("objects", "bi-box"));
            defaults.Add(AddImage("supported-languages", "bi-translate"));
            defaults.Add(AddImage("component-views", "bi-layout-text-window"));
            defaults.Add(AddImage("component", "bi-grid-3x3-gap"));
            defaults.Add(AddImage("menu-groups", "bi-list"));
            defaults.Add(AddImage("regenerate", "bi-arrow-repeat"));
            defaults.Add(AddImage("menu", "bi-list"));
            defaults.Add(AddImage("block-groups", "bi-collection"));
            defaults.Add(AddImage("script-groups", "bi-collection"));
            defaults.Add(AddImage("script-logs", "bi-journal-text"));
            defaults.Add(AddImage("workflow-groups", "bi-diagram-3"));
            defaults.Add(AddImage("workflow-logs", "bi-journal-text"));
            defaults.Add(AddImage("key-management-logs", "bi-journal-text"));
            defaults.Add(AddImage("manufacturers", "bi-building"));
            defaults.Add(AddImage("install", "bi-download"));
            defaults.Add(AddImage("subtotals", "bi-calculator"));
            defaults.Add(AddImage("subtotal-groups", "bi-collection"));
            defaults.Add(AddImage("subtotal-logs", "bi-journal-text"));
            defaults.Add(AddImage("gateway-groups", "bi-collection"));
            defaults.Add(AddImage("gateways", "bi-hdd-network"));
            defaults.Add(AddImage("rebuild", "bi-arrow-repeat"));
            defaults.Add(AddImage("calculate", "bi-calculator"));
            defaults.Add(AddImage("reports", "bi-file-earmark-bar-graph"));
            defaults.Add(AddImage("report-logs", "bi-journal-text"));
            defaults.Add(AddImage("export", "bi-box-arrow-up"));
            defaults.Add(AddImage("import", "bi-box-arrow-in-down"));
            defaults.Add(AddImage("ip-access-control", "bi-shield-lock"));
            defaults.Add(AddImage("workflow-group", "bi-diagram-3"));
            defaults.Add(AddImage("component-view", "bi-layout-text-window"));
            defaults.Add(AddImage("view", "bi-layout-text-window"));
            defaults.Add(AddImage("script-view", "bi-code-square"));
            defaults.Add(AddImage("modules-updates", "bi-cloud-download"));
            defaults.Add(AddImage("updates", "bi-cloud-download"));
            defaults.Add(AddImage("block", "bi-grid"));
            defaults.Add(AddImage("login", "bi-box-arrow-in-right"));
            defaults.Add(AddImage("actions", "bi-lightning"));
            defaults.Add(AddImage("access-control", "bi-shield-lock"));
            defaults.Add(AddImage("key-management-log", "bi-journal-text"));
            defaults.Add(AddImage("notifications", "bi-bell"));
            defaults.Add(AddImage("web-push", "bi-bell"));
            defaults.Add(AddImage("show", "bi-eye"));
            defaults.Add(AddImage("script-methods", "bi-code-slash"));
            defaults.Add(AddImage("logout", "bi-box-arrow-right"));
            defaults.Add(AddImage("reload", "bi-arrow-clockwise"));
            defaults.Add(AddImage("component-view-group", "bi-collection"));
            defaults.Add(AddImage("component-view-groups", "bi-collection"));
            defaults.Add(AddImage("key-management-group", "bi-collection"));
            defaults.Add(AddImage("menu-group", "bi-collection"));
            defaults.Add(AddImage("module-group", "bi-collection"));
            defaults.Add(AddImage("script-group", "bi-collection"));
            defaults.Add(AddImage("trigger-group", "bi-collection"));
            defaults.Add(AddImage("view-group", "bi-collection"));
            defaults.Add(AddImage("view-groups", "bi-collection"));
            defaults.Add(AddImage("settings", "bi-sliders"));
            // Keep identifiers referenced by existing menus and views.
            defaults.Add(AddImage("key-management", "bi-key"));
            defaults.Add(AddImage("trigger", "bi-lightning"));
            defaults.Add(AddImage("views", "bi-layout-text-window"));
            defaults.Add(AddImage("workflow", "bi-diagram-3"));
            defaults.Add(AddImage("change-password", "bi-key"));

            //Add flags for countries.
            defaults.Add(AddImage("fi", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"fi\" viewBox=\"0 0 24 16\">\r\n  <rect width=\"24\" height=\"16\" fill=\"white\"/>\r\n  <rect x=\"6\" width=\"4\" height=\"16\" fill=\"#003580\"/>\r\n  <rect y=\"6\" width=\"24\" height=\"4\" fill=\"#003580\"/>\r\n</svg>"));
            defaults.Add(AddImage("fr", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"fr\" viewBox=\"0 0 24 16\">\r\n  <rect width=\"8\" height=\"16\" fill=\"#0055A4\"/>\r\n  <rect x=\"8\" width=\"8\" height=\"16\" fill=\"white\"/>\r\n  <rect x=\"16\" width=\"8\" height=\"16\" fill=\"#EF4135\"/>\r\n</svg>"));
            defaults.Add(AddImage("de", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"de\" viewBox=\"0 0 24 16\">\r\n  <rect width=\"24\" height=\"16\" fill=\"black\"/>\r\n  <rect y=\"5.33\" width=\"24\" height=\"5.33\" fill=\"red\"/>\r\n  <rect y=\"10.67\" width=\"24\" height=\"5.33\" fill=\"gold\"/>\r\n</svg>"));
            defaults.Add(AddImage("it", "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 16\">\r\n    <rect width=\"8\" height=\"16\" fill=\"#008C45\"/>\r\n    <rect x=\"8\" width=\"8\" height=\"16\" fill=\"white\"/>\r\n    <rect x=\"16\" width=\"8\" height=\"16\" fill=\"#CD212A\"/>\r\n</svg>\r\n"));
            defaults.Add(AddImage("sv", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"sv\" viewBox=\"0 0 24 16\">\r\n  <rect width=\"24\" height=\"16\" fill=\"#0057B7\"/>\r\n  <rect x=\"6\" width=\"4\" height=\"16\" fill=\"#FFCC00\"/>\r\n  <rect y=\"6\" width=\"24\" height=\"4\" fill=\"#FFCC00\"/>\r\n</svg>"));
            defaults.Add(AddImage("en", "<svg xmlns=\"http://www.w3.org/2000/svg\"\r\n     width=\"60\" height=\"30\" viewBox=\"0 0 60 30\">\r\n    <clipPath id=\"clip\">\r\n        <path d=\"M0,0 v30 h60 v-30 z\"/>\r\n    </clipPath>\r\n    <g clip-path=\"url(#clip)\">\r\n        <rect width=\"60\" height=\"30\" fill=\"#012169\"/>\r\n        <path d=\"M0,0 L60,30 M60,0 L0,30\" stroke=\"#fff\" stroke-width=\"6\"/>\r\n        <path d=\"M0,0 L60,30 M60,0 L0,30\" stroke=\"#C8102E\" stroke-width=\"4\"/>\r\n        <path d=\"M30,0 V30 M0,15 H60\" stroke=\"#fff\" stroke-width=\"10\"/>\r\n        <path d=\"M30,0 V30 M0,15 H60\" stroke=\"#C8102E\" stroke-width=\"6\"/>\r\n    </g>\r\n</svg>\r\n"));
            defaults.Add(AddImage("et", "<svg xmlns=\"http://www.w3.org/2000/svg\" id=\"ee\" viewBox=\"0 0 24 16\">\r\n    <rect width=\"24\" height=\"16\" fill=\"white\"/>\r\n    <rect width=\"24\" height=\"5.33\" fill=\"#0072CE\"/>\r\n    <rect y=\"5.33\" width=\"24\" height=\"5.33\" fill=\"black\"/>\r\n</svg>"));
            // Keep the first definition, matching the original initialization order.
            var result = defaults.DistinctBy(a => a.Name, StringComparer.Ordinal).ToList();
            foreach (var appearance in result)
            {
                appearance.AccessVisibility = ResourceVisibility.Public;
                appearance.Enabled = appearance.ResourceType == (byte)ResourceType.Image || appearance.Enabled == true;
                appearance.Preload ??= false;
            }
            return result;
        }

        /// <summary>Create only missing built-in appearances.</summary>
        public async Task EnsureDefaultsAsync(int? type, CancellationToken cancellationToken = default)
        {
            await GXHistoryMutation.RunAllAsync<GXAppearance>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices,
        async () =>
        {
            await ApplyDefaultsAsync(type, null, cancellationToken);
        }, cancellationToken);
        }

        /// <summary>Restore selected built-in appearances in place.</summary>
        public async Task ResetToDefaultsAsync(int? type, IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            ids = ids.ToArray();
            await GXHistoryMutation.RunIdsAsync<GXAppearance, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, ids,
        async () =>
        {
            ArgumentNullException.ThrowIfNull(ids);
            var selected = ids.Distinct().ToArray();
            if (selected.Length == 0 || selected.Contains(Guid.Empty))
            {
                throw new ArgumentException("Existing appearance identifiers are required.", nameof(ids));
            }
            await ApplyDefaultsAsync(type, selected, cancellationToken);
        }, cancellationToken);
        }

        private async Task ApplyDefaultsAsync(int? type, Guid[]? selectedIds, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXAppearancePolicies.Regenerate);
            var ids = selectedIds ?? [];
            var defaults = GetDefaults();
            var byName = defaults.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);
            var changedTypes = new HashSet<byte>();
            var replacedImages = new List<string>();
            await using (var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken))
            {
                if (connection.Connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.Connection.OpenAsync(cancellationToken);
                }
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                try
                {
                    var args = GXSelectArgs.SelectAll<GXAppearance>();
                    args.Joins.AddLeftJoin<GXAppearance, GXUser>(a => a.Creator!, u => u.Id!);
                    args.Columns.Add<GXUser>(u => u.Id!);
                    var rows = await connection.SelectAsync<GXAppearance>(transaction, args, cancellationToken);
                    var byId = rows.ToDictionary(a => a.Id);
                    var existing = rows.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);
                    var selectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var id in ids)
                    {
                        if (!byId.TryGetValue(id, out var selected) || !byName.ContainsKey(selected.Name) ||
                            (type.HasValue && byName[selected.Name].ResourceType != type.Value))
                        {
                            throw new ArgumentException($"Appearance '{id}' has no matching built-in default.", nameof(selectedIds));
                        }
                        selectedNames.Add(selected.Name);
                        if (GXAppearanceAccess.IsOwnerOnly(selected))
                        {
                            GXAppearanceAccess.RequirePrivateOwner(User, selected);
                        }
                    }
                    defaults = [.. defaults.Where(a => (!type.HasValue || a.ResourceType == type.Value)
                        && (ids.Length == 0 || selectedNames.Contains(a.Name)))];
                    var now = DateTimeOffset.Now;
                    var creator = new GXUser { Id = ClientHelpers.GetUserId(User) };
                    foreach (var appearance in defaults)
                    {
                        if (existing.TryGetValue(appearance.Name, out var previous))
                        {
                            if (selectedIds == null)
                            {
                                continue;
                            }
                            appearance.Id = previous.Id;
                            appearance.AccessVisibility = previous.AccessVisibility;
                            if (previous.ResourceType == (byte)ResourceType.Image && appearance.ResourceType != (byte)ResourceType.Image)
                            {
                                await GXProfilePictures.EnsureNotReferencedAsync(connection, previous.Id, cancellationToken, transaction);
                            }
                            appearance.Updated = now;
                            appearance.ConcurrencyStamp = Guid.NewGuid().ToString();
                            var update = GXUpdateArgs.Update(appearance);
                            update.Exclude<GXAppearance>(a => new { a.Creator, a.CreationTime });
                            await connection.UpdateAsync(transaction, update, cancellationToken);
                            if (previous.StorageKey != null)
                            {
                                replacedImages.Add(previous.StorageKey);
                            }
                            await connection.DeleteAsync(transaction,
                                GXDeleteArgs.Delete<GXAppearancePolicy>(p => p.AppearanceId == appearance.Id), cancellationToken);
                            if (previous.ResourceType is byte previousType)
                            {
                                changedTypes.Add(previousType);
                            }
                        }
                        else
                        {
                            appearance.Id = Guid.NewGuid();
                            appearance.Creator = creator;
                            appearance.CreationTime = now;
                            appearance.ConcurrencyStamp = Guid.NewGuid().ToString();
                            await connection.InsertAsync(transaction, GXInsertArgs.Insert(appearance), cancellationToken);
                            existing.Add(appearance.Name, appearance);
                        }
                        if (appearance.ResourceType is byte resourceType)
                        {
                            changedTypes.Add(resourceType);
                        }
                    }
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
            }
            foreach (var key in replacedImages)
            {
                _imageStorage.Delete(key);
            }
            foreach (var it in changedTypes)
            {
                await Update(it, cancellationToken);
            }
            if (changedTypes.Count != 0)
            {
                _notificationCache.Add(await GetNotifiedUsers(Guid.Empty, NotificationAction.Edit),
                    new GXTarget(GXAppearancePolicies.Edit));
            }
        }
    }
}
