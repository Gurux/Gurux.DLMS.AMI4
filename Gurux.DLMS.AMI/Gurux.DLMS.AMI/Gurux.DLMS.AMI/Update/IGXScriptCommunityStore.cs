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
using Gurux.DLMS.AMI.Services.Database;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Model;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Durable storage boundary for private submission records.</summary>
public interface IGXScriptCommunityStore
{
    /// <summary>Reads durable submission receipts.</summary>
    /// <param name="token">Cancels reading the stored submission records.</param>
    /// <returns>All retained private submission records.</returns>
    Task<List<GXScriptCommunityRecord>> ReadAsync(CancellationToken token);
    /// <summary>Persists a new or updated private receipt.</summary>
    /// <param name="record">Submission state to persist.</param>
    /// <param name="insert">True to create a new receipt; false to update the existing record.</param>
    /// <param name="token">Cancels the persistence operation.</param>
    /// <returns>A task that completes when the receipt has been stored.</returns>
    Task SaveAsync(GXScriptCommunityRecord record, bool insert, CancellationToken token);
}
