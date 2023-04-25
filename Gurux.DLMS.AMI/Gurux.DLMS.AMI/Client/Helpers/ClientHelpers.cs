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

using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.Enums;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Gurux.DLMS.AMI.Shared.DIs;

namespace Gurux.DLMS.AMI.Client.Helpers
{
    public static class HttpHelpers
    {
        public static async Task<RET> PostAsJson<RET>(this HttpClient client, string requestUri, object value, CancellationToken cancellationToken = default)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            HttpResponseMessage response = await client.PostAsJsonAsync(requestUri, value, options, cancellationToken);
            ClientHelpers.ValidateStatusCode(response);
            var ret = await response.Content.ReadFromJsonAsync<RET>();
            if (ret == null)
            {
                throw new Exception("Invalid reply.");
            }
            return ret;
        }

        public static async Task PostAsJson(this HttpClient client, string requestUri, object value, CancellationToken cancellationToken = default)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            HttpResponseMessage response = await client.PostAsJsonAsync(requestUri, value, options, cancellationToken);
            ClientHelpers.ValidateStatusCode(response);
        }

        public static async Task<RET> GetAsJsonAsync<RET>(this HttpClient client, string requestUri, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await client.GetAsync(requestUri, cancellationToken);
            ClientHelpers.ValidateStatusCode(response);
            var ret = await response.Content.ReadFromJsonAsync<RET>();
            if (ret == null)
            {
                throw new Exception("Invalid reply.");
            }
            return ret;
        }

    }

    public static class ClientHelpers
    {
        /// <summary>
        /// Get user ID from claims.
        /// </summary>
        /// <param name="user"></param>
        /// <returns>User ID.</returns>
        public static string GetUserId(ClaimsPrincipal user)
        {
            string? id = GetUserId(user, true);
            if (id == null)
            {
                throw new UnauthorizedAccessException();
            }
            return id;
        }

        /// <summary>
        /// Get user ID from claims.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="throwException">Exception is thrown if user is unknown.</param>
        /// <returns>User ID.</returns>
        public static string? GetUserId(ClaimsPrincipal user, bool throwException)
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
                return null;
            }
            return id.Value;
        }

        public static int GetHashCode(this string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = (hash1 << 5) + hash1 ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = (hash2 << 5) + hash2 ^ str[i + 1];
                }
                return hash1 + hash2 * 1566083941;
            }
        }

        public static void ValidateStatusCode(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new Exception(response.ReasonPhrase);
                }
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new Exception(response.ReasonPhrase);
                }
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    string? error = response.Content.ReadAsStringAsync().Result;
                    if (string.IsNullOrEmpty(error))
                    {
                        error = response.ReasonPhrase;
                    }
                    GXMaintenanceException e1 = new GXMaintenanceException(error);
                    if (response.Headers.RetryAfter != null && response.Headers.RetryAfter.Delta.HasValue
                        && response.Headers.RetryAfter.Delta.Value.TotalSeconds != 0)
                    {
                        e1.RetryAfter = response.Headers.RetryAfter.Delta;
                        e1.EndTime = DateTime.Now + response.Headers.RetryAfter.Delta;
                    }
                    throw e1;
                }
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    string error = response.Content.ReadAsStringAsync().Result;
                    throw new Exception(error);
                }
                else
                {
                    Exception? ex = null;
                    try
                    {
                        ex = JsonSerializer.Deserialize<GXAmiException>(response.Content.ReadAsStringAsync().Result);
                    }
                    catch (Exception)
                    {
                        ex = null;
                    }
                    if (ex != null)
                    {
                        throw ex;
                    }
                    response.EnsureSuccessStatusCode();
                }
            }
            else if (response.StatusCode == HttpStatusCode.NoContent)
            {
                throw new Exception("No content is returned from the server.");
            }
        }

        //Green dot is shown if item is active.
        public static string GetActiveDot(bool? active)
        {
            if (active.HasValue && active.Value)
            {
                return "green-dot";
            }
            return "red-dot";
        }

        //Convert trace level from string to int.
        public static int? LevelToInt(object? value)
        {
            if (value is null)
            {
                return null;
            }
            if (value is string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    return null;
                }
                if (int.TryParse(str, out int ret))
                {
                    return ret;
                }
                if (Enum.TryParse(str, true, out TraceLevel level))
                {
                    return (int)level;
                }
            }
            throw new ArgumentException("Invalid level value " + value);
        }

        //Get trace level as a string.
        public static string LevelToString(int? value)
        {
            if (value > -1 && value < 5)
            {
                return ((TraceLevel)value).ToString();
            }
            return Convert.ToString(value);
        }

        static IEnumerable<T> SortEnumByName<T>()
        {
            return from e in Enum.GetValues(typeof(T)).Cast<T>()
                   let nm = e.ToString()
                   orderby nm
                   select e;
        }

        /// <summary>
        /// Returns collection of notifications that can be ignored.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<TargetType> GetNotifications()
        {
            List<TargetType> items = new List<TargetType>();
            foreach (var it in SortEnumByName<TargetType>())
            {
                if (it != TargetType.None &&
                    it != TargetType.Cron &&
                    it != TargetType.Role)
                {
                    items.Add(it);
                }
            }
            return items;
        }
        /// <summary>
        /// Clone the object.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="value">Object to clone.</param>
        /// <returns>Cloned value.</returns>
        public static T Clone<T>(object value)
        {
            var serialized = JsonSerializer.Serialize(value);
            var ret = JsonSerializer.Deserialize<T>(serialized);
            if (ret == null)
            {
                throw new Exception("Clone failed.");
            }
            return ret;
        }

        public static void Copy(object source, object target)
        {
            if (source.GetType() != target.GetType())
            {
                throw new Exception("Copy failed.");
            }
            foreach (var it in source.GetType().GetProperties())
            {
                object? value = it.GetValue(source);
                it.SetValue(target, value);
            }
        }

        public static string GetParentUrl(string url)
        {
            return url.Replace("/Edit/", "/").Replace("/Add/", "/").Replace("/Add", "");
        }

        /// <summary>
        /// Navigate to the given url and save the current url.
        /// </summary>
        /// <param name="navigationManager">Navigation manager.</param>
        /// <param name="notifier">Notifier.</param>
        /// <param name="url">Url.</param>
        public static void NavigateTo(this NavigationManager navigationManager, IGXNotifier notifier, string url)
        {
            notifier.ChangePage(navigationManager.Uri);
            navigationManager.NavigateTo(url);
        }

        /// <summary>
        /// Navigate to the previous url.
        /// </summary>
        /// <param name="navigationManager">Navigation manager.</param>
        /// <param name="notifier">Notifier.</param>
        public static void NavigateToLastPage(this NavigationManager navigationManager, IGXNotifier notifier)
        {
            if (notifier.LastUrl != null)
            {
                navigationManager.NavigateTo(notifier.LastUrl);
            }
            else
            {
                navigationManager.NavigateTo(GetParentUrl(navigationManager.Uri));
            }
        }

        /// <summary>
        /// Convert string to CRUD action.
        /// </summary>
        /// <param name="value">String.</param>
        /// <returns>CRUD action.</returns>
        public static CrudAction GetAction(string? value)
        {
            CrudAction ret;
            if (string.Compare(value, "Add", true) == 0)
            {
                ret = CrudAction.Create;
            }
            else if (string.Compare(value, "Edit", true) == 0)
            {
                ret = CrudAction.Update;
            }
            else if (string.Compare(value, "Remove", true) == 0)
            {
                ret = CrudAction.Delete;
            }
            else
            {
                throw new ArgumentException(Properties.Resources.InvalidTarget);
            }
            return ret;
        }
    }
}
