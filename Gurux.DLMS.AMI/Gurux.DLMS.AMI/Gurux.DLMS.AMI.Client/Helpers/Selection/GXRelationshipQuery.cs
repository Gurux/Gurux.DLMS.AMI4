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

using Gurux.UI.Components;

namespace Gurux.DLMS.AMI.Client.Helpers.Selection;

/// <summary>Restricts a relationship page to included records or excludes its opening snapshot.</summary>
/// <typeparam name="TItem">The endpoint's record type.</typeparam>
public sealed class GXRelationshipQuery<TItem>
{
    /// <summary>Creates a bounded query using the table's paging and authorization scope.</summary>
    /// <param name="page">The table request.</param>
    /// <param name="included">Records whose identities must match, or null for no inclusion restriction.</param>
    /// <param name="excluded">Records whose identities must be excluded.</param>
    public GXRelationshipQuery(GXItemsProviderRequest page, IReadOnlyList<TItem>? included = null,
        IReadOnlyList<TItem>? excluded = null)
    {
        Page = page;
        Included = included;
        Excluded = excluded ?? [];
    }

    /// <summary>Gets the table request, including its cancellation token.</summary>
    public GXItemsProviderRequest Page { get; }
    /// <summary>Gets the required records, or null when any accessible record may match.</summary>
    public IReadOnlyList<TItem>? Included { get; }
    /// <summary>Gets records excluded to keep available-page offsets stable during selection.</summary>
    public IReadOnlyList<TItem> Excluded { get; }
    /// <summary>Gets the offset within the filtered segment.</summary>
    public int StartIndex => Page.StartIndex;
    /// <summary>Gets the maximum number of rows requested.</summary>
    public int Count => Page.Count;
    /// <summary>Gets whether the authenticated administrator requested all users' records.</summary>
    public bool ShowAllUserData => Page.ShowAllUserData;
    /// <summary>Gets the requested sort direction.</summary>
    public bool Descending => Page.Descending;
    /// <summary>Gets the token that cancels superseded queries.</summary>
    public CancellationToken CancellationToken => Page.CancellationToken;
}
