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

using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Synchronizes enum rows inside the caller's existing template mutation transaction.</summary>
internal static class GXAttributeListItems
{
    /// <summary>Rejects invalid enum definitions before inserting a template or replacing its rows.</summary>
    /// <param name="template">The proposed attribute definition.</param>
    /// <exception cref="ValidationException">A label, numeric value or bit-field definition is invalid.</exception>
    internal static void Validate(GXAttributeTemplate template)
    {
        if (GXAttributeEnumValues.GetError(template) is string error)
        {
            throw new ValidationException(error);
        }
    }

    /// <summary>Updates retained rows, inserts new rows and deletes removed rows for one attribute.</summary>
    /// <param name="connection">The caller's database connection and history transaction lease.</param>
    /// <param name="template">The authorized attribute whose non-null list replaces its stored enum rows.</param>
    /// <param name="token">Cancels database operations.</param>
    /// <returns>A task that completes after the supplied list has been synchronized.</returns>
    /// <exception cref="ValidationException">An enum label, value or bit-field definition is invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">A supplied row identifier does not belong to this attribute.</exception>
    internal static async Task SaveAsync(GXDbConnection connection, GXAttributeTemplate template, CancellationToken token)
    {
        if (template.ListItems == null)
        {
            return;
        }
        Validate(template);
        var stored = await connection.SelectAsync<GXAttributeListItem>(
            GXSelectArgs.SelectAll<GXAttributeListItem>(item => item.Template == template), token);
        var existingIds = stored.Select(item => item.Id).ToHashSet();
        if (template.ListItems.Any(item => item.Id != Guid.Empty && !existingIds.Contains(item.Id)))
        {
            throw new UnauthorizedAccessException();
        }
        var retained = template.ListItems.Where(item => item.Id != Guid.Empty).Select(item => item.Id).ToHashSet();
        foreach (var item in stored.Where(item => !retained.Contains(item.Id)))
        {
            await connection.DeleteAsync(GXDeleteArgs.DeleteById<GXAttributeListItem>(item.Id), token);
        }
        foreach (var item in template.ListItems)
        {
            item.Template = template;
            if (item.Id == Guid.Empty)
            {
                await connection.InsertAsync(GXInsertArgs.Insert(item), token);
            }
            else
            {
                await connection.UpdateAsync(GXUpdateArgs.Update(item, row => new { row.UIValue, row.Value, row.Shift, row.MaskSize }), token);
            }
        }
    }
}
