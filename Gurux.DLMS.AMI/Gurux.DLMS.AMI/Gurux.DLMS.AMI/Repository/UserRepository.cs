//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Identity;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using System.Linq.Expressions;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.Data;
using Gurux.DLMS.AMI.Data;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class UserRepository : IUserRepository
    {
        private readonly ClaimsPrincipal? User;
        private readonly IGXHost _host;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        public UserRepository(IGXHost host,
            IGXAmiContextAccessor contextAccessor,
            IUserGroupRepository userGroupRepository,
            UserManager<ApplicationUser> userManager,
            IGXEventsNotifier eventsNotifier,
            IServiceProvider serviceProvider)
        {
            User = contextAccessor?.User;
            _host = host;
            _userManager = userManager;
            _eventsNotifier = eventsNotifier;
            _userGroupRepository = userGroupRepository;
            _serviceProvider = serviceProvider;
        }


        /// <inheritdoc />
        public async Task<List<string>> GetUserIdsInRoleAsync(IEnumerable<string> roles)
        {
            List<string> list = new List<string>();
            //Check that user can access this role.
            if (User != null)
            {
                bool isAdmin = User.IsInRole(GXRoles.Admin);
                foreach (var role in roles)
                {
                    if (isAdmin || User.IsInRole(role))
                    {
                        list.Add(role);
                    }
                }
                GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id, where => where.Removed == null);
                args.Joins.AddInnerJoin<GXUser, GXUserRole>(j => j.Id, j => j.UserId);
                args.Joins.AddInnerJoin<GXUserRole, GXRole>(j => j.RoleId, j => j.Id);
                args.Where.And<GXRole>(where => list.Contains(where.Name));
                return (await _host.Connection.SelectAsync<GXUser>(args)).Select(s => s.Id).ToList();
            }
            return list;
        }

        /// <summary>
        /// Return user roles.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="admin">Is user admin.</param>
        /// <returns>Available user roles.</returns>
        private async Task<List<string>> GetUserRoleNamesAsync(string userId, bool admin)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXRole>(s => s.Name);
            if (!admin)
            {
                arg.Joins.AddInnerJoin<GXRole, GXUserRole>(j => j.Id, j => j.RoleId);
                arg.Where.And<GXUserRole>(w => w.UserId == userId);
            }
            return await _host.Connection.SelectAsync<string>(arg);
        }

        /// <summary>
        /// Return user scopes.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="admin">Is user admin.</param>
        /// <returns>Available user scopes.</returns>
        private async Task<List<string>> GetUserScopeNamesAsync(string userId, bool admin)
        {
            GXSelectArgs arg = GXSelectArgs.Select<GXRole>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXRole, GXScope>(j => j.Id, j => j.Role);
            arg.Columns.Add<GXScope>(s => new { s.Id, s.Name, s.Role });
            if (!admin)
            {
                arg.Joins.AddInnerJoin<GXScope, GXUserScope>(j => j.Id, j => j.ScopeId);
                arg.Where.Or<GXUserScope>(w => w.UserId == userId);
            }
            var roles = await _host.Connection.SelectAsync<GXRole>(arg);
            List<string> scopes = new List<string>();
            foreach (var role in roles)
            {
                scopes.AddRange(role.GetScopes());
            }
            return scopes;
        }

        /// <summary>
        /// Return roles and scopes that user can access.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="isAdmin">Is user admin.</param>
        /// <returns>Available user roles and scopes.</returns>
        private async Task<List<GXRole>> GetUserRolesAndScopesAsync(string userId, bool isAdmin)
        {
            GXSelectArgs arg;
            if (!isAdmin)
            {
                //Get scopes.
                arg = GXSelectArgs.Select<GXRole>(s => new { s.Id, s.Name });
                arg.Columns.Add<GXScope>(s => new { s.Id, s.Name, s.Role });
                arg.Joins.AddLeftJoin<GXRole, GXScope>(j => j.Id, j => j.Role);
                arg.Joins.AddInnerJoin<GXScope, GXUserScope>(j => j.Id, j => j.ScopeId);
                arg.Where.Or<GXUserScope>(w => w.UserId == userId);
                List<GXRole> list = await _host.Connection.SelectAsync<GXRole>(arg);
                //Get roles.
                arg = GXSelectArgs.Select<GXRole>(s => new { s.Id, s.Name });
                if (!isAdmin)
                {
                    arg.Joins.AddInnerJoin<GXRole, GXUserRole>(j => j.Id, j => j.RoleId);
                    arg.Where.And<GXUserRole>(w => w.UserId == userId);
                }
                list.AddRange(await _host.Connection.SelectAsync<GXRole>(arg));
                return list;
            }
            arg = GXSelectArgs.Select<GXRole>(s => new { s.Id, s.Name });
            arg.Columns.Add<GXScope>(s => new { s.Id, s.Name, s.Role });
            arg.Joins.AddLeftJoin<GXRole, GXScope>(j => j.Id, j => j.Role);
            return await _host.Connection.SelectAsync<GXRole>(arg);
        }

        /// <summary>
        /// Return users who can access the user.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <remarks>
        /// Manager and user must be on the same user group to allow access.
        /// </remarks>
        List<string> GetUsersAsync(string userId)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.User) &&
                !User.IsInRole(GXRoles.UserManager)))
            {
                throw new UnauthorizedAccessException();
            }
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            string creatorId = ServerHelpers.GetUserId(User);
            GXSelectArgs args;
            //Check that user belongs for the same group as the manager
            string[] roles = [GXRoles.Admin, GXRoles.UserManager];
            if (!isAdmin)
            {
                //Get all user groups where user belongs.
                args = GXSelectArgs.Select<GXUserGroupUser>(s => s.UserGroupId, where => where.UserId == creatorId);
                List<Guid> groups = _host.Connection.Select<GXUserGroupUser>(args).Select(s => s.UserGroupId).ToList();
                args = GXSelectArgs.Select<GXUser>(s => s.Id, s => s.Id == userId);
                args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
                args.Where.And<GXUserGroupUser>(where => groups.Contains(where.UserGroupId));
                List<GXUser> users = _host.Connection.Select<GXUser>(args);
                if (!users.Any())
                {
                    return new List<string>();
                }
            }
            //Return all manager or admin users that belong to the same group to notification them.
            args = GXSelectArgs.Select<GXUser>(s => s.Id);
            args.Joins.AddInnerJoin<GXUser, GXUserRole>(a => a.Id, b => b.UserId);
            args.Joins.AddInnerJoin<GXUserRole, GXRole>(a => a.RoleId, b => b.Id);
            args.Joins.AddInnerJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            if (!isAdmin)
            {
                args.Where.And<GXRole>(where => roles.Contains(where.Name));
            }
            args.Where.And<GXUserGroupUser>(where => where.UserId == userId);
            List<string> list = _host.Connection.Select<GXUser>(args).Select(s => s.Id).ToList();
            //Notification admin also even not in the user group.
            if (isAdmin)
            {
                list.Add(creatorId);
            }
            return list;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            IEnumerable<string> users,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.UserManager)))
            {
                throw new UnauthorizedAccessException();
            }
            if (!users.Any())
            {
                throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
            }
            Dictionary<string, List<string>> updates = new Dictionary<string, List<string>>();
            foreach (string it in users)
            {
                if (it == ServerHelpers.GetUserId(User))
                {
                    throw new ArgumentException(Properties.Resources.YouCanRemoveYourself);
                }
                List<string> list = GetUsersAsync(it);
                if (list == null || list.Count == 0)
                {
                    throw new UnauthorizedAccessException("Invalid user ID.");
                }
                updates[it] = list;
            }
            if (delete)
            {
                GXDeleteArgs del = GXDeleteArgs.Delete<GXUser>(w => users.Contains(w.Id));
                await _host.Connection.DeleteAsync(del);
            }
            else
            {
                //Set removed time for all the removed users.
                GXUser u = new GXUser() { Removed = DateTime.Now };
                GXUpdateArgs update = GXUpdateArgs.Update(u, q => q.Removed);
                update.Where.And<GXUser>(q => users.Contains(q.Id));
                _host.Connection.Update(update);
            }
            GXPerformanceSettings? performanceSettings = _serviceProvider.GetService<GXPerformanceSettings>();
            if (performanceSettings != null)
            {
                foreach (var it in updates)
                {
                    var users2 = NotificationRepository.GetNotifiedUsers(_host.Connection, performanceSettings,
                        it.Value, TargetType.User, NotificationAction.Remove);
                    if (users2 == null)
                    {
                        break;
                    }
                    await _eventsNotifier.UserDelete(users2, [new GXUser() { Id = it.Key }]);
                }
            }
        }

        /// <inheritdoc />
        public async Task<GXUser[]> ListAsync(
            ListUsers? request,
            ListUsersResponse? response,
            CancellationToken cancellationToken)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.User) &&
                !User.IsInRole(GXRoles.UserManager)))
            {
                throw new UnauthorizedAccessException();
            }
            string userId = ServerHelpers.GetUserId(User);
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            GXSelectArgs arg;
            bool allUsers = request != null && request.AllUsers && User.IsInRole(GXRoles.Admin);
            if (allUsers)
            {
                //Admin can see all the users.
                arg = GXSelectArgs.SelectAll<GXUser>();
                //Get user groups where user belongs.
                arg.Joins.AddLeftJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
                arg.Joins.AddLeftJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
                arg.Where.And<GXUserGroup>(q => q.Removed == null);
            }
            else
            {
                arg = GXQuery.GetUsersByUser(s => "*", userId);
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
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXUser>(w => !request.Exclude.Contains(w.Id));
                }
                if (request?.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXUser>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && request.Filter != null && request.Filter.Roles != null && request.Filter.Roles.Count != 0)
            {
                arg.Where.And<GXRole>(q => request.Filter.Roles.Contains(q.Name));
            }
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXUser>(q => GXSql.DistinctCount(q.Id));
                total.Distinct = true;
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            GXUser[] users = (await _host.Connection.SelectAsync<GXUser>(arg)).ToArray();
            //Roles must retrieve separetly because only role and scope names are shown for security reasons.
            foreach (GXUser it in users)
            {
                bool admin = it.Roles?.Contains(GXRoles.Admin) == true;
                it.Roles = await GetUserRoleNamesAsync(it.Id, admin);
                it.Scopes = await GetUserScopeNamesAsync(it.Id, admin);
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
        public async Task<GXUser> ReadAsync(string? id)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.User) &&
                !User.IsInRole(GXRoles.UserManager)))
            {
                throw new UnauthorizedAccessException();
            }
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            if (string.IsNullOrEmpty(id))
            {
                //Returns information for the current user.
                id = ServerHelpers.GetUserId(User);
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXUser>(q => q.Id == id);
            //Get user groups where user belongs.
            arg.Columns.Add<GXUserGroup>();
            arg.Distinct = true;
            //Users are ignored from the user group
            //so there is no reference relation that is causing problems with JSON parser.
            arg.Columns.Exclude<GXUserGroup>(e => new { e.Users, e.Creator });
            //Get user groups where user belongs.
            arg.Joins.AddLeftJoin<GXUser, GXUserGroupUser>(a => a.Id, b => b.UserId);
            arg.Joins.AddLeftJoin<GXUserGroupUser, GXUserGroup>(a => a.UserGroupId, b => b.Id);
            arg.Where.And<GXUserGroup>(q => q.Removed == null);
            if (!isAdmin)
            {
                //Get only users that are in the same user groups than the user.
                arg.Where.And<GXUserGroupUser>(q => q.UserId == id);
            }
            arg.OrderBy.Add<GXUser>(q => q.CreationTime);
            arg.Columns.Exclude<GXUser>(q => new { q.PasswordHash, q.SecurityStamp });
            if (!isAdmin)
            {
                //Only admin can see removed users.
                arg.Where.And<GXUser>(q => q.Removed == null);
            }

            GXUser ret = (await _host.Connection.SingleOrDefaultAsync<GXUser>(arg));
            if (ret == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Roles must retrieve separetly because only role and scope names are shown for security reasons.
            bool admin = ret.Roles?.Contains(GXRoles.Admin) == true;
            ret.Roles = await GetUserRoleNamesAsync(id, admin);
            ret.Scopes = await GetUserScopeNamesAsync(id, admin);
            //Read user settings.
            arg = GXSelectArgs.SelectAll<GXUserSetting>(q => q.User == ret);
            arg.Distinct = true;
            ret.Settings = await _host.Connection.SelectAsync<GXUserSetting>(arg);
            if (ret.Settings == null)
            {
                ret.Settings = new List<GXUserSetting>();
            }
            //Read rows per page from the system settings.
            GXConfiguration conf = _host.Connection.SingleOrDefault<GXConfiguration>(GXSelectArgs.SelectAll<GXConfiguration>(w => w.Name == GXConfigurations.System));
            SystemSettings? settings = null;
            if (conf != null && !string.IsNullOrEmpty(conf.Settings))
            {
                settings = JsonSerializer.Deserialize<SystemSettings>(conf.Settings);
            }
            if (settings == null)
            {
                settings = new SystemSettings();
            }
            SystemSettings s = new SystemSettings()
            {
                RowsPerPage = settings.RowsPerPage
            };
            ret.Settings.Add(new GXUserSetting()
            {
                Name = GXConfigurations.System,
                Value = JsonSerializer.Serialize(s)
            });
            return ret;
        }

        /// <summary>
        /// Add new user.
        /// </summary>
        /// <param name="givenName">Given name.</param>
        /// <param name="surname">Surname (last name).</param>
        /// <param name="email">User email address.</param>
        /// <param name="password">User password.</param>
        /// <param name="roles">User roles.</param>
        /// <param name="scopes">User scopes.</param>
        private async Task<string> AddUser(
            string? givenName,
            string? surname,
            string email,
            string? password,
            List<string>? roles,
            List<string>? scopes)
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
                IdentityResult ret;
                if (string.IsNullOrEmpty(password))
                {
                    ret = await _userManager.CreateAsync(newAppUser);
                }
                else
                {
                    ret = await _userManager.CreateAsync(newAppUser, password);
                }
                if (!ret.Succeeded)
                {
                    throw new Exception("Failed to create a new user." + Environment.NewLine +
                        string.Join(Environment.NewLine, ret.Errors.Select(q => q.Description)));
                }
                if (roles?.Any() == true)
                {
                    ret = await _userManager.AddToRolesAsync(newAppUser, roles);
                    if (!ret.Succeeded)
                    {
                        throw new Exception("Failed to add role for the new user." + Environment.NewLine +
                            string.Join(Environment.NewLine, ret.Errors.Select(q => q.Description)));
                    }
                }
                if (scopes?.Any() == true)
                {
                    List<Claim> claims = new List<Claim>();
                    foreach (var it in scopes)
                    {
                        claims.Add(new Claim("scope", it));
                    }
                    await _userManager.AddClaimsAsync(user, claims);
                }

                List<string> list = GetUsersAsync(newAppUser.Id);
                if (list == null || list.Count == 0)
                {
                    throw new UnauthorizedAccessException("Invalid user ID.");
                }
                return newAppUser.Id;
            }
            return user.Id;
        }

        /// <inheritdoc />
        public async Task<string[]> UpdateAsync(
            IEnumerable<GXUser> users,
            Expression<Func<GXUser, object?>>? columns)
        {
            if (User == null ||
                (!User.IsInRole(GXRoles.Admin) &&
                !User.IsInRole(GXRoles.User) &&
                !User.IsInRole(GXRoles.UserManager)))
            {
                throw new UnauthorizedAccessException();
            }
            string creatorId = ServerHelpers.GetUserId(User);
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            DateTime now = DateTime.Now;
            Dictionary<GXUser, List<string>> updates = new Dictionary<GXUser, List<string>>();
            List<string> list = new List<string>();
            List<GXUserGroup>? defaultGroups = null;
            //Get list from roles that creator can access.
            List<GXRole> available = await GetUserRolesAndScopesAsync(creatorId, isAdmin);
            foreach (GXUser user in users)
            {
                //Get roles and scopes that user can access.
                GXRole[] roles = GXRole.GetRoles(available, user.Roles, false, true);
                GXScope[] scopes = GXRole.GetScopes(available, user.Scopes, true);
                if (string.IsNullOrEmpty(user.Email))
                {
                    throw new ArgumentException(Properties.Resources.InvalidEmailAddress);
                }
                if (user.Roles?.Any() != true && user.Scopes?.Any() != true)
                {
                    throw new ArgumentException(Properties.Resources.InvalidUserRoleAtLeastOneRoleMustBeSelected);
                }
                //Remove all other roles when user is admin.
                if (user.Roles?.Any(s => string.Equals(s, GXRoles.Admin, StringComparison.OrdinalIgnoreCase)) == true)
                {
                    user.Roles.Clear();
                    user.Scopes?.Clear();
                    user.Roles.Add(GXRoles.Admin);
                }
                user.NormalizedEmail = user.Email.ToUpper();
                if (string.IsNullOrEmpty(user.Id))
                {
                    if (user.UserGroups == null || !user.UserGroups.Any())
                    {
                        if (defaultGroups == null)
                        {
                            //Get default user groups.
                            ListUserGroups request = new ListUserGroups()
                            {
                                Filter = new GXUserGroup() { Default = true }
                            };
                            defaultGroups = [.. await _userGroupRepository.ListAsync(request, null, CancellationToken.None)];
                        }
                        user.UserGroups = [.. defaultGroups];
                        if (!user.UserGroups.Any())
                        {
                            throw new ArgumentException(Properties.Resources.UsersMustBelongToAtLeastOneUserGroup);
                        }
                    }
                    //Check that user email is not added yet.
                    GXSelectArgs total = GXSelectArgs.Select<GXUser>(q => GXSql.Count(q));
                    total.Where.And<GXUser>(q => q.NormalizedEmail.Contains(user.Email));
                    if (await _host.Connection.SingleOrDefaultAsync<int>(total) != 0)
                    {
                        throw new ArgumentException(string.Format("Email address '{0}' already in use.", user.Email));
                    }
                    user.Id = await AddUser(user.GivenName, user.Surname, user.Email, user.Password, user.Roles, user.Scopes);
                    list.Add(user.Id);
                    //Add default user groups.
                    List<GXUserGroupUser> tmp = new List<GXUserGroupUser>();
                    //Insert new user groups that are not in the DB.
                    foreach (var it in user.UserGroups)
                    {
                        tmp.Add(new GXUserGroupUser() { UserId = user.Id, UserGroupId = it.Id });
                    }
                    GXInsertArgs args = GXInsertArgs.InsertRange(tmp);
                    _host.Connection.Insert(args);
                }
                else
                {
                    if (user.UserGroups == null || !user.UserGroups.Any())
                    {
                        throw new ArgumentException(Properties.Resources.UsersMustBelongToAtLeastOneUserGroup);
                    }
                    GXSelectArgs m = GXSelectArgs.Select<GXUser>(q => q.ConcurrencyStamp, where => where.Id == user.Id);
                    string updated = _host.Connection.SingleOrDefault<string>(m);
                    if (!string.IsNullOrEmpty(updated) && updated != user.ConcurrencyStamp)
                    {
                        throw new ArgumentException(Properties.Resources.ContentEdited);
                    }

                    GXUser tmpUser = GetUser(user.Id);
                    if (user.Email.CompareTo(tmpUser.Email) != 0)
                    {
                        tmpUser.UserName = tmpUser.Email = user.Email;
                        tmpUser.NormalizedUserName = tmpUser.NormalizedEmail = user.Email.ToUpper();
                    }
                    var roleComparer = new UniqueComparer<GXRole, Guid>();
                    var scopeComparer = new UniqueComparer<GXScope, Guid>();
                    //Get current roles and scopes.
                    List<GXRole> current = await GetUserRolesAndScopesAsync(user.Id, false);
                    var existingScopes = current.SelectMany(s => s.Scopes ?? Enumerable.Empty<GXScope>()).ToArray();
                    var scopeRoles = existingScopes.Select(s => s.Role).Cast<GXRole>().ToArray();
                    //Get roles that are not part of scopes.
                    current = current.Except(scopeRoles).ToList();
                    //GXRole[] current2 = current.Where(w => w.Scopes?.Any() != true).ToArray();
                    List<GXRole> addedRoles = roles.Except(current, roleComparer).ToList();
                    List<GXRole> removedRoles = current.Except(roles, roleComparer).ToList();
                    List<GXScope> removedScopes = existingScopes.Except(scopes, scopeComparer).ToList();
                    List<GXScope> addedScopes = scopes.Except(existingScopes, scopeComparer).ToList();
                    if (removedRoles.Any())
                    {
                        var ids = removedRoles.Select(w => w.Id).ToArray();
                        GXDeleteArgs del = GXDeleteArgs.Delete<GXUserRole>(w => ids.Contains(w.RoleId));
                        await _host.Connection.DeleteAsync(del);
                    }
                    if (addedRoles.Any())
                    {
                        var tmp = new List<GXUserRole>();
                        foreach (var r in addedRoles)
                        {
                            tmp.Add(new GXUserRole()
                            {
                                UserId = user.Id,
                                RoleId = r.Id,
                            });
                        }
                        GXInsertArgs insert = GXInsertArgs.InsertRange(tmp);
                        await _host.Connection.InsertAsync(insert);
                    }
                    if (removedScopes.Any())
                    {
                        var ids = removedScopes.Select(w => w.Id).Cast<Guid?>().ToArray();
                        GXDeleteArgs del = GXDeleteArgs.Delete<GXUserScope>(w => ids.Contains(w.ScopeId));
                        await _host.Connection.DeleteAsync(del);
                    }
                    if (addedScopes.Any())
                    {
                        var tmp = new List<GXUserScope>();
                        foreach (var s in addedScopes)
                        {
                            tmp.Add(new GXUserScope()
                            {
                                UserId = user.Id,
                                ScopeId = s.Id,
                            });
                        }
                        GXInsertArgs insert = GXInsertArgs.InsertRange(tmp);
                        await _host.Connection.InsertAsync(insert);
                    }
                    user.Updated = DateTime.Now;
                    List<GXUserGroup> userGroups = await _userGroupRepository.GetJoinedUserGroups(user.Id);
                    var comparer = new UniqueComparer<GXUserGroup, Guid>();
                    List<GXUserGroup> removedUserGroups = userGroups.Except(user.UserGroups, comparer).ToList();
                    List<GXUserGroup> addedUserGroups = user.UserGroups.Except(userGroups, comparer).ToList();
                    if (removedUserGroups.Any())
                    {
                        //Remove un-checked user groups from the DB.
                        List<Guid> tmp = new List<Guid>();
                        foreach (var it in removedUserGroups)
                        {
                            tmp.Add(it.Id);
                        }
                        GXDeleteArgs args = GXDeleteArgs.Delete<GXUserGroupUser>(where => where.UserId == user.Id && tmp.Contains(where.UserGroupId));
                        _host.Connection.Delete(args);
                    }
                    if (addedUserGroups.Any())
                    {
                        List<GXUserGroupUser> tmp = new List<GXUserGroupUser>();
                        //Insert new user groups that are not in the DB.
                        foreach (var it in addedUserGroups)
                        {
                            tmp.Add(new GXUserGroupUser() { UserId = user.Id, UserGroupId = it.Id });
                        }
                        GXInsertArgs args = GXInsertArgs.InsertRange(tmp);
                        _host.Connection.Insert(args);
                    }
                    user.Updated = now;
                    user.ConcurrencyStamp = Guid.NewGuid().ToString();
                    GXUpdateArgs arg2 = GXUpdateArgs.Update(user, columns);
                    arg2.Exclude<GXUser>(q => new { q.PasswordHash, q.SecurityStamp, q.CreationTime, q.UserGroups, q.Roles });
                    await _host.Connection.UpdateAsync(arg2);
                }
                updates[user] = GetUsersAsync(user.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.UserUpdate(it.Value, new GXUser[] { it.Key });
            }
            return list.ToArray();
        }

        private GXUser GetUser(string userId)
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<GXUser>(where => where.Id == userId);
            return _host.Connection.SingleOrDefault<GXUser>(args);
        }
    }
}
