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

using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Properties;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Resources;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>
    /// Server helper methods.
    /// </summary>
    static class ServerHelpers
    {
        /// <summary>
        /// Retrieves the culture-invariant resource string associated with the specified key, or returns the key itself
        /// if no resource is found.
        /// </summary>
        /// <remarks>This method uses the invariant culture to retrieve resource strings, ensuring
        /// consistent results regardless of the current thread's culture settings.</remarks>
        /// <param name="value">The resource key to look up. Cannot be null.</param>
        /// <returns>The culture-invariant resource string corresponding to the specified key, or the key itself if no matching
        /// resource is found.</returns>
        public static string GetInvariantString(this string value)
        {
            string? tmp = Resources.ResourceManager.GetString(value, CultureInfo.InvariantCulture) ?? value;
            return tmp;
        }

        /// <summary>
        /// Get remote IP address.
        /// </summary>
        /// <param name="req">HttpRequest.</param>
        /// <param name="http">HttpContext.</param>
        public static string? GetRemoteIpAddress(HttpRequest req, HttpContext http)
        {
            string? ip = req.Headers["X-Forwarded-For"]
                            .FirstOrDefault()?
                            .Split(',')[0]
                            .Trim();
            if (string.IsNullOrEmpty(ip))
            {
                ip = http.Connection.RemoteIpAddress?.ToString();
            }
            return ip;
        }

        /// <summary>
        /// Get user ID from claims.
        /// </summary>
        /// <param name="user"></param>
        /// <returns>User ID.</returns>
        public static string GetUserId(ClaimsPrincipal? user)
        {
            string? id = GetUserId(user, true) ?? throw new UnauthorizedAccessException();
            return id;
        }

        /// <summary>
        /// Get user ID from claims.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="throwException">Exception is thrown if user is unknown.</param>
        /// <returns>User ID.</returns>
        public static string? GetUserId(ClaimsPrincipal? user, bool throwException)
        {
            if (user == null)
            {
                return null;
            }
            var id = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (id == null)
            {
                if (throwException)
                {
                    throw new UnauthorizedAccessException();
                }
            }
            return id?.Value;
        }

        /// <summary>
        /// Create ClaimsPrincipal from the user information.
        /// </summary>
        /// <param name="user">User information.</param>
        /// <returns>Claims principal.</returns>
        public static ClaimsPrincipal CreateClaimsPrincipalFromUser(GXUser? user)
        {
            if (user == null || string.IsNullOrEmpty(user.Id))
            {
                throw new UnauthorizedAccessException();
            }
            List<Claim> claims =
            [
                new Claim("sub", user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            ];
            if (user.Roles != null)
            {
                foreach (var role in user.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Basic"));
        }

        /// <summary>
        /// Add distinct items to the list.
        /// </summary>
        /// <param name="self">List where items are added.</param>
        /// <param name="items">Added items.</param>
        public static void AddDistinct(this IList self, IEnumerable items)
        {
            foreach (object item in items)
            {
                if (!self.Contains(item))
                {
                    self.Add(item);
                }
            }
        }

        /// <summary>
        /// Get component views from the assembly and add them to the database.
        /// </summary>
        /// <param name="connection">Database connection.</param>
        /// <param name="transaction">Database transaction.</param>
        /// <param name="assembly">Assebly where component views are searched for.</param>
        public static void UpdateComponentViews(GXDbConnection connection,
            IDbTransaction transaction, Assembly assembly)
        {
            //All values are saved using invariant culture.
            CultureInfo? culture = CultureInfo.DefaultThreadCurrentUICulture;
            try
            {
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                List<GXComponentView> list = [];
                foreach (Type it in assembly.GetTypes())
                {
                    if (!it.IsAbstract && typeof(IGXAmiComponent).IsAssignableFrom(it))
                    {
                        GXSelectArgs args = GXSelectArgs.SelectAll<GXComponentView>(where => where.ClassName == it.FullName);
                        GXComponentView? item = connection.SingleOrDefault<GXComponentView>(transaction, args);
                        if (item == null)
                        {
                            IGXAmiComponent tmp = Activator.CreateInstance(it) as IGXAmiComponent ?? throw new InvalidOperationException($"Could not create component {it.FullName}.");
                            string? configurationUI = null;
                            if (tmp.ConfigurationUI != null)
                            {
                                configurationUI = tmp.ConfigurationUI.FullName;
                            }
                            list.Add(new GXComponentView() { ClassName = it.FullName, Name = tmp.Name, ConfigurationUI = configurationUI, Icon = tmp.Icon });
                        }
                    }
                }
                connection.Insert(transaction, GXInsertArgs.InsertRange(list));
            }
            finally
            {
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
        }

        /// <summary>
        /// Update localized strings.
        /// </summary>
        /// <param name="connection">DB connection.</param>
        /// <param name="enumTypeRepository">Enum type repository</param>
        /// <param name="languages">Updated languages</param>
        /// <param name="creator">Creator</param>
        internal static async Task<List<GXLanguage>> UpdateLanguageResourcesAsync(
            GXDbConnection connection,
            IEnumTypeRepository enumTypeRepository,
            IEnumerable<GXLanguage>? languages,
            GXUser creator)
        {
            await UpdateLanguageResourcesAsync(connection, enumTypeRepository, typeof(AccountSettings), languages, creator);
            return await UpdateLanguageResourcesAsync(connection, enumTypeRepository, typeof(Properties.Resources), languages, creator);
        }

        /// <summary>
        /// Update localized strings.
        /// </summary>
        /// <param name="enumTypeRepository">Enum type repository</param>
        /// <param name="type"></param>
        /// <param name="languages">Updated languages</param>
        /// <param name="creator">Creator</param>
        /// <param name="connection">The database connection.</param>
        private static async Task<List<GXLanguage>> UpdateLanguageResourcesAsync(
            GXDbConnection connection,
            IEnumTypeRepository enumTypeRepository,
            Type type,
            IEnumerable<GXLanguage>? languages,
            GXUser creator)
        {
            List<GXLanguage> updated = [];
            List<GXLocalizedResource> strings = [];
            List<GXAppearance> images = [];
            GXSelectArgs args;
            //Update activated languages.
            args = GXSelectArgs.SelectAll<GXLanguage>(w => w.Enabled == true);
            if (languages?.Any() == true)
            {
                var ids = languages.Select(s => s.Id).ToList();
                args.Where.And<GXLanguage>(w => ids.Contains(w.Id));
            }
            languages = [.. connection.Select<GXLanguage>(args)];
            DateTime now = DateTime.Now;
            string name = type.Assembly.GetName().Name + ".Properties.Resources";
            //Update resources.
            ResourceManager rm = new(name, type.Assembly);
            try
            {
                ResourceSet? invariantResourceSet = rm.GetResourceSet(CultureInfo.InvariantCulture, true, true);
                foreach (var lang in languages)
                {

                    CultureInfo culture;
                    if (lang.Id == "en")
                    {
                        culture = CultureInfo.InvariantCulture;
                    }
                    else
                    {
                        culture = CultureInfo.GetCultureInfo(lang.Id);
                    }
                    ResourceSet? localResourceSet = rm.GetResourceSet(culture, true, false);
                    if (invariantResourceSet != null && localResourceSet != null)
                    {
                        try
                        {
                            foreach (DictionaryEntry it in invariantResourceSet)
                            {
                                if (it.Key is string id)
                                {
                                    if (it.Value is string value)
                                    {
                                        string? localString = localResourceSet.GetString(id);
                                        string status = "Success";
                                        if (localString == null)
                                        {
                                            status = "Localized value missing";
                                            localString = value;
                                        }
                                        //Add new localized strings.
                                        string hash = ClientHelpers.GetHashCode(value);
                                        args = GXSelectArgs.SelectAll<GXLocalizedResource>(w => w.Hash == hash);
                                        args.Joins.AddLeftJoin<GXLocalizedResource, GXLanguage>(j => j.Language!, j => j.Id);
                                        args.Where.And<GXLanguage>(w => w.Id == lang.Id);
                                        GXLocalizedResource? res = connection.SingleOrDefault<GXLocalizedResource>(args);
                                        if (res == null)
                                        {
                                            res = new GXLocalizedResource()
                                            {
                                                Status = await enumTypeRepository.GetLogTypeAsync(TargetType.LocalizedResource, status),
                                                Hash = hash,
                                                Value = localString,
                                                Language = lang,
                                                Creator = creator,
                                                CreationTime = now
                                            };
                                            try
                                            {
                                                await connection.InsertAsync(GXInsertArgs.Insert(res));
                                                strings.Add(res);
                                            }
                                            catch (Exception)
                                            {
                                                throw;
                                                //string is ignored if it already exists.
                                            }
                                        }
                                    }
                                    else if (it.Value is byte[] ba2)
                                    {
                                        //Update image.
                                        images.Add(new GXAppearance()
                                        {
                                            Name = id,
                                            Creator = creator,
                                            CreationTime = now,
                                            ResourceType = (byte)ResourceType.Image,
                                            Value = ASCIIEncoding.ASCII.GetString(ba2),
                                        });
                                    }
                                }
                            }

                        }
                        catch (Exception)
                        {
                            //TODO: Log error.
                        }
                    }
                }
            }
            finally
            {
                rm.ReleaseAllResources();
            }
            return updated;
        }

        public static void AddHostedService<TService, TImplementation>(this IServiceCollection services)
 where TService : class
 where TImplementation : class, IHostedService, TService
        {
            services.AddSingleton<TService, TImplementation>();
            services.AddHostedService<HostedServiceWrapper<TService>>();
        }

        private class HostedServiceWrapper<TService>(TService hostedService) : IHostedService
        {
            private readonly IHostedService _hostedService = hostedService as IHostedService ?? throw new ArgumentException("The service must implement IHostedService.", nameof(hostedService));

            public Task StartAsync(CancellationToken cancellationToken)
            {
                return _hostedService.StartAsync(cancellationToken);
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return _hostedService.StopAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Check if expression contains given parameter.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool Contains(Expression? expression, string name)
        {
            return expression switch
            {
                LambdaExpression lambda => Contains(lambda.Body, name),
                UnaryExpression unary => Contains(unary.Operand, name),
                MemberExpression member => member.Member.Name == name,
                NewExpression creation => creation.Arguments.Any(argument => Contains(argument, name)),
                NewArrayExpression array => array.Expressions.Any(argument => Contains(argument, name)),
                _ => false
            };
        }

        /// <summary>
        /// Filter by.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter">Filter.</param>
        public static void FilterBy<T>(List<T> target, T? filter)
        {
            List<T> list = [];
            foreach (var it in target)
            {
                if (FilterBy(it, filter))
                {
                    list.Add(it);
                }
            }
            target.RemoveAll(w => list.Contains(w));
        }

        public static bool Compare(object? val1, object? val2)
        {
            if (val1 == null || val2 == null)
            {
                return ReferenceEquals(val1, val2);
            }
            if (val1.GetType() == typeof(string) || val1.GetType() == typeof(Guid))
            {

            }
            else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(val1.GetType()))
            {
                IEnumerator v1 = ((IEnumerable)val1).GetEnumerator();
                IEnumerator v2 = ((IEnumerable)val2).GetEnumerator();
                while (v1.MoveNext() && v2.MoveNext())
                {
                    val1 = v1.Current;
                    val2 = v2.Current;
                    Compare(val1, val2);
                }
                if (v1.MoveNext() || v2.MoveNext())
                {
                    Debug.WriteLine("DIFFERENT amount of values. ");
                    return false;
                }
                return true;
            }
            else if (val1.GetType().IsClass &&
                val1.GetType() != typeof(DefaultJsonTypeInfoResolver) &&
                val1.GetType() != typeof(ModelAttributes) &&
                val1.GetType().Name != "RuntimeType" &&
                val1.GetType().Namespace != typeof(System.Text.Json.JsonCommentHandling).Namespace &&
                val1.GetType().Namespace != typeof(System.Text.EncodingProvider).Namespace &&
                val1.GetType() != typeof(StringOutputFormatter))
            {
                IEnumerator p1 = val1.GetType().GetProperties().GetEnumerator();
                IEnumerator p2 = val2.GetType().GetProperties().GetEnumerator();
                while (p1.MoveNext() && p2.MoveNext())
                {
                    object? value = ((PropertyInfo)p1.Current).GetValue(val1);
                    object? value2 = ((PropertyInfo)p2.Current).GetValue(val2);
                    if (value != null &&
                        value.GetType() != typeof(ModelAttributes) &&
                        value.GetType().Namespace != typeof(System.Text.Json.JsonCommentHandling).Namespace &&
                        value.GetType() != typeof(DefaultJsonTypeInfoResolver) &&
                        value.GetType() != typeof(StringOutputFormatter))
                    {
                        if (value.GetType() != typeof(string) &&
                            value.GetType() != typeof(Guid) &&
                            value.GetType().IsClass == true)
                        {
                            if (!Compare(value, value2))
                            {
                                return false;
                            }
                        }
                        if (value?.Equals(value2) != true)
                        {
                            Debug.WriteLine("DIFFERENT: " + value + ": " + value2);
                            return false;
                        }
                        else
                        {
                            Debug.WriteLine("SAME: " + value + ": " + value2);
                        }
                    }
                }
                if (p1.MoveNext() || p2.MoveNext())
                {
                    Debug.WriteLine("DIFFERENT amount of values. ");
                    return false;
                }
            }
            if (val1.Equals(val2) != true)
            {
                Debug.WriteLine("DIFFERENT" + val1 + ": " + val2);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Filter by.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="filter">Filter.</param>
        private static bool FilterBy<T>(T target, T? filter)
        {
            if (target != null && filter != null)
            {
                Dictionary<PropertyInfo, FilterAttribute> properties = [];
                foreach (var it in filter.GetType().GetProperties())
                {
                    var att = it.GetCustomAttribute<FilterAttribute>();
                    if (it != null && att != null)
                    {
                        properties.Add(it, att);
                    }
                }
                foreach (var it in properties)
                {
                    object? actual = it.Key?.GetValue(target);
                    object? filterValue = it.Key?.GetValue(filter);
                    if (actual == null && filterValue == null)
                    {
                    }
                    else if (actual != null && filterValue != null)
                    {
                        if (actual is DateTime d && filterValue is DateTime d2)
                        {
                            if (d2 != DateTime.MinValue && d != d2)
                            {
                                return true;
                            }
                            continue;
                        }
                        else if (actual is DateTimeOffset dto && filterValue is DateTimeOffset dto2)
                        {
                            if (dto2 != DateTimeOffset.MinValue && dto != dto2)
                            {
                                return true;
                            }
                            continue;
                        }
                        else if (actual is Guid q && filterValue is Guid q2)
                        {
                            if (q2 != Guid.Empty && q != q2)
                            {
                                return true;
                            }
                            continue;
                        }
                    }
                    if (actual != null)
                    {
                        if (actual is not string)
                        {
                            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(actual.GetType()))
                            {
                                foreach (var e1 in (System.Collections.IEnumerable)actual)
                                {
                                    if (FilterBy(e1, filterValue))
                                    {
                                        return true;
                                    }
                                }
                                continue;
                            }
                            else if (actual.GetType().IsClass)
                            {
                                if (FilterBy(actual, filterValue))
                                {
                                    return true;
                                }
                                continue;
                            }
                        }
                    }
                    if (filterValue != null && Convert.ToString(filterValue) != Convert.ToString(actual))
                    {
                        if (actual != null)
                        {
                            if (actual.GetType().IsEnum)
                            {
                                actual = Convert.ToInt64(actual);
                            }
                            if (actual is bool b)
                            {
                                int val = b ? 1 : 0;
                                if (!val.Equals(filterValue))
                                {
                                    return true;
                                }
                            }
                            else if (actual is Guid)
                            {
                                if ((Guid)filterValue != Guid.Empty && !actual.Equals(filterValue))
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                switch (it.Value.FilterType)
                                {
                                    case FilterType.Exact:
                                        if (filterValue != null && !actual.Equals(filterValue))
                                        {
                                            return true;
                                        }
                                        break;
                                    case FilterType.Equals:
                                        if (filterValue != null && !actual.Equals(filterValue))
                                        {
                                            return true;
                                        }
                                        break;
                                    case FilterType.Greater:
                                        //TODO: And<T>(q => GXSql.Greater(it.Value.Target, actual));
                                        break;
                                    case FilterType.Less:
                                        //TODO: And<T>(q => GXSql.Less(it.Value.Target, actual));
                                        break;
                                    case FilterType.GreaterOrEqual:
                                        //TODO: And<T>(q => GXSql.GreaterOrEqual(it.Value.Target, actual));
                                        break;
                                    case FilterType.LessOrEqual:
                                        //TODO: And<T>(q => GXSql.LessOrEqual(it.Value.Target, actual));
                                        break;
                                    case FilterType.StartsWith:
                                        if (filterValue != null && !(Convert.ToString(actual) ?? string.Empty).StartsWith((Convert.ToString(filterValue) ?? string.Empty), StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            return true;
                                        }
                                        break;
                                    case FilterType.EndsWith:
                                        if (filterValue != null && !(Convert.ToString(actual) ?? string.Empty).ToLower().EndsWith((Convert.ToString(filterValue) ?? string.Empty).ToLower()))
                                        {
                                            return true;
                                        }
                                        break;
                                    case FilterType.Contains:
                                        if (filterValue != null && !(Convert.ToString(actual) ?? string.Empty).Contains((Convert.ToString(filterValue) ?? string.Empty), StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            return true;
                                        }
                                        break;
                                    case FilterType.Null:
                                        if (filterValue != null && actual != null)
                                        {
                                            return true;
                                        }
                                        break;
                                    case FilterType.NotNull:
                                        if (filterValue != null && actual == null)
                                        {
                                            return true;
                                        }
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException(nameof(filter), "The filter type is not supported.");
                                }
                            }
                        }
                    }
                    else if (it.Value.FilterType == FilterType.Null)
                    {
                        if (filterValue != null && actual != null)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static void OrderByInternal<T>(List<T> list, Type type, string[] orderBy, int index, bool descending)
        {
            PropertyInfo? prop = type.GetProperty(orderBy[index]);
            if (prop != null)
            {
                if (orderBy.Length == 1 + index)
                {
                    if (descending)
                    {
                        list = [.. list.OrderByDescending(o => prop.GetValue(o, null))];
                    }
                    else
                    {
                        if (index == 0)
                        {
                            list = [.. list.OrderBy(o => prop.GetValue(o, null))];
                        }
                        else
                        {
                            list = [.. list.OrderBy(o => prop.GetValue(o, null))];
                        }
                    }
                }
                else
                {
                    OrderByInternal(list, prop.PropertyType, orderBy, 1 + index, descending);
                }
            }
        }

        /// <summary>
        /// GXComparer is used to sort classes using class properties.
        /// </summary>
        class GXComparer : IComparer<object?>
        {
            private readonly bool _descending;
            private readonly List<PropertyInfo>? _props = [];
            readonly Dictionary<string, Type>? _enumTypes;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="enumTypes">Property enum types.</param>
            /// <param name="descending">Descending</param>
            /// <param name="orderBy">Dot separated order by name.</param>
            public GXComparer(Dictionary<string, Type>? enumTypes, bool descending, string orderBy)
            {
                _enumTypes = enumTypes;
                _descending = descending;
                Type type = typeof(GXObject);
                string[] list = orderBy.Split('.');
                foreach (var it in list)
                {
                    PropertyInfo pi = type.GetProperty(it) ?? throw new ArgumentException($"Unknown sort property: {it}.", nameof(orderBy));
                    type = pi.PropertyType;
                    _props.Add(pi);
                }
            }

            /// <inheritdoc/>
            public int Compare(object? obj1, object? obj2)
            {
                string? propName = null;
                object? value1 = obj1, value2 = obj2;
                Comparer comparer = new(Thread.CurrentThread.CurrentCulture);
                if (_props != null)
                {
                    foreach (var it in _props)
                    {
                        value1 = it.GetValue(value1, null);
                        value2 = it.GetValue(value2, null);
                        propName = it.Name;
                    }
                }
                if (value1 != null && value2 != null && _enumTypes != null &&
                    propName != null && _enumTypes.TryGetValue(propName, out Type? type))
                {
                    value1 = Enum.GetName(type, value1);
                    value2 = Enum.GetName(type, value2);
                }
                if (value1 == null || value2 == null)
                {

                }
                if (_descending)
                {
                    return comparer.Compare(value2, value1);
                }
                return comparer.Compare(value1, value2);
            }
        }

        /// <summary>
        /// Order by.
        /// </summary>
        /// <param name="objects">Ordered objects</param>
        /// <param name="enumTypes">Property enum types.</param>
        /// <param name="orderBy">Order by.</param>
        /// <param name="descending">Descending.</param>
        public static List<T> OrderBy<T>(List<T> objects, string orderBy, bool descending = false, Dictionary<string, Type>? enumTypes = null)
        {
            var comparer = new GXComparer(enumTypes, descending, orderBy);
            return [.. objects.OrderBy(o => o, comparer)];
        }
    }

    static partial class ScopeHelpers
    {
        public static async Task EnsureAnyPolicyAsync(
            this IAuthorizationService auth,
            ClaimsPrincipal? user,
            params string[] policies)
        {
            if (user != null)
            {
                foreach (var policy in policies)
                {
                    if ((await auth.AuthorizeAsync(user, policy)).Succeeded)
                    {
                        return;
                    }
                }
            }
            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException("Authentication is required.");
            }
            throw new Gurux.DLMS.AMI.Shared.GXAmiForbiddenException(policies);
        }

        /// <summary>
        /// Validate that ID is not null or empty.
        /// This method is used to validate that ID is valid before using it in the database query.
        /// </summary>
        /// <param name="ids">List of IDs.</param>
        public static void ValidateIds(this IEnumerable<Guid> ids)
        {
            if (ids != null)
            {
                foreach (var it in ids)
                {
                    if (it == Guid.Empty)
                    {
                        throw new ArgumentException("Identifier must not be empty.", nameof(ids));
                    }
                }
            }
        }

        /// <summary>
        /// Validate that ID is not null or empty.
        /// This method is used to validate that ID is valid before using it in the database query.
        /// </summary>
        /// <param name="ids">List of IDs.</param>
        public static void ValidateIds(this IEnumerable<string?>? ids)
        {
            if (ids != null)
            {
                foreach (var it in ids)
                {
                    if (it == null || it == string.Empty)
                    {
                        throw new ArgumentException("Identifier must not be empty.", nameof(ids));
                    }
                }
            }
        }

        /// <summary>
        /// This method is used to check can user access any of the given policies.
        /// It is used in cases where user can have multiple policies and any of them is enough to access the resource.
        /// </summary>
        /// <param name="auth">IAuthorizationService</param>
        /// <param name="user">User</param>
        /// <param name="policies">Policies</param>
        /// <returns>True if the user can access any of the given policies, otherwise false.</returns>
        public static async Task<bool> CanAccessPolicyAsync(
                    this IAuthorizationService auth,
                    ClaimsPrincipal user,
                    params string[] policies)
        {
            foreach (var policy in policies)
            {
                if ((await auth.AuthorizeAsync(user, policy)).Succeeded)
                {
                    return true;
                }
            }
            return false;
        }

        internal static async Task UpdateUserRolesAsync(this UserManager<ApplicationUser> userManager,
            ApplicationUser user,
            IEnumerable<string> newRoles)
        {
            var currentRoles = await userManager.GetRolesAsync(user);

            var rolesToAdd = newRoles.Except(currentRoles);
            var rolesToRemove = currentRoles.Except(newRoles);

            if (rolesToRemove.Any())
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    throw new Exception("Failed to remove roles");
                }
            }

            if (rolesToAdd.Any())
            {
                var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    throw new Exception("Failed to add roles");
                }
            }
        }
    }
}
