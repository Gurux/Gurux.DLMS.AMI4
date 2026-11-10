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

using System.Security.Claims;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared.KeyManagement;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using DeviceSettings = Gurux.DLMS.AMI.Shared.DTOs.Device.GXDLMSSettings;

namespace Gurux.DLMS.AMI.Server.Services.KeyManagement;

/// <summary>Populates only the device-setting secrets needed for the authorized transport.</summary>
public static class GXDeviceSettingsKeys
{
    /// <summary>Whether the current caller can receive operational key material.</summary>
    public static bool CanUse(ClaimsPrincipal principal) => principal.Identity?.IsAuthenticated == true &&
        (principal.HasClaim("Permission", GXKeyPermissions.Use) || principal.IsInRole(GXRoles.Admin) || principal.HasClaim("Permission", GXRoles.Admin));

    /// <summary>Produces a response snapshot; never writes operational secrets to stored device settings.</summary>
    public static async Task PopulateAsync(ClaimsPrincipal principal, GXDevice device, IEnumerable<GXKeyManagement> managers,
        bool notification, Func<GXKeyManagementKey, GXKeyPurpose, CancellationToken, Task<byte[]>> acquire, CancellationToken token = default, byte[]? systemTitle = null)
    {
        var settings = JsonSerializer.Deserialize<DeviceSettings>(string.IsNullOrWhiteSpace(device.Settings) ? device.Template?.Settings ?? "null" : device.Settings) ?? new();
        settings.Password = settings.BlockCipherKey = settings.AuthenticationKey = settings.BroadcastKey = null;
        device.Settings = JsonSerializer.Serialize(settings);
        if (!CanUse(principal)) {
            return;
        }
        var contexts = managers.Where(m => m.Removed == null).ToArray();
        byte[]? title = systemTitle ?? settings.DeviceSystemTitle;
        if (title is { Length: > 0 })
        {
            var exact = contexts.Where(m => m.SystemTitle != null && m.SystemTitle.SequenceEqual(title)).ToArray();
            contexts = exact.Length > 0 ? exact : contexts.Where(m => m.SystemTitle is not { Length: > 0 }).ToArray();
            if (notification) {
                settings.DeviceSystemTitle = title.ToArray();
            }
        }
        var keys = contexts.SelectMany(m => m.Keys ?? []).Where(k => k.Removed == null && k.State == Gurux.DLMS.AMI.Shared.Enums.KeyState.Active).ToArray();
        GXKeyManagementKey? Select(KeyManagementType role, bool required)
        {
            var matches = keys.Where(k => k.KeyType == role).ToArray();
            if (matches.Length > 1 || required && matches.Length == 0)
                {
                throw new InvalidOperationException("Exactly one device key is required for role " + role + ".");
            }
            return matches.SingleOrDefault();
        }
        async Task<byte[]?> Get(KeyManagementType role, GXKeyPurpose purpose, bool required)
        {
            var key = Select(role, required);
            if (key == null) {
                return null;
            }
            var bytes = await acquire(key, purpose, token);
            if (bytes.Length == 0) {
                throw new InvalidOperationException("Device key material is empty.");
            }
            return bytes.ToArray();
        }
        if (notification)
        {
            settings.AuthenticationKey = await Get(KeyManagementType.Authentication, GXKeyPurpose.MacVerify, false);
            if (Select(KeyManagementType.Broadcast, false) != null)
                {
                settings.BroadcastKey = await Get(KeyManagementType.Broadcast, GXKeyPurpose.Decrypt, true);
            }
            else {
                settings.BlockCipherKey = await Get(KeyManagementType.BlockCipher, GXKeyPurpose.Decrypt, false);
            }
        }
        else
        {
            var authentication = (Gurux.DLMS.Enums.Authentication)settings.Authentication;
            if (authentication == Gurux.DLMS.Enums.Authentication.Low)
                {
                settings.Password = await Get(KeyManagementType.LLSPassword, GXKeyPurpose.Password, true);
            }
            else if (authentication is not (Gurux.DLMS.Enums.Authentication.None or Gurux.DLMS.Enums.Authentication.HighGMAC or Gurux.DLMS.Enums.Authentication.HighECDSA))
                {
                settings.Password = await Get(KeyManagementType.HLSPassword, GXKeyPurpose.Password, true);
            }
            if (settings.Security != 0 || authentication == Gurux.DLMS.Enums.Authentication.HighGMAC)
            {
                bool gmac = settings.Security is 0 or (byte)Gurux.DLMS.Enums.Security.Authentication;
                settings.BlockCipherKey = await Get(KeyManagementType.BlockCipher, gmac ? GXKeyPurpose.MacGenerate : GXKeyPurpose.Encrypt, true);
                settings.AuthenticationKey = await Get(KeyManagementType.Authentication, GXKeyPurpose.MacGenerate, true);
            }
        }
        device.Settings = JsonSerializer.Serialize(settings);
    }
}
