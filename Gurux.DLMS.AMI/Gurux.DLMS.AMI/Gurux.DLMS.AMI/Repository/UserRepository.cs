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

using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using System.Data;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public partial class UserRepository : IUserRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IAuthorizationService _authorizationService;
        private readonly NotificationCache _notificationCache;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserRepository(IDbConnectionFactory dbFactory,
            IServiceProvider serviceProvider,
            IAuthorizationService authorizationService,
            IHttpContextAccessor httpContextAccessor,
            IUserGroupRepository userGroupRepository,
            UserManager<ApplicationUser> userManager,
            NotificationCache notificationCache)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            else
            {
                User = httpContextAccessor.HttpContext.User;
            }
            _dbFactory = dbFactory;
            _userManager = userManager;
            _userGroupRepository = userGroupRepository;
            _authorizationService = authorizationService;
            _notificationCache = notificationCache;
            _serviceProvider = serviceProvider;
        }


        private Task<List<string>> GetNotifiedUsers(object id, NotificationAction action,
            GXDbConnection? connection = null, System.Data.IDbTransaction? transaction = null)
            => GXRepositoryUsers.GetNotifiedUsersAsync(_dbFactory, typeof(GXUser), id, action, GXUserPolicies.View, connection, transaction, notifications: _notificationCache);

        /// <inheritdoc />
        public async Task<List<string>> GetUserIdsInRoleAsync(IEnumerable<string> roles)
        {
            ArgumentNullException.ThrowIfNull(roles);
            //Check that user can access this role.
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            var list = roles.Where(role => !string.IsNullOrWhiteSpace(role) &&
                    (isAdmin || User.Identities.Any(identity => identity.FindAll(identity.RoleClaimType)
                        .Any(claim => string.Equals(claim.Value, role, StringComparison.OrdinalIgnoreCase)))))
                .Select(role => role.ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
            if (list.Length == 0)
            {
                //If user is not any of the roles, return empty list.
                return [];
            }
            GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id!, where => where.Removed == null);
            args.Joins.AddInnerJoin<GXUser, GXUserRole>(j => j.Id!, j => j.UserId!);
            args.Joins.AddInnerJoin<GXUserRole, GXRole>(j => j.RoleId!, j => j.Id);
            args.Where.And<GXRole>(where => where.Removed == null && list.Contains(where.NormalizedName!));
            args.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            return [.. (await connection.SelectAsync<GXUser>(args)).Select(s => s.Id!)];
        }

        /// <summary>
        /// Return user roles.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="userId">User ID.</param>
        /// <returns>Available user roles.</returns>
        private static async Task<List<string>> GetUserRoleNamesAsync(GXDbConnection connection, string userId)
        {
            var arg = GXSelectArgs.Select<GXRole>(s => s.Name!, r => r.Removed == null);
            arg.Joins.AddInnerJoin<GXRole, GXUserRole>(j => j.Id, j => j.RoleId!);
            arg.Where.And<GXUserRole>(w => w.UserId == userId);
            return await connection.SelectAsync<string>(arg);
        }
        /// <inheritdoc />
        public async Task DeleteAsync(
            IEnumerable<string> users,
            bool delete = true, bool notify = true, CancellationToken cancellationToken = default)
        {
            users = users.ToArray();
            await GXHistoryMutation.RunDeleteAsync<GXUser, string>(_dbFactory, User, _serviceProvider, users,
        async () =>
        {
            var userIds = users.Distinct(StringComparer.Ordinal).ToArray();
            userIds.ValidateIds();
            Dictionary<GXUser, List<string>> updates;
            List<string> imageKeys;
            for (int attempt = 0; ; ++attempt)
            {
                try
                {
                    (updates, imageKeys) = await DeleteUsersAttemptAsync(userIds, delete, notify, cancellationToken);
                    break;
                }
                catch (GXDatabaseException error) when (attempt < 2 && error.ErrorType == Gurux.Service.Orm.Enums.DatabaseErrorType.Deadlock)
                {
                    // The attempt has rolled back and disposed its connection.
                    // Retry authorization, ownership reads and cleanup together.
                    await Task.Delay(TimeSpan.FromMilliseconds(25 * (attempt + 1)), cancellationToken);
                }
            }
            if (imageKeys.Count != 0)
            {
                var imageStorage = _serviceProvider.GetRequiredService<Gurux.DLMS.AMI.Server.Services.GXImageStorage>();
                foreach (var key in imageKeys)
                {
                    imageStorage.Delete(key);
                }
            }
            foreach (var update in updates)
            {
                _notificationCache.Add(update.Value,
                    new GXTarget(GXUserPolicies.Delete, update.Key.Id, update.Key.UserName));
            }
        }, cancellationToken);
        }

        private async Task<(Dictionary<GXUser, List<string>> Updates, List<string> ImageKeys)> DeleteUsersAttemptAsync(
            string[] userIds, bool delete, bool notify, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXUserPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            var updates = new Dictionary<GXUser, List<string>>();
            var imageKeys = new List<string>();
            try
            {
                GXSelectArgs query;
                if (User.IsInRole(GXRoles.Admin))
                {
                    query = GXSelectArgs.Select<GXUser>(u => new { u.Id, u.UserName, u.Email }, u => userIds.Contains(u.Id!));
                }
                else
                {
                    query = GXQuery.GetUsersByUser(u => new { u.Id, u.UserName, u.Email },
                        ServerHelpers.GetUserId(User), DateTimeOffset.MinValue, null, userIds);
                }
                var exists = await connection.SelectAsync<GXUser>(transaction, query, cancellationToken);
                if (exists.Count != userIds.Length)
                {
                    throw new GXAmiForbiddenException(string.Join(", ", userIds));
                }
                if (exists.Any(u => u.Id == ServerHelpers.GetUserId(User)))
                {
                    throw new ArgumentException(Properties.Resources.YouCanRemoveYourself);
                }
                foreach (var user in exists)
                {
                    if (notify)
                    {
                        updates[user] = await GetNotifiedUsers(user.Id!, NotificationAction.Remove, connection, transaction);
                    }
                }
                if (delete)
                {
                    await LockUsersForDeletionAsync(connection, transaction, userIds, cancellationToken);
                    var deletedUsers = GXSelectArgs.Select<GXUser>(u => u.Id!, u => userIds.Contains(u.Id!));
                    var images = await connection.SelectAsync<GXAppearance>(transaction,
                        GXSelectArgs.Select<GXAppearance>(a => a.StorageKey!,
                            a => GXSql.Exists<GXAppearance, GXUser>(a => a.Creator!, u => u.Id!, deletedUsers)), cancellationToken);
                    imageKeys.AddRange(images.Select(a => a.StorageKey).OfType<string>().Where(k => k.Length != 0).Distinct(StringComparer.Ordinal));
                    await DeleteNonCascadingUserDataAsync(connection, transaction, userIds, cancellationToken);
                    await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteRange(exists), cancellationToken);
                }
                else
                {
                    var removed = DateTimeOffset.UtcNow;
                    foreach (var user in exists)
                    {
                        user.Removed = removed;
                        await connection.UpdateAsync(transaction, GXUpdateArgs.Update(user, u => u.Removed!), cancellationToken);
                    }
                }
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                if (transaction.Connection != null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                throw;
            }
            return (updates, imageKeys);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXUser>> ListAsync(
            ListUsers? request,
            ListUsersResponse? response,
            CancellationToken cancellationToken = default)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXUserPolicies.View);
            string userId = ServerHelpers.GetUserId(User);
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            GXSelectArgs arg;
            bool allUsers = request != null && request.AllUsers && User.IsInRole(GXRoles.Admin);
            if (allUsers)
            {
                //Admin can see all the users.
                arg = GXSelectArgs.SelectAll<GXUser>();
                //Get user groups where user belongs.
                arg.Joins.AddLeftJoin<GXUser, GXUserGroupUser>(a => a.Id!, b => b.UserId!);
                arg.Joins.AddLeftJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
                arg.Where.And<GXUserGroup>(q => q.Removed == null);
            }
            else
            {
                arg = GXQuery.GetUsersByUser(s => "*", userId,
                    request?.Filter?.Removed, request?.Exclude, request?.Included);
            }
            arg.Columns.Exclude<GXUser>(e => new
            {
                e.NormalizedEmail,
                e.NormalizedUserName,
                e.PhoneNumber,
                e.PhoneNumberConfirmed,
                e.TwoFactorEnabled,
                e.LockoutEnabled,
                e.ConcurrencyStamp,
                e.AccessFailedCount
            });

            arg.Distinct = true;
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXUser>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXUser>(q => q.CreationTime);
            }
            arg.Columns.Exclude<GXUser>(q => new { q.PasswordHash, q.SecurityStamp });
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXUser>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXUser>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && request.Filter != null && request.Filter.Roles != null && request.Filter.Roles.Count != 0)
            {
                arg.Where.And<GXRole>(q => request.Filter.Roles.Contains(q.Name!));
            }
            GXDateTimeFilters.Apply<GXUser>(arg, request?.DateFilters);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXUser>(q => GXSql.Count(1));
                total.Distinct = true;
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
            GXUser[] users = [.. (await connection.SelectAsync<GXUser>(arg))];
            //Roles must retrieve separetly because only role and scope names are shown for security reasons.
            foreach (GXUser it in users)
            {
                it.Roles = await GetUserRoleNamesAsync(connection, it.Id!);
                it.Permissions = await UserPermissions.ReadDirectAsync(connection, it.Id!);
            }
            if (response != null)
            {
                response.Users = users;
                if (response.Count == 0)
                {
                    response.Count = users.Length;
                }
            }
            return users;
        }

        /// <inheritdoc />
        public async Task<GXUser> ReadAsync(string? id, Expression<Func<GXUser, object>>? columns = null, CancellationToken cancellationToken = default)
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXUserPolicies.View);
                bool isAdmin = User.IsInRole(GXRoles.Admin);
                if (string.IsNullOrEmpty(id))
                {
                    //Returns information for the current user.
                    id = ServerHelpers.GetUserId(User);
                }
                GXSelectArgs arg = GXSelectArgs.SelectAll<GXUser>(q => q.Id == id);
                arg.Joins.AddLeftJoin<GXUser, GXAppearance>(u => u.ProfilePicture!, a => a.Id);
                arg.Columns.Add<GXAppearance>(a => a.Id);
                arg.Distinct = true;
                if (!isAdmin)
                {
                    // Match ListAsync: non-administrators can read only their own identity.
                    // Shared user groups grant resource access, not access to other profiles.
                    string callerId = ServerHelpers.GetUserId(User);
                    arg.Where.And<GXUser>(q => q.Id == callerId);
                    arg.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id!, b => b.UserId!);
                    arg.Joins.AddInnerJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
                    arg.Where.And<GXUserGroup>(q => q.Removed == null);
                    var memberships = GXSelectArgs.Select<GXUserGroupUser>(q => GXSql.One,
                        q => q.UserId == callerId && q.Removed == null);
                    arg.Where.And<GXUserGroupUser>(q => q.Removed == null);
                    arg.Where.And<GXUserGroup>(q => GXSql.Exists<GXUserGroup, GXUserGroupUser>(
                        group => group.Id, membership => membership.UserGroupId, memberships));
                }
                arg.OrderBy.Add<GXUser>(q => q.CreationTime);
                arg.Columns.Exclude<GXUser>(q => new { q.PasswordHash, q.SecurityStamp });
                if (!isAdmin)
                {
                    //Only admin can see removed users.
                    arg.Where.And<GXUser>(q => q.Removed == null);
                }
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                GXUser? ret = (await connection.SingleOrDefaultAsync<GXUser>(arg)) ?? throw new GXAmiForbiddenException(id.ToString());
                // Collection joins do not hydrate GXUser.UserGroups. Read active memberships
                // explicitly, with the same shared-group restriction as the user lookup.
                var groups = GXSelectArgs.SelectAll<GXUserGroup>(g => g.Removed == null);
                groups.Distinct = true;
                groups.Columns.Exclude<GXUserGroup>(g => new { g.Users, g.Creator });
                groups.Joins.AddInnerJoin<GXUserGroup, GXUserGroupUser>(g => g.Id, membership => membership.UserGroupId);
                groups.Where.And<GXUserGroupUser>(membership => membership.UserId == id && membership.Removed == null);
                if (!isAdmin)
                {
                    string callerId = ServerHelpers.GetUserId(User);
                    var memberships = GXSelectArgs.Select<GXUserGroupUser>(m => GXSql.One,
                        m => m.UserId == callerId && m.Removed == null);
                    groups.Where.And<GXUserGroup>(g => GXSql.Exists<GXUserGroup, GXUserGroupUser>(
                        group => group.Id, membership => membership.UserGroupId, memberships));
                }
                ret.UserGroups = await connection.SelectAsync<GXUserGroup>(groups);
                //Roles must retrieve separetly because only role and scope names are shown for security reasons.
                ret.Roles = await GetUserRoleNamesAsync(connection, id);
                ret.Permissions = await UserPermissions.ReadDirectAsync(connection, id);
                //Read user settings.
                arg = GXSelectArgs.SelectAll<GXUserProperty>(q => q.User == ret && q.Module == null);
                arg.Distinct = true;
                ret.Properties = await connection.SelectAsync<GXUserProperty>(arg);
                ret.Properties ??= [];
                return ret;

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
                        return await ReadAsync(id, columns, linkedCts.Token);
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
        }

        /// <summary>
        /// Add new user.
        /// </summary>
        /// <param name="givenName">Given name.</param>
        /// <param name="surname">Surname (last name).</param>
        /// <param name="email">User email address.</param>
        /// <param name="password">User password.</param>
        /// <param name="roles">User roles.</param>
        /// <param name="permissions">Direct user permissions.</param>
        private async Task<string> AddUser(
            string? givenName,
            string? surname,
            string email,
            string? password,
            List<string>? roles,
            List<string>? permissions)
        {
            ApplicationUser? user = await _userManager.FindByNameAsync(email);
            if (user == default)
            {
                var newAppUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    GivenName = givenName,
                    Surname = surname,
                    //Update creation time so default values are not set in the middleware.
                    CreationTime = DateTime.Now,
                    EmailConfirmed = false
                };
                IdentityResult ret = await ((GXApplicationUserManager)_userManager)
                    .CreateAsync(newAppUser, password, roles, permissions);
                if (!ret.Succeeded)
                {
                    if (ret.Errors != null && ret.Errors.Any())
                    {
                        throw new ArgumentException(ret.Errors.First().Description);
                    }
                    throw new ArgumentException("Failed to create a new user.");
                }
                return newAppUser.Id;
            }
            return user.Id;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> UpsertAsync(
            IEnumerable<GXUser> users,
            Expression<Func<GXUser, object>>? columns, CancellationToken cancellationToken = default)
        {
            users = users.ToArray();
            return await GXHistoryMutation.RunAsync<GXUser, IEnumerable<string>>(_dbFactory, User, _serviceProvider, users,
        async () =>
        {
            try
            {

                await _authorizationService.EnsureAnyPolicyAsync(User, GXUserPolicies.Add, GXUserPolicies.Edit);
                string creatorId = ServerHelpers.GetUserId(User);
                bool isAdmin = User.IsInRole(GXRoles.Admin);
                DateTime now = DateTime.Now;
                List<string> list = [];
                List<GXUserGroup>? defaultGroups = null;
                //Get list from roles that creator can access.
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                foreach (GXUser user in users)
                {
                    if (string.IsNullOrEmpty(user.Id) || columns == null || ServerHelpers.Contains(columns, nameof(GXUser.ProfilePicture)))
                    {
                        await Gurux.DLMS.AMI.Server.Services.GXProfilePictures.ValidateAsync(connection, _authorizationService, User, user.ProfilePicture?.Id, cancellationToken);
                    }
                    if (string.IsNullOrEmpty(user.Email))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidEmailAddress);
                    }
                    if (user.Permissions != null)
                    {
                        await UserPermissions.ValidateAsync(connection, user.Permissions);
                    }

                    if (user.Roles != null)
                    {
                        foreach (var role in user.Roles)
                        {
                            var normalized = role.ToUpperInvariant();
                            var assignedRole = await connection.SingleOrDefaultAsync<GXRole>(GXSelectArgs.SelectAll<GXRole>(r => r.NormalizedName == normalized && r.Removed == null)) ?? throw new ArgumentException($"Role '{role}' not found.");
                        }
                    }
                    user.NormalizedEmail = user.Email.ToUpper();
                    if (string.IsNullOrEmpty(user.Id))
                    {
                        var registration = await Gurux.DLMS.AMI.Server.Services.GXAccountRegistrationPolicy.ReadAsync(connection, cancellationToken);
                        if (!registration.CanCreate(User))
                        {
                            throw new GXAmiForbiddenException(Properties.Resources.RegistrationDisabled);
                        }
                        if (user.UserGroups == null || user.UserGroups.Count == 0)
                        {
                            if (defaultGroups == null)
                            {
                                //Get default user groups.
                                ListUserGroups request = new()
                                {
                                    Filter = new GXUserGroup() { Default = true }
                                };
                                defaultGroups = [.. await _userGroupRepository.ListAsync(request, cancellationToken: cancellationToken)];
                            }
                            user.UserGroups = [.. defaultGroups];
                            if (user.UserGroups.Count == 0)
                            {
                                throw new ArgumentException(Properties.Resources.UsersMustBelongToAtLeastOneUserGroup);
                            }
                        }
                        //Check that user email is not added yet.
                        GXSelectArgs total = GXSelectArgs.Select<GXUser>(q => GXSql.Count(q));
                        var normalizedEmail = user.Email.ToUpper();
                        total.Where.And<GXUser>(q => q.NormalizedEmail == normalizedEmail);
                        if (await connection.SingleOrDefaultAsync<int>(total) != 0)
                        {
                            throw new ArgumentException(string.Format("Email address '{0}' already in use.", user.Email));
                        }
                        user.Id = await AddUser(user.GivenName, user.Surname, user.Email, user.Password, user.Roles, user.Permissions);
                        if (user.ProfilePicture != null)
                            {
                            await connection.UpdateAsync(GXUpdateArgs.Update(user, item => item.ProfilePicture!), cancellationToken);
                        }
                        list.Add(user.Id);
                        //Add default user groups.
                        List<GXUserGroupUser> tmp = [];
                        //Insert new user groups that are not in the DB.
                        foreach (var it in user.UserGroups)
                        {
                            tmp.Add(new GXUserGroupUser() { UserId = user.Id, UserGroupId = it.Id });
                        }
                        GXInsertArgs args = GXInsertArgs.InsertRange(tmp);
                        connection.Insert(args);

                        _notificationCache.Add(await GetNotifiedUsers(user.Id, NotificationAction.Add, connection),
                                               new GXTarget(GXUserPolicies.Add, user.Id, user.UserName));
                    }
                    else
                    {
                        GXSelectArgs m = GXSelectArgs.Select<GXUser>(q => q.ConcurrencyStamp!, where => where.Id == user.Id);
                        string? updated = connection.SingleOrDefault<string>(m);
                        if (!string.IsNullOrEmpty(updated) && updated != user.ConcurrencyStamp)
                        {
                            throw new ArgumentException(Properties.Resources.ContentEdited);
                        }

                        GXUser tmpUser = GetUser(connection, user.Id);
                        user.IsApproved ??= tmpUser.IsApproved;
                        if (!isAdmin && user.IsApproved != tmpUser.IsApproved &&
                            (columns == null || ServerHelpers.Contains(columns, nameof(GXUser.IsApproved))))
                        {
                            throw new GXAmiForbiddenException("Only an administrator can change account approval.");
                        }
                        if (user.Email.CompareTo(tmpUser.Email) != 0)
                        {
                            tmpUser.UserName = tmpUser.Email = user.Email;
                            tmpUser.NormalizedUserName = tmpUser.NormalizedEmail = user.Email.ToUpper();
                        }

                        if (user.UserGroups == null || user.UserGroups.Count == 0)
                        {
                            throw new ArgumentException(Properties.Resources.UsersMustBelongToAtLeastOneUserGroup);
                        }

                        var identityUser = await _userManager.FindByIdAsync(user.Id)
                            ?? throw new ArgumentException($"User '{user.Email}' not found.");
                        var currentRoles = await _userManager.GetRolesAsync(identityUser);
                        var currentPermissions = await UserPermissions.ReadDirectAsync(connection, user.Id);
                        bool rightsChanged = (user.Roles != null && !currentRoles.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(user.Roles)) ||
                            (user.Permissions != null && !currentPermissions.ToHashSet(StringComparer.Ordinal).SetEquals(user.Permissions));
                        if (user.Roles != null)
                        {
                            try
                            {
                                await _userManager.UpdateUserRolesAsync(identityUser, user.Roles);
                            }
                            finally
                            {
                                if (!GXHistoryScope.Defer(GXUserPrincipalCache.InvalidateAll))
                                {
                                    GXUserPrincipalCache.InvalidateAll();
                                }
                            }
                        }

                        if (user.Permissions != null)
                        {
                            try
                            {
                                await UserPermissions.ReplaceAsync(connection, user.Id, user.Permissions);
                            }
                            finally
                            {
                                if (!GXHistoryScope.Defer(GXUserPrincipalCache.InvalidateAll))
                                {
                                    GXUserPrincipalCache.InvalidateAll();
                                }
                            }
                        }

                        user.Updated = DateTime.Now;
                        List<GXUserGroup> userGroups = await _userGroupRepository.GetJoinedUserGroupsAsync(user.Id);
                        var comparer = new UniqueComparer<GXUserGroup, Guid>();
                        List<GXUserGroup> removedUserGroups = [.. userGroups.Except(user.UserGroups, comparer)];
                        List<GXUserGroup> addedUserGroups = [.. user.UserGroups.Except(userGroups, comparer)];
                        if (removedUserGroups.Count != 0)
                        {
                            //Remove un-checked user groups from the DB.
                            List<Guid> tmp = [];
                            foreach (var it in removedUserGroups)
                            {
                                tmp.Add(it.Id);
                            }
                            GXDeleteArgs args = GXDeleteArgs.Delete<GXUserGroupUser>(where => where.UserId == user.Id && tmp.Contains(where.UserGroupId));
                            connection.Delete(args);
                        }
                        if (addedUserGroups.Count != 0)
                        {
                            List<GXUserGroupUser> tmp = [];
                            //Insert new user groups that are not in the DB.
                            foreach (var it in addedUserGroups)
                            {
                                tmp.Add(new GXUserGroupUser() { UserId = user.Id, UserGroupId = it.Id });
                            }
                            GXInsertArgs args = GXInsertArgs.InsertRange(tmp);
                            connection.Insert(args);
                        }
                        if (rightsChanged)
                        {
                            var stampResult = await _userManager.UpdateSecurityStampAsync(identityUser);
                            if (!stampResult.Succeeded)
                            {
                                throw new InvalidOperationException(string.Join(", ", stampResult.Errors.Select(e => e.Description)));
                            }
                        }
                        user.Updated = now;
                        user.ConcurrencyStamp = Guid.NewGuid().ToString();
                        GXUpdateArgs arg2 = GXUpdateArgs.Update(user, columns);
                        arg2.Exclude<GXUser>(q => new { q.PasswordHash, q.SecurityStamp, q.CreationTime, q.UserGroups, q.Roles });
                        await connection.UpdateAsync(arg2, cancellationToken);
                        if (rightsChanged)
                        {
                            if (user.Id == creatorId)
                            {
                                await _serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>().RefreshSignInAsync(identityUser);
                            }

                            _notificationCache.Add(user.Id, new GXTarget(GXUserPolicies.Edit, user.Id, user.UserName));
                        }
                        _notificationCache.Add(await GetNotifiedUsers(user.Id, NotificationAction.Edit, connection),
                            new GXTarget(GXUserPolicies.Edit, user.Id, user.UserName));
                    }
                }
                if (!GXHistoryScope.Defer(GXUserPrincipalCache.InvalidateAll))
                {
                    GXUserPrincipalCache.InvalidateAll();
                }
                return [.. users.Select(s => s.Id!)];

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
                        return await UpsertAsync(users, columns, linkedCts.Token);
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

        private static GXUser GetUser(GXDbConnection connection, string userId)
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<GXUser>(where => where.Id == userId);
            return connection.SingleOrDefault<GXUser>(args) ?? throw new ArgumentException("User was not found.", nameof(userId));
        }
    }
}
