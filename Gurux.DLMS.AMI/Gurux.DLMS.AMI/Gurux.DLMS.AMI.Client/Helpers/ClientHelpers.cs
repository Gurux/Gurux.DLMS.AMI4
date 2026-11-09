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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.ManufacturerSettings;
using Gurux.DLMS.Objects;
using Microsoft.AspNetCore.Components;
using System.Diagnostics;
using System.Drawing;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Client.Helpers
{
    public static class ClientHelpers
    {
        /// <summary>Adapts a 64-bit API total to Blazor's 32-bit virtualization limit, preserving the unknown-count sentinel.</summary>
        public static int ToVirtualizedCount(long count) => (int)Math.Clamp(count, -1L, int.MaxValue);

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
                return null;
            }
            return id.Value;
        }

        /// <summary>
        /// Get hash code to string.
        /// </summary>
        public static string GetHashCode(this string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            byte[] hashBytes = SHA256.HashData(bytes);
            //Convert to hex.
            StringBuilder sb = new(64);
            foreach (byte it in hashBytes)
            {
                sb.Append(it.ToString("x2"));
            }
            return sb.ToString();
        }

        //Green dot is shown if item is active.
        public static string GetActiveDot(bool? active)
        {
            if (active == true)
            {
                return "<div class=\"rounded-circle bg-success\" style=\"width: 16px; height: 16px;\"></div>";
            }
            return "<div class=\"rounded-circle bg-warning\" style=\"width: 16px; height: 16px;\"></div>";
        }


        //Get trace level as a string.
        public static string? LevelToString(int? value)
        {
            if (value > -1 && value < 5)
            {
                return ((TraceLevel)value).ToString();
            }
            return Convert.ToString(value);
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
            var ret = JsonSerializer.Deserialize<T>(serialized) ?? throw new Exception("Clone failed.");
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
            var ret = JsonSerializer.Deserialize<T>(serialized) ?? throw new Exception("Clone failed.");
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

        /// <summary>
        /// Current route path, without query parameters or fragment, for selecting a menu tab.
        /// </summary>
        public static string GetCurrentPagePath(this NavigationManager navigationManager, string? url = null)
        {
            if (string.IsNullOrEmpty(url))
            {
                return navigationManager.ToBaseRelativePath(new Uri(navigationManager.Uri).GetLeftPart(UriPartial.Path));
            }
            if (TryGetLocalUrl(navigationManager, url, out var target) &&
               target != navigationManager.Uri &&
               HttpUtility.ParseQueryString(new Uri(target).Query).GetValues("returnUrl") == null)
            {
                url = navigationManager.GetUriWithQueryParameters(target,
                    new Dictionary<string, object?> { ["returnUrl"] = new Uri(navigationManager.Uri).PathAndQuery + new Uri(navigationManager.Uri).Fragment });
            }
            return url;
        }

        /// <summary>
        /// Navigate to the given URL, carrying the current URL as returnUrl.
        /// </summary>
        /// <param name="navigationManager">Navigation manager.</param>
        /// <param name="url">Destination URL. An existing returnUrl is preserved.</param>
        public static void NavigateTo(this NavigationManager navigationManager, string url)
        {
            url = GetCurrentPagePath(navigationManager, url);
            navigationManager.NavigateTo(url);
        }

        /// <summary>
        /// Navigate to returnUrl in the current URL, or to the application base when it is absent or invalid.
        /// </summary>
        /// <param name="navigationManager">Navigation manager.</param>
        public static void NavigateToLastPage(this NavigationManager navigationManager)
        {
            var query = HttpUtility.ParseQueryString(new Uri(navigationManager.Uri).Query);
            var target = navigationManager.BaseUri;
            var values = query.GetValues("returnUrl");
            if (values is { Length: 1 } &&
                TryGetLocalUrl(navigationManager, values[0], out var returnUrl) &&
                returnUrl != navigationManager.Uri)
            {
                target = returnUrl;
            }
            GXEditorMenu.AcceptSavedNavigation();
            navigationManager.NavigateTo(target);
        }

        private static bool TryGetLocalUrl(NavigationManager navigationManager, string? url, out string target)
        {
            target = "";
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            try
            {
                var uri = navigationManager.ToAbsoluteUri(url);
                if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                {
                    return false;
                }
                // This also validates the application base path when AMI is hosted in a subdirectory.
                navigationManager.ToBaseRelativePath(uri.AbsoluteUri);
                target = uri.AbsoluteUri;
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Convert string to CRUD action.
        /// </summary>
        /// <param name="navigationManager">Navigation used to redirect invalid actions to the 404 page.</param>
        /// <param name="value">String.</param>
        /// <param name="def">Optional default action for an unrecognized value.</param>
        /// <returns>CRUD action, or None after redirecting to 404.</returns>
        public static CrudAction GetAction(NavigationManager navigationManager, string? value, CrudAction? def = null)
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
            else if (string.Compare(value, "Clone", true) == 0)
            {
                ret = CrudAction.Clone;
            }
            else
            {
                if (def == null)
                {
                    navigationManager.NavigateTo("404");
                    return CrudAction.None;
                }
                ret = def.Value;
            }
            return ret;
        }

        /// <summary>
        /// Convert GXDLMSDirector device file to Gurux.DLMS.AMI device templates.
        /// </summary>
        /// <param name="xml">XML string.</param>
        public static List<GXDeviceTemplate> ConvertToTemplates(ILogger? logger, string xml)
        {
            List<GXDeviceTemplate> templates = [];
            GXDLMSDevice[] devices;
            using (var tr = new StringReader(xml))
            {
                List<Type> types = [.. Gurux.DLMS.GXDLMSClient.GetObjectTypes()];
                types.Add(typeof(GXDLMSAttributeSettings));
                types.Add(typeof(GXDLMSAttribute));
                XmlSerializer serializer = new(typeof(GXDLMSDevice[]), null, [.. types], null, "Gurux1");
                devices = serializer.Deserialize(tr) as GXDLMSDevice[] ?? throw new InvalidOperationException("The device file contains no devices.");
            }

            _ = new GXDeviceTemplate();
            foreach (GXDLMSDevice it in (devices ?? []))
            {
                int AssociationViewVersion = 1;
                GXDeviceTemplate t = new();
                Copy(t, it);
                List<GXObjectTemplate> list = [];
                if (it.Objects.Count == 0)
                {
                    throw new Exception("There are no objects. Read the association view first.");
                }

                if (it.UseLogicalNameReferencing)
                {
                    GXDLMSObjectCollection objs = it.Objects.GetObjects(Enums.ObjectType.AssociationLogicalName);
                    if (objs.Count != 0)
                    {
                        if (objs[0] is GXDLMSAssociationLogicalName ln)
                        {
                            AssociationViewVersion = ln.Version;
                        }
                    }
                }

                foreach (GXDLMSObject value in it.Objects)
                {
                    string[] names = ((IGXDLMSBase)value).GetNames();
                    GXObjectTemplate obj = new(null)
                    {
                        LogicalName = value.LogicalName,
                        ObjectType = (int)value.ObjectType,
                        Name = value.Description,
                        Version = value.Version,
                        ShortName = value.ShortName,
                        Attributes = []
                    };
                    if (obj.Name.Length > 256)
                    {
                        logger?.LogError("Name '{Name}' is too long,", obj.Name);
                        obj.Name = obj.Name[..256];
                    }
                    list.Add(obj);
                    for (int pos = 2; pos <= ((IGXDLMSBase)value).GetAttributeCount(); ++pos)
                    {
                        GXAttributeTemplate a = new(null)
                        {
                            Name = names[pos - 1],
                            Index = pos,
                            Weight = pos - 1
                        };
                        if (AssociationViewVersion < 3)
                        {
                            a.AccessLevel = (int)value.GetAccess(pos);
                        }
                        else
                        {
                            a.AccessLevel = (int)value.GetAccess3(pos);
                        }
                        a.DataType = (int)((IGXDLMSBase)value).GetDataType(pos);
                        a.UIDataType = (int)value.GetUIDataType(pos);
                        if (value.GetStatic(pos))
                        {
                            a.ExpirationTime = DateTime.MaxValue;
                        }
                        if (value is GXDLMSProfileGeneric pg)
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
                                ValueEventArgs e = new(value, pos, 0, null);
                                object val = ((IGXDLMSBase)value).GetValue(null, e);
                                string? str;
                                if (val is byte[] barray)
                                {
                                    GXDLMSTranslator t2 = new()
                                    {
                                        Hex = false
                                    };
                                    t2.DataToXml(barray, out str);
                                }
                                else
                                {
                                    str = val.ToString();
                                }
                                a.DefaultValue = str;
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
                                        (a.ListItems ??= []).Add(li);
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
            var settings = new Gurux.DLMS.AMI.Shared.DTOs.Device.GXDLMSSettings
            {
                MaximumBaudRate = source.MaximumBaudRate,
                Authentication = (byte)source.Authentication,
                AuthenticationName = source.AuthenticationName,
                Standard = (byte)source.Standard,
                // Password is not saved as a default.
                // User can change it if needed.
                // settings.Password = source.Password;
                // settings.HexPassword = source.HexPassword;
                Security = (byte)source.Security,
                ClientSystemTitle = GXDLMSTranslator.HexToBytes(source.SystemTitle)
            };
            if (source.PreEstablished)
            {
                settings.DeviceSystemTitle = GXDLMSTranslator.HexToBytes(source.ServerSystemTitle);
            }
            settings.DedicatedKey = GXDLMSTranslator.HexToBytes(source.DedicatedKey);
            settings.PreEstablished = source.PreEstablished;
            settings.BlockCipherKey = GXDLMSTranslator.HexToBytes(source.BlockCipherKey);
            settings.AuthenticationKey = GXDLMSTranslator.HexToBytes(source.AuthenticationKey);
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
            settings.DateTimeSkips = (int)source.DateTimeSkips;
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
            string str = value.GetValueOrDefault(KeyManagementType.LLSPassword) switch
            {
                KeyManagementType.LLSPassword => Properties.Resources.KeyManagementLLSPassword,
                KeyManagementType.HLSPassword => Properties.Resources.KeyManagementHLSPassword,
                KeyManagementType.BlockCipher => Properties.Resources.KeyManagementUnicastKey,
                KeyManagementType.Authentication => Properties.Resources.KeyManagementAuthenticationKey,
                KeyManagementType.Broadcast => Properties.Resources.KeyManagementBroadcast,
                KeyManagementType.MasterKey => Properties.Resources.KeyManagementMasterKey,
                KeyManagementType.PrivateKey => Properties.Resources.KeyManagementPrivateKey,
                KeyManagementType.PublicKey => Properties.Resources.KeyManagementPublicKey,
                _ => throw new Exception(Properties.Resources.InvalidTarget),
            };
            return str;
        }

        /// <summary>
        ///Convert hex to RGB.
        /// </summary>
        /// <param name="value">Hex value</param>
        /// <returns>RGB value.</returns>
        public static string HexToRgb(string? value)
        {
            if (value == null)
            {
                return "0, 0, 0";
            }
            if (!value.StartsWith('#'))
            {
                var c = Color.FromName(value);
                return string.Format("{0}, {1}, {2}", c.R, c.G, c.B);
            }
            value = value.Replace("#", "");
            return Convert.ToInt32(value[..2], 16) + "," +
                Convert.ToInt32(value.Substring(2, 2), 16) + "," +
                Convert.ToInt32(value.Substring(4, 2), 16);
        }

        /// <summary>
        /// Convert rgb value to Hex.
        /// </summary>
        /// <param name="rgb">RGB value.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string RgbToHex(string? rgb)
        {
            if (rgb == null)
            {
                return "#0";
            }
            rgb = rgb.Replace("rgb(", "")
                     .Replace("RGB(", "")
                     .Replace(")", "")
                     .Trim();

            var parts = rgb.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != 3)
            {
                throw new ArgumentException("RGB string must contain exactly three values, e.g. \"255, 0, 0\".");
            }
            int r = int.Parse(parts[0]);
            int g = int.Parse(parts[1]);
            int b = int.Parse(parts[2]);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        /// <summary>
        /// Returns iconpack icons.
        /// </summary>
        public static List<(string, string)> GetIconPackIcons(string iconPack)
        {
            string[] list;
            if (iconPack == "Bootstrap")
            {
                list = Properties.Resources.BootstrapIcons.Split(Environment.NewLine);
                return [.. list.Select(n => (Name: "bi-" + n, Value: n))];
            }
            else if (iconPack == "FontAwesome")
            {
                list = Properties.Resources.FontawesomeIcons.Split(Environment.NewLine);
                return [.. list.Select(n => (Name: "fa-solid fa-" + n, Value: n))];
            }
            else
            {
                //Material icons
                list = Properties.Resources.MaterialIcons.Split(Environment.NewLine);
                return [.. list.Select(n => (Name: n, Value: n))];
            }
        }

        public static IEnumerable<string>? GetBadges(GXMenu? menu)
        {
            if (menu?.Links != null)
            {
                return menu.Links?.SelectMany(l =>
                {
                    if (!string.IsNullOrEmpty(l.Padges))
                    {
                        return l.Padges.Split(',');
                    }
                    return [];
                });
            }
            return [];
        }

        public static int GetBadgeCount(IEnumerable<GXUserStamp> updateStamps, string? padges)
        {
            int count = 0;
            if (!string.IsNullOrEmpty(padges))
            {
                padges.Split(',').ToList().ForEach(badge =>
                {
                    updateStamps.Where(w => w.TargetType == badge).ToList().ForEach(stamp =>
                    {
                        count += stamp.GetCount();
                    });
                });
            }
            return count;
        }

        public static TraceLevel GetBadgeTraceLevel(IEnumerable<GXUserStamp> updateStamps,
            string? padges)
        {
            TraceLevel level = TraceLevel.Off;
            if (!string.IsNullOrEmpty(padges))
            {
                padges.Split(',').ToList().ForEach(badge =>
                {
                    updateStamps.Where(w => w.TargetType == badge).ToList().ForEach(stamp =>
                    {
                        var tmp = stamp.GetTraceLevel();
                        if (tmp > level)
                        {
                            level = tmp;
                        }
                    });
                });
            }
            return level;
        }

        public static List<GXUserStamp> GetUserStamps(GXMenu? menu, List<GXUserStamp> updateStamps)
        {
            updateStamps.Clear();
            if (menu?.Links != null)
            {
                foreach (var link in menu.Links)
                {
                    if (!string.IsNullOrEmpty(link.Padges))
                    {
                        link.Padges.Split(',').ToList().ForEach(badge =>
                        {
                            updateStamps.Add(new GXUserStamp()
                            {
                                TargetType = badge
                            });
                        });
                    }
                }
            }
            return updateStamps;
        }
        public static bool UpdateUserStamps(IEnumerable<GXUserStamp> updatedStamps,
            IEnumerable<GXUserStamp> stamps)
        {
            bool updated = false;
            foreach (var it in updatedStamps)
            {
                foreach (var stamp in stamps)
                {
                    if (!string.IsNullOrEmpty(stamp.TargetType))
                    {
                        stamp.TargetType.Split(',').ToList().ForEach(badge =>
                        {
                            if (stamp.TargetType == it.TargetType)
                            {
                                it.CopyTo(stamp);
                                updated = true;
                            }
                        });
                    }
                }
            }
            return updated;
        }

        /// <summary>
        /// Convert the text to kebab case.
        /// </summary>
        /// <remarks>
        /// For example, "HelloWorld" will be converted to "hello-world".
        /// This is used to convert role name and scope name to kebab case for better readability.
        /// </remarks>
        public static string GetKebabCase(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }
            var result = new StringBuilder();
            if (char.IsUpper(text[0]))
            {
                result.Append(text[0].ToString().ToLower());
            }
            else
            {
                result.Append(text[0]);
            }

            for (int i = 1; i < text.Length; i++)
            {
                char current = text[i];

                if (char.IsUpper(current))
                {
                    result.Append('-');
                    result.Append(current.ToString().ToLower());
                }
                else
                {
                    result.Append(current);
                }
            }
            return result.ToString();
        }

        public static string? KebabToTitle(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }
            var parts = input.Split('-', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var word = parts[i].ToLower();
                parts[i] = char.ToUpper(word[0]) + word[1..];
            }
            return string.Join(" ", parts);
        }
    }


    public class CachedHttpClient(
        HttpClient http,
        ClientCacheService cache,
        ILogger<CachedHttpClient> logger)
    {
        private readonly HttpClient _http = http;
        private readonly ClientCacheService _cache = cache;
        private readonly ILogger<CachedHttpClient> _logger = logger;

        public async Task<T?> GetAsync<T>(
            string url,
            TimeSpan cacheTime = default)
        {
            if (cacheTime == default)
            {
                cacheTime = TimeSpan.FromMinutes(60);
            }
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Getting data from {Url} with cache time {CacheTime}", url, cacheTime);
            }

            var cached = await _cache.GetAsync<T>(url);
            if (cached != null)
            {
                return cached;
            }
            var result = await _http.GetAsJsonAsync<T>(url);

            if (result != null)
            {
                await _cache.SetAsync(
                    url,
                    result,
                    cacheTime);
            }
            return result;
        }

        /// <summary>
        /// Remove cache for the given url.
        /// </summary>
        /// <param name="url">Removed url.</param>
        public Task RemoveAsync(string url)
        {
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
            {
                _logger.LogInformation("Removing cache for {Url}", url);
            }
            _cache.RemoveAsync(url);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Clear cache.
        /// </summary>
        public Task ClearAsync()
        {
            _logger.LogInformation("Clearing all cache");
            _cache.ClearAsync();
            return Task.CompletedTask;
        }
    }

    public sealed class CacheItem<T>
    {
        public T? Value { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }

    public sealed class ClientCacheService
    {
        private readonly Dictionary<string, CacheItem<object>> _storage = [];

        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan lifetime)
        {
            _storage.Add(key, new CacheItem<object>
            {
                Value = value,
                ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime)
            });
            return Task.CompletedTask;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            if (_storage.TryGetValue(key, out CacheItem<object>? cacheItem))
            {
                if (cacheItem.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    await RemoveAsync(key);
                    return default;
                }
                return (T?)cacheItem.Value;
            }
            return default;
        }

        public Task RemoveAsync(string key)
        {
            _storage.Remove(key);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _storage.Clear();
            return Task.CompletedTask;
        }
    }
}
