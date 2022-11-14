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
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Client.Helpers
{
    public static class ClientHelpers
    {
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
    }
}
