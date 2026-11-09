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

using System.Linq.Expressions;
using System.Text;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Repository;

public partial class AppearanceRepository
{
    private async Task HydrateImageAsync(GXAppearance appearance, CancellationToken token)
    {
        if (appearance.StorageKey == null)
        {
            return;
        }
        var bytes = await _imageStorage.ReadAsync(appearance.StorageKey, token);
        appearance.Value = appearance.ContentType == "image/svg+xml" ? Encoding.UTF8.GetString(bytes) : Convert.ToBase64String(bytes);
    }

    private async Task PersistImageAsync(GXAppearance appearance, GXAppearance? old,
        Expression<Func<GXAppearance, object>>? columns, Func<Task> save, CancellationToken token)
    {
        // File references are host-owned, including on metadata-only and non-image writes.
        var input = appearance.Value;
        var originalName = GXImageStorage.OriginalName(appearance.OriginalFileName);
        appearance.StorageKey = old?.StorageKey;
        appearance.ContentType = old?.ContentType;
        if (columns != null && old != null)
        {
            var selected = new ImageColumnVisitor(); selected.Visit(columns);
            if (selected.IncludesImage && (old.ResourceType == (byte)ResourceType.Image || appearance.ResourceType == (byte)ResourceType.Image || old.StorageKey != null))
            {
                throw new ArgumentException("Image content and storage metadata require a full appearance update.");
            }
            await save();
            return;
        }

        string? created = null;
        bool committed = false;
        var stamp = appearance.ConcurrencyStamp;
        var updated = appearance.Updated;
        try
        {
            appearance.StorageKey = null;
            appearance.ContentType = null;
            appearance.OriginalFileName = null;
            if (appearance.ResourceType == (byte)ResourceType.Image)
            {
                var decoded = _imageStorage.Decode(input, originalName != null);
                if (decoded is { } image)
                {
                    appearance.OriginalFileName = originalName;
                    appearance.ContentType = image.ContentType;
                    if (old?.StorageKey != null && await ImageMatchesAsync(old.StorageKey, image.Bytes, token))
                    {
                        appearance.StorageKey = old.StorageKey;
                    }
                    else
                    {
                        appearance.StorageKey = created = await _imageStorage.WriteAsync(image.Bytes, image.Extension, token);
                    }

                    appearance.Value = "";
                }
            }
            await save();
            committed = true;
            if (old?.StorageKey != null && old.StorageKey != appearance.StorageKey)
            {
                _imageStorage.Delete(old.StorageKey);
            }
        }
        finally
        {
            appearance.Value = input;
            if (!committed)
            {
                if (created != null)
                {
                    _imageStorage.Delete(created);
                }
                appearance.StorageKey = old?.StorageKey;
                appearance.ContentType = old?.ContentType;
                appearance.OriginalFileName = originalName;
                appearance.ConcurrencyStamp = stamp;
                appearance.Updated = updated;
            }
        }
    }

    private async Task<bool> ImageMatchesAsync(string key, byte[] bytes, CancellationToken token)
    {
        try
        {
            return (await _imageStorage.ReadAsync(key, token)).AsSpan().SequenceEqual(bytes);
        }
        catch (Exception error)when (error is FileNotFoundException or DirectoryNotFoundException or InvalidDataException)
        {
            // A missing file or an older image exceeding a lowered limit must not prevent repair.
            return false;
        }
    }

    private sealed class ImageColumnVisitor : ExpressionVisitor
    {
        internal bool IncludesImage;
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.DeclaringType == typeof(GXAppearance) && node.Member.Name is
                nameof(GXAppearance.Value) or nameof(GXAppearance.StorageKey) or nameof(GXAppearance.ContentType) or nameof(GXAppearance.OriginalFileName) or nameof(GXAppearance.ResourceType))
            {
                IncludesImage = true;
            }
            return base.VisitMember(node);
        }
    }
}
