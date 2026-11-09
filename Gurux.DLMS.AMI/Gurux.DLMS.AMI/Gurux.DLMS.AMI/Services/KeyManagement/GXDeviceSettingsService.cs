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
using Gurux.DLMS.AMI.Client.Shared.KeyManagement;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Server.Services.KeyManagement;

/// <summary>Releases operational settings through Device responses after server-side authorization.</summary>
public sealed class GXDeviceSettingsService(IDeviceRepository devices, IKeyManagementRepository keys, GXKeyAcquisitionService acquisition)
{
    /// <summary>Reads the caller-visible meter and fills secrets needed for its reading association.</summary>
    public async Task<GXDevice> ReadAsync(ClaimsPrincipal principal, Guid id, bool notification, CancellationToken token, byte[]? systemTitle = null)
    {
        var device = await devices.ReadAsync(id, null, token);
        var managers = GXDeviceSettingsKeys.CanUse(principal)
            ? await keys.ListAsync(new ListKeyManagements { Filter = new GXKeyManagement { Device = new GXDevice { Id = id } }, Select = ["KeyManagementKey"] }, null, token)
            : [];
        await GXDeviceSettingsKeys.PopulateAsync(principal, device, managers, notification, async (key, purpose, ct) =>
        {
            var result = await acquisition.AcquireAsync(principal, new GXAcquireKeyRequest { KeyId = key.Id, Purpose = purpose }, ct);
            if (result.Metadata.Id != key.Id || result.Metadata.KeyType != key.KeyType || result.Metadata.ConcurrencyStamp != key.ConcurrencyStamp)
                {
                throw new InvalidOperationException("Device key changed while preparing settings.");
            }
            // A meter association both sends and receives protected frames.
            if (!notification && purpose == GXKeyPurpose.Encrypt) {
                GXKeyPolicy.ValidateUse(result.Metadata, GXKeyPurpose.Decrypt, DateTimeOffset.UtcNow);
            }
            if (!notification && purpose == GXKeyPurpose.MacGenerate) {
                GXKeyPolicy.ValidateUse(result.Metadata, GXKeyPurpose.MacVerify, DateTimeOffset.UtcNow);
            }
            return result.Material;
        }, token, systemTitle);
        return device;
    }

    /// <summary>Resolves an exact notification system title, then applies normal device visibility and key authorization.</summary>
    public async Task<GXDevice> ReadBySystemTitleAsync(ClaimsPrincipal principal, string systemTitle, CancellationToken token)
    {
        if (systemTitle.Length != 16 || !systemTitle.All(Uri.IsHexDigit)) {
            throw new ArgumentException("System title must contain eight hexadecimal bytes.");
        }
        byte[] title = Convert.FromHexString(systemTitle);
        var managers = await keys.ListAsync(new ListKeyManagements { Filter = new GXKeyManagement { SystemTitle = title }, Select = ["Device"] }, null, token);
        var ids = managers.Where(m => m.Removed == null && m.SystemTitle != null && m.SystemTitle.SequenceEqual(title))
            .Select(m => m.Device?.Id).Where(id => id.HasValue && id.Value != Guid.Empty).Select(id => id!.Value).Distinct().ToArray();
        if (ids.Length != 1) {
            throw new InvalidOperationException("Exactly one accessible device must match the notification system title.");
        }
        return await ReadAsync(principal, ids[0], true, token, title);
    }
}
