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

using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Http;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Coordinates an appearance image with its owning resource's Save action.</summary>
public sealed class GXProfilePictureEdit
{
    private Guid? originalId;
    private readonly string? ownerId;

    /// <summary>Captures the initially saved picture reference, without loading an image list.</summary>
    public GXProfilePictureEdit(Guid? id, string? ownerId = null)
    {
        originalId = id == Guid.Empty ? null : id;
        this.ownerId = ownerId;
    }

    /// <summary>Saves a new appearance before its reference, then deletes the replaced image after unlinking.</summary>
    public async Task SaveAsync(HttpClient http, GXAppearance? pending, Action<GXAppearance?> assign,
        Func<Task> saveOwner, Action<Exception> reportCleanupError)
    {
        Guid? added = null;
        var previous = originalId;
        var savedPicture = pending;
        try
        {
            if (pending?.Id == Guid.Empty)
            {
                var image = new GXAppearance
                {
                    Name = "profile-picture-" + Guid.NewGuid().ToString("N"),
                    ResourceType = (byte)ResourceType.Image,
                    Value = pending.Value,
                    OriginalFileName = pending.OriginalFileName,
                    Enabled = true,
                    Preload = false,
                    AccessVisibility = ResourceVisibility.Private,
                    Creator = string.IsNullOrEmpty(ownerId) ? null : new Gurux.DLMS.AMI.Shared.DTOs.Authentication.GXUser { Id = ownerId }
                };
                var response = await http.PostAsJson<UpdateAppearanceResponse>("api/Appearance/Add",
                    new UpdateAppearance { Appearances = [image] });
                var id = response.Ids?.SingleOrDefault() ?? Guid.Empty;
                if (id == Guid.Empty)
                {
                    throw new InvalidOperationException(Properties.Resources.InvalidTarget);
                }
                added = id;
                savedPicture = new GXAppearance { Id = added.Value };
            }
            assign(savedPicture);
            await saveOwner();
        }
        catch
        {
            assign(pending);
            if (added is Guid id)
            {
                try
                { await DeleteAsync(http, id); }
                catch (Exception cleanup) { reportCleanupError(cleanup); }
            }
            throw;
        }
        originalId = savedPicture?.Id;
        if (previous is Guid oldId && oldId != originalId)
        {
            try
            {
                var previousImage = await http.GetAsJsonAsync<GetAppearanceResponse>($"api/Appearance?type={(int)ResourceType.Image}&id={oldId}");
                if (previousImage.Item?.AccessVisibility is ResourceVisibility.Private or ResourceVisibility.Inherit)
                {
                    await DeleteAsync(http, oldId);
                }
            }
            catch (Exception cleanup) { reportCleanupError(cleanup); }
        }
    }

    private static Task<RemoveAppearanceResponse> DeleteAsync(HttpClient http, Guid id)
        => http.PostAsJson<RemoveAppearanceResponse>("api/Appearance/Delete", new RemoveAppearance { Ids = [id], Delete = true });
}
