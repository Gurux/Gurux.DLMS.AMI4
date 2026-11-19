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
    /// <summary>Identifier of the image linked by the last successfully saved owner.</summary>
    private Guid? _originalId;
    /// <summary>Optional user identifier assigned as creator when saving a private image.</summary>
    private readonly string? _ownerId;

    /// <summary>Captures the initially saved picture reference, without loading an image list.</summary>
    /// <param name="id">Identifier of the saved appearance image.</param>
    /// <param name="ownerId">Optional identifier of the user who owns newly uploaded private images.</param>
    public GXProfilePictureEdit(Guid? id, string? ownerId = null)
    {
        _originalId = id == Guid.Empty ? null : id;
        this._ownerId = ownerId;
    }

    /// <summary>Saves a new appearance before its reference, then deletes the replaced image after unlinking.</summary>
    /// <param name="http">Transport used to create, read and delete appearance images.</param>
    /// <param name="pending">Selected appearance, including a newly uploaded image with an empty identifier.</param>
    /// <param name="assign">Callback that updates the owner&apos;s selected appearance reference.</param>
    /// <param name="saveOwner">Callback that persists the owner after the appearance has been prepared.</param>
    /// <param name="reportCleanupError">Callback that reports cleanup failures without replacing the original save result.</param>
    /// <returns>A task that completes after saving the owner and attempting cleanup of replaced or orphaned images.</returns>
    public async Task SaveAsync(HttpClient http, GXAppearance? pending, Action<GXAppearance?> assign,
        Func<Task> saveOwner, Action<Exception> reportCleanupError)
    {
        Guid? added = null;
        var previous = _originalId;
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
                    Creator = string.IsNullOrEmpty(_ownerId) ? null : new Gurux.DLMS.AMI.Shared.DTOs.Authentication.GXUser { Id = _ownerId }
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
        _originalId = savedPicture?.Id;
        if (previous is Guid oldId && oldId != _originalId)
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

    /// <summary>Permanently removes an appearance that has been unlinked or created by a failed save.</summary>
    /// <param name="http">Transport used to create, read and delete appearance images.</param>
    /// <param name="id">Identifier of the saved appearance image.</param>
    /// <returns>The appearance deletion response from the API.</returns>
    private static Task<RemoveAppearanceResponse> DeleteAsync(HttpClient http, Guid id)
        => http.PostAsJson<RemoveAppearanceResponse>("api/Appearance/Delete", new RemoveAppearance { Ids = [id], Delete = true });
}
