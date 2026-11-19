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

using Gurux.DLMS.AMI.Client.Pages.Agent;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;


namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    /// <summary>
    /// Constructor.
    /// </summary>
    partial class EnumTypeRepository(IDbConnectionFactory dbFactory,
        IHttpContextAccessor httpContextAccessor) : IEnumTypeRepository
    {
        /// <summary>Principal whose identity and claims govern repository access.</summary>
        private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;
        /// <summary>Creates database connections for repository queries and mutations.</summary>
        private readonly IDbConnectionFactory _dbFactory = dbFactory;

        /// <inheritdoc />
        public async Task<int> GetLogTypeAsync(string type,
            string name,
            CancellationToken cancellationToken)
        {
            return await GXHistoryScope.RunAsync(_dbFactory, async historyConnection =>
            {
                if (_user == null)
                {
                    throw new UnauthorizedAccessException();
                }
                var historyName = name;
                var trackedType = await historyConnection.SingleOrDefaultAsync<GXEnumType>(
                    GXSelectArgs.SelectAll<GXEnumType>(e => e.Type == type && e.Name == historyName), cancellationToken)
                    ?? new GXEnumType { Type = type, Name = historyName };
                return await GXHistoryMutation.RunAsync<GXEnumType, int>(_dbFactory, _user,
                    new HttpContextAccessor().HttpContext?.RequestServices, new[] { trackedType }, async () =>
                    {
                        var id = await ApplyChangeAsync();
                        trackedType.Id = id;
                        return id;
                    }, cancellationToken);
            }, cancellationToken);

            async Task<int> ApplyChangeAsync()
            {
            if (_user == null)
            {
                throw new UnauthorizedAccessException();
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXEnumType>(w => w.Type == type && w.Name == name);
            arg.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                GXEnumType? lt = await connection.SingleOrDefaultAsync<GXEnumType>(transaction, arg);
                if (lt == null)
                {
                    lt = new GXEnumType() { Type = type, Name = name };
                    connection.Insert(transaction, GXInsertArgs.Insert(lt));
                    await transaction.CommitAsync(cancellationToken);
                }
                return lt.Id;
            }
            catch (GXDatabaseException ex)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);                        // If deadlock, wait max 1 seconds and retry.
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await GetLogTypeAsync(type, name, linkedCts.Token);
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
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
                    }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXEnumType>> ListAsync(ListEnumTypes? request,
           ListEnumTypesResponse? response = null,
           CancellationToken cancellationToken = default)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXEnumType>();
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude?.Any() == true)
                {
                    arg.Where.And<GXEnumType>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included?.Any() == true)
                {
                    arg.Where.And<GXEnumType>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXEnumType>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXEnumType>(q => q.Name!);
                arg.Descending = false;
            }
            arg.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXEnumType>(q => GXSql.Count(1));
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
            var types = (await connection.SelectAsync<GXEnumType>(arg)).ToArray();
            response?.Types = types;
            return types;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string type,
            CancellationToken cancellationToken = default)
        {
            await GXHistoryScope.RunAsync(_dbFactory, async historyConnection =>
            {
                var historyIds = await historyConnection.SelectAsync<int>(
                    GXSelectArgs.Select<GXEnumType>(e => e.Id, e => e.Type == type), cancellationToken);
                await GXHistoryMutation.RunIdsAsync<GXEnumType, int>(_dbFactory, _user,
                    new HttpContextAccessor().HttpContext?.RequestServices, historyIds,
        async () =>
        {
            try
            {
                if (_user == null ||
                    !_user.IsInRole(GXRoles.Admin) && !_user.IsInRole(GXRoles.Authenticated) &&
                    _user.Identity?.IsAuthenticated != true)
                {
                    throw new UnauthorizedAccessException();
                }
                GXDeleteArgs args = GXDeleteArgs.Delete<GXEnumType>(w => w.Type == type);
                await using var connection = await _dbFactory.GetHistoryConnectionAsync();
                await connection.DeleteAsync(args, cancellationToken);
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
                        await DeleteAsync(type, linkedCts.Token);
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
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> GetLogTypeAsync(string type, Enum value,
            CancellationToken cancellationToken)
        {
            return await GXHistoryScope.RunAsync(_dbFactory, async historyConnection =>
            {
                if (_user == null)
                {
                    throw new UnauthorizedAccessException();
                }
                var historyName = value.ToString();
                var trackedType = await historyConnection.SingleOrDefaultAsync<GXEnumType>(
                    GXSelectArgs.SelectAll<GXEnumType>(e => e.Type == type && e.Name == historyName), cancellationToken)
                    ?? new GXEnumType { Type = type, Name = historyName };
                return await GXHistoryMutation.RunAsync<GXEnumType, int>(_dbFactory, _user,
                    new HttpContextAccessor().HttpContext?.RequestServices, new[] { trackedType }, async () =>
                    {
                        var id = await ApplyChangeAsync();
                        trackedType.Id = id;
                        return id;
                    }, cancellationToken);
            }, cancellationToken);

            async Task<int> ApplyChangeAsync()
            {
            if (_user == null)
            {
                throw new UnauthorizedAccessException();
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException(nameof(type));
            }
            var name = value.ToString();
            Enum.GetValues(value.GetType());
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("The enum value does not have a name.", nameof(value));
            }
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXEnumType>(w => w.Type == type && w.Name == name);
            arg.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                GXEnumType? lt = await connection.SingleOrDefaultAsync<GXEnumType>(transaction, arg, cancellationToken);
                if (lt == null)
                {
                    lt = new GXEnumType() { Type = type, Name = name, Value = Convert.ToInt32(value) };
                    await connection.InsertAsync(transaction, GXInsertArgs.Insert(lt), cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
                return lt.Id;
            }
            catch (GXDatabaseException ex)
            {
                transaction.Rollback();
                if (ex.ErrorType == DatabaseErrorType.Deadlock)
                {
                    //Max time is 5 seconds to avoid infinite retry when there is some other issue that cause deadlock and not just temporary deadlock.
                    CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(5));
                    try
                    {
                        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                        // If deadlock, wait max 1 second and retry.
                        await Task.Delay(1000, linkedCts.Token);
                        return await GetLogTypeAsync(type, value, linkedCts.Token);
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
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
                    }
        }

        // Lifecycle operations share this repository's persistence rules and notification queue.
        /// <summary>Creates repository operations restricted to the identity and transaction of the supplied module scope.</summary>
        /// <param name="scope">Module identity and transaction governing all repository operations.</param>
        /// <returns>A module-scoped repository facade.</returns>
        internal static IModuleEnumTypeRepository ForModule(GXModuleRepositoryScope scope) => new ModuleOperations(scope);

        /// <summary>Implements repository operations within one owning module's identity and transaction scope.</summary>
        /// <param name="scope">Module or history scope that owns this non-owning adapter.</param>
        private sealed class ModuleOperations(GXModuleRepositoryScope scope) : IModuleEnumTypeRepository
        {
            /// <inheritdoc />
            public async Task<int> EnsureLogCategoryAsync(string name, CancellationToken token)
            {
                if (string.IsNullOrWhiteSpace(name) || name.Length > 64)
                {
                    throw new ArgumentException("Invalid log category.");
                }
                var existing = await scope.Connection.SingleOrDefaultAsync<GXEnumType>(scope.Transaction,
                    GXSelectArgs.SelectAll<GXEnumType>(e => e.Type == "log" && e.Name == name), token);
                if (existing != null)
                {
                    return existing.Id;
                }
                var category = new GXEnumType { Type = "log", Name = name };
                Validator.ValidateObject(category, new ValidationContext(category), true);
                await scope.Connection.InsertAsync(scope.Transaction, GXInsertArgs.Insert(category), token);
                await GXModuleRepositoryNotifications.QueueAsync(scope, "enum-type.add", category.Id.ToString(), token);
                return category.Id;
            }
        }
    }
}
