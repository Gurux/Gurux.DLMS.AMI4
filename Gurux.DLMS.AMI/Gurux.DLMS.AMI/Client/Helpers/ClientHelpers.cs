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
using Microsoft.AspNetCore.Components;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.ManufacturerSettings;
using Gurux.DLMS.Objects;
using System.Xml.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Components;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;

namespace Gurux.DLMS.AMI.Client.Helpers
{
    public static class ClientHelpers
    {
        /// <summary>
        /// Add distinct items to the list.
        /// </summary>
        /// <param name="self">List where items are added.</param>
        /// <param name="items">Added items.</param>
        public static void AddDistinct2(this System.Collections.IList self, System.Collections.IEnumerable items)
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
                    string? error = response.Content.ReadAsStringAsync().Result;
                    if (string.IsNullOrEmpty(error))
                    {
                        error = response.ReasonPhrase;
                    }
                    throw new GXAmiNotFoundException(error);
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
        /// <param name="lowercase">Are named returned as lowercase.</param>
        /// <returns></returns>
        public static IEnumerable<string> GetNotifications(bool lowercase)
        {
            List<string> items = new List<string>();
            foreach (var it in typeof(TargetType).GetFields())
            {
                if (it.Name != TargetType.Cron &&
                    it.Name != TargetType.Role)
                {
                    if (lowercase)
                    {
                        items.Add(it.Name.ToLower());
                    }
                    else
                    {
                        items.Add(it.Name);
                    }
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
        public static T Clone<T>(T value)
        {
            var serialized = JsonSerializer.Serialize(value);
            var ret = JsonSerializer.Deserialize<T>(serialized);
            if (ret == null)
            {
                throw new Exception("Clone failed.");
            }
            return ret;
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
            int index = url.ToLower().IndexOf("/config/");
            if (index != -1)
            {
                return url.Substring(0, index + 7);
            }
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

        /// <summary>
        /// Convert GXDLMSDirector device file to Gurux.DLMS.AMI device templates.
        /// </summary>
        /// <param name="xml">XML string.</param>
        public static List<GXDeviceTemplate> ConvertToTemplates(ILogger? logger, string xml)
        {
            List<GXDeviceTemplate> templates = new List<GXDeviceTemplate>();
            GXDLMSDevice[] devices;
            using (var tr = new StringReader(xml))
            {
                List<Type> types = new List<Type>(Gurux.DLMS.GXDLMSClient.GetObjectTypes());
                types.Add(typeof(GXDLMSAttributeSettings));
                types.Add(typeof(GXDLMSAttribute));
                XmlSerializer serializer = new XmlSerializer(typeof(GXDLMSDevice[]), null, types.ToArray(), null, "Gurux1");
                devices = (GXDLMSDevice[])serializer.Deserialize(tr);
            }
            GXDeviceTemplate m = new GXDeviceTemplate();
            foreach (GXDLMSDevice it in devices)
            {
                int AssociationViewVersion = 1;
                GXDeviceTemplate t = new GXDeviceTemplate();
                Copy(t, it);
                List<GXObjectTemplate> list = new List<GXObjectTemplate>();
                if (it.Objects.Count == 0)
                {
                    throw new Exception("There are no objects. Read the association view first.");
                }

                if (it.UseLogicalNameReferencing)
                {
                    GXDLMSObjectCollection objs = it.Objects.GetObjects(Enums.ObjectType.AssociationLogicalName);
                    if (objs.Any())
                    {
                        GXDLMSAssociationLogicalName? ln = objs[0] as GXDLMSAssociationLogicalName;
                        if (ln != null)
                        {
                            AssociationViewVersion = ln.Version;
                        }
                    }
                }

                foreach (GXDLMSObject value in it.Objects)
                {
                    string[] names = ((IGXDLMSBase)value).GetNames();
                    GXObjectTemplate obj = new GXObjectTemplate(null)
                    {
                        LogicalName = value.LogicalName,
                        ObjectType = (int)value.ObjectType,
                        Name = value.Description,
                        Version = value.Version,
                        ShortName = value.ShortName,
                        Attributes = new List<GXAttributeTemplate>()
                    };
                    if (obj.Name.Length > 256)
                    {
                        logger?.LogError(string.Format("Name '{0}' is too long,", obj.Name));
                        obj.Name = obj.Name.Substring(0, 256);
                    }
                    list.Add(obj);
                    for (int pos = 2; pos <= ((IGXDLMSBase)value).GetAttributeCount(); ++pos)
                    {
                        GXAttributeTemplate a = new GXAttributeTemplate(null);
                        a.Name = names[pos - 1];
                        a.Index = pos;
                        a.Weight = pos - 1;
                        if (AssociationViewVersion < 3)
                        {
                            a.AccessLevel = (int)value.GetAccess(pos);
                        }
                        else
                        {
                            a.AccessLevel = (int)value.GetAccess3(pos);
                        }
                        a.DataType = (int)((IGXDLMSBase)value).GetDataType(pos);
                        a.UIDataType = (int)((GXDLMSObject)value).GetUIDataType(pos);
                        if (value.GetStatic(pos))
                        {
                            a.ExpirationTime = DateTime.MaxValue;
                        }
                        if (value is GXDLMSProfileGeneric)
                        {
                            //Capture objects.
                            if (pos == 3 ||
                                //Capture Period
                                pos == 4 ||
                                //Sort Method
                                pos == 5 ||
                                //Sort Object
                                pos == 6 ||
                                //Profile Entries
                                pos == 8)
                            {
                                a.ExpirationTime = DateTime.MaxValue;
                            }
                        }
                        if (value is GXDLMSAssociationLogicalName)
                        {
                            //Object List.
                            if (pos == 2 ||
                                //Associated partners Id
                                pos == 3 ||
                                //Application Context Name.
                                pos == 4 ||
                                // xDLMS Context Info.
                                pos == 5 ||
                                //Authentication Mechanism Name.
                                pos == 6 ||
                                //Secret.
                                pos == 7)
                            {
                                a.ExpirationTime = DateTime.MaxValue;
                            }
                        }
                        if (a.DataType == (int)Gurux.DLMS.Enums.DataType.Enum)
                        {
                            //Add enum values as list items.
                            try
                            {
                                object tmp = value.GetValues()[pos - 1];
                                if (tmp != null)
                                {
                                    foreach (var v in Enum.GetValues(tmp.GetType()))
                                    {
                                        GXAttributeListItem li = new()
                                        {
                                            UIValue = v.ToString(),
                                            Value = Convert.ToInt32(v)
                                        };
                                        a.ListItems.Add(li);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //It's OK if this fails.
                                Console.WriteLine(ex.Message);
                            }
                        }
                        //Profile generic capture objects are not read as default.
                        if (value is GXDLMSProfileGeneric && pos == 3)
                        {
                            a.ExpirationTime = DateTime.MaxValue;
                        }
                        //Scaler and unit are read only once.
                        if (value is GXDLMSRegister && pos == 3)
                        {
                            a.ExpirationTime = DateTime.MaxValue;
                        }
                        obj.Attributes.Add(a);
                    }
                    string sb = "";
                    if (AssociationViewVersion < 3)
                    {
                        for (int pos = 1; pos <= ((IGXDLMSBase)value).GetMethodCount(); ++pos)
                        {
                            sb += ((int)value.GetMethodAccess(pos)).ToString();
                        }
                    }
                    else
                    {
                        sb = "0x";
                        for (int pos = 1; pos <= ((IGXDLMSBase)value).GetMethodCount(); ++pos)
                        {
                            sb += ((int)value.GetMethodAccess3(pos)).ToString("X2");
                        }
                    }
                    obj.ActionAccessLevels = sb;
                    t.Objects = list;
                }
                templates.Add(t);
            }
            return templates;
        }

        internal static string GetLastChecked(DateTimeOffset? offset)
        {
            if (offset == null)
            {
                return Properties.Resources.Never;
            }
            TimeSpan value = DateTime.Now - offset.Value;
            string? str = "";
            if ((int)value.TotalDays != 0)
            {
                str = (int)value.TotalDays + " " + Properties.Resources.Days;
            }
            if (value.Hours != 0)
            {
                if (str != "")
                {
                    str += " ";
                }
                str += value.Hours + " " + Properties.Resources.Hours;
            }
            if (value.Minutes != 0)
            {
                if (str != "")
                {
                    str += " ";
                }
                str += value.Minutes + " " + Properties.Resources.Minutes;
            }
            if (value.Seconds != 0)
            {
                if (str != "")
                {
                    str += " ";
                }
                str += value.Seconds + " " + Properties.Resources.Seconds;
            }
            else if (str == "")
            {
                //Append seconds always.
                str = value.Seconds + " " + Properties.Resources.Seconds;
            }
            return str;
        }

        /// <summary>
        /// Copy meter settings.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        public static void Copy(GXDeviceTemplate target, GXDLMSDevice source)
        {
            target.WaitTime = source.WaitTime;
            target.ResendCount = source.ResendCount;
            target.Type = source.Name;
            target.MediaType = source.MediaType;
            target.MediaSettings = source.MediaSettings;
            var settings = new Gurux.DLMS.AMI.Shared.DTOs.Device.GXDLMSSettings();
            settings.MaximumBaudRate = source.MaximumBaudRate;
            settings.Authentication = (byte)source.Authentication;
            settings.AuthenticationName = source.AuthenticationName;
            settings.Standard = (byte)source.Standard;
            // Password is not saved as a default.
            // User can change it if needed.
            // settings.Password = source.Password;
            // settings.HexPassword = source.HexPassword;
            settings.Security = (byte)source.Security;
            settings.ClientSystemTitle = source.SystemTitle;
            settings.DeviceSystemTitle = source.ServerSystemTitle;
            settings.DedicatedKey = source.DedicatedKey;
            settings.PreEstablished = source.PreEstablished;
            settings.BlockCipherKey = source.BlockCipherKey;
            settings.AuthenticationKey = source.AuthenticationKey;
            settings.InvocationCounter = source.InvocationCounter;
            settings.FrameCounter = source.FrameCounter;
            settings.Challenge = source.Challenge;
            settings.Profiles.Add(new GXCommunicationProfile()
            {
                InterfaceType = (int)source.InterfaceType,
                ClientAddress = source.ClientAddress,
                PhysicalAddress = source.PhysicalAddress,
                LogicalAddress = source.LogicalAddress,
            });
            settings.UtcTimeZone = source.UtcTimeZone;
            settings.UseRemoteSerial = source.UseRemoteSerial;
            settings.MaxInfoTX = source.MaxInfoTX;
            settings.MaxInfoRX = source.MaxInfoRX;
            settings.WindowSizeTX = source.WindowSizeTX;
            settings.WindowSizeRX = source.WindowSizeRX;
            settings.PduSize = source.PduSize;
            settings.UserId = source.UserId;
            settings.NetworkId = source.NetworkId;
            settings.InactivityTimeout = source.InactivityTimeout;
            settings.ServiceClass = (byte)source.ServiceClass;
            settings.Priority = (byte)source.Priority;
            settings.ServerAddressSize = source.ServerAddressSize;
            settings.Conformance = source.Conformance;
            settings.Manufacturer = source.Manufacturer;
            settings.HDLCAddressing = (int)source.HDLCAddressing;
            settings.UseLogicalNameReferencing = source.UseLogicalNameReferencing;
            settings.UseProtectedRelease = source.UseProtectedRelease;
            target.Settings = JsonSerializer.Serialize(settings);
        }

        public static string GetKeyTypeDescription(KeyManagementType? value)
        {
            string str;
            switch (value.GetValueOrDefault(KeyManagementType.LLSPassword))
            {
                case KeyManagementType.LLSPassword:
                    str = Properties.Resources.KeyManagementLLSPassword;
                    break;
                case KeyManagementType.HLSPassword:
                    str = Properties.Resources.KeyManagementHLSPassword;
                    break;
                case KeyManagementType.BlockCipher:
                    str = Properties.Resources.KeyManagementUnicastKey;
                    break;
                case KeyManagementType.Authentication:
                    str = Properties.Resources.KeyManagementAuthenticationKey;
                    break;
                case KeyManagementType.Broadcast:
                    str = Properties.Resources.KeyManagementBroadcast;
                    break;
                case KeyManagementType.MasterKey:
                    str = Properties.Resources.KeyManagementMasterKey;
                    break;
                case KeyManagementType.PrivateKey:
                    str = Properties.Resources.KeyManagementPrivateKey;
                    break;
                case KeyManagementType.PublicKey:
                    str = Properties.Resources.KeyManagementPublicKey;
                    break;
                default:
                    throw new Exception(Properties.Resources.InvalidTarget);
            }
            return str;
        }
    }
}
