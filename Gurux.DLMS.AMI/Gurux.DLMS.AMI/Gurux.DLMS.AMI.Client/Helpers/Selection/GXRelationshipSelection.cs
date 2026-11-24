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
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace Gurux.DLMS.AMI.Client.Helpers.Selection;

/// <summary>Maintains key-based relationship selection independently of filtered, paged DTO instances.</summary>
/// <typeparam name="TItem">The type of related record.</typeparam>
public sealed class GXRelationshipSelection<TItem> : IDisposable
{
    /// <summary>Resolves the current edit model's authoritative relationship.</summary>
    private readonly Func<IList<TItem>> _items;
    /// <summary>Extracts a stable identity without relying on DTO equality.</summary>
    private readonly Func<TItem, object> _key;
    /// <summary>Preserves the initial selected records even after they are deselected.</summary>
    private TItem[] _snapshot = [];
    /// <summary>Cancels a page when a newer request supersedes it.</summary>
    private CancellationTokenSource? _request;
    /// <summary>Serializes relationship mutations and rollback.</summary>
    private readonly SemaphoreSlim _mutation = new(1, 1);

    /// <summary>Creates selection state for an existing edit model.</summary>
    /// <param name="items">Returns the live relationship collection.</param>
    /// <param name="key">Returns each record's stable persisted identity.</param>
    public GXRelationshipSelection(Func<IList<TItem>> items, Func<TItem, object> key)
    {
        _items = items;
        _key = key;
    }

    /// <summary>Gets the number of unique selected identities, including unloaded records.</summary>
    public int Count => _items().Select(_key).Distinct().Count();

    /// <summary>Captures selected rows after the wrapper has initialized default groups.</summary>
    public void Initialize()
    {
        _snapshot = _items().DistinctBy(_key).ToArray();
    }

    /// <summary>Tests selection using identity, allowing the server to return fresh DTO instances.</summary>
    /// <param name="item">The row to test.</param>
    /// <returns>True when the relationship contains the row's identity.</returns>
    public bool IsSelected(TItem item) => _items().Any(value => Equals(_key(value), _key(item)));

    /// <summary>Composes a page from server-filtered opening selections followed by available records.</summary>
    /// <param name="startIndex">Offset in the combined result.</param>
    /// <param name="count">Maximum number of records to return.</param>
    /// <param name="selectedOnly">Whether to query the current selection only.</param>
    /// <param name="provider">Executes the wrapper's typed server query.</param>
    /// <param name="showAllUsers">Whether the caller requested the administrator's all-users scope.</param>
    /// <param name="cancellationToken">Cancels this request.</param>
    /// <returns>The page and total number of matching records.</returns>
    /// <exception cref="OperationCanceledException">A newer request superseded this page or cancellation was requested.</exception>
    public async ValueTask<ItemsProviderResult<TItem>> ReadAsync(int startIndex, int count, bool selectedOnly,
        Func<GXRelationshipQuery<TItem>, ValueTask<ItemsProviderResult<TItem>>> provider,
        bool showAllUsers = false, CancellationToken cancellationToken = default)
    {
        using var request = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var previous = Interlocked.Exchange(ref _request, request);
        previous?.Cancel();
        try
        {
            int size = Math.Max(1, count);
            TItem[] included = selectedOnly ? _items().DistinctBy(_key).ToArray() : _snapshot;
            ItemsProviderResult<TItem> selected = new([], 0);
            if (included.Length != 0)
            {
                selected = await provider(new GXRelationshipQuery<TItem>(
                    new GXItemsProviderRequest(startIndex, size, showAllUsers, cancellationToken: request.Token), included));
                request.Token.ThrowIfCancellationRequested();
            }
            if (selectedOnly)
            {
                return selected;
            }
            var rows = selected.Items.DistinctBy(_key).Take(size).ToList();
            int remaining = size - rows.Count;
            var available = await provider(new GXRelationshipQuery<TItem>(
                new GXItemsProviderRequest(Math.Max(0, startIndex - selected.TotalItemCount), Math.Max(1, remaining),
                    showAllUsers, cancellationToken: request.Token), excluded: _snapshot));
            request.Token.ThrowIfCancellationRequested();
            rows.AddRange(available.Items.Take(remaining));
            int total = (int)Math.Min(int.MaxValue, (long)selected.TotalItemCount + available.TotalItemCount);
            return new(rows.DistinctBy(_key).ToArray(), total);
        }
        finally
        {
            Interlocked.CompareExchange(ref _request, null, request);
        }
    }

    /// <summary>Changes only the supplied identities, with explicit mutation ownership and rollback.</summary>
    /// <param name="items">Rows selected by the row or visible-page checkbox.</param>
    /// <param name="selected">True to add, false to remove.</param>
    /// <param name="ownerMutates">True when the wrapper callback owns the relationship mutation.</param>
    /// <param name="owner">The wrapper's add or remove callback.</param>
    /// <returns>A task completing when the relationship has changed.</returns>
    /// <exception cref="InvalidOperationException">Owner mutation was requested without an owner callback.</exception>
    public async Task ChangeAsync(IEnumerable<TItem> items, bool selected, bool ownerMutates = false,
        Func<IReadOnlyList<TItem>, Task>? owner = null)
    {
        await _mutation.WaitAsync();
        IList<TItem> relationship = _items();
        TItem[] before = relationship.ToArray();
        try
        {
            TItem[] changes = items.DistinctBy(_key).Where(item => IsSelected(item) != selected).ToArray();
            if (changes.Length == 0)
            {
                return;
            }
            if (ownerMutates)
            {
                if (owner == null)
                {
                    throw new InvalidOperationException("An owner callback is required for owner-managed selection.");
                }
                await owner(changes);
            }
            else if (selected)
            {
                foreach (var item in changes)
                {
                    relationship.Add(item);
                }
            }
            else
            {
                var keys = changes.Select(_key).ToHashSet();
                for (int index = relationship.Count - 1; index >= 0; --index)
                {
                    if (keys.Contains(_key(relationship[index])))
                    {
                        relationship.RemoveAt(index);
                    }
                }
            }
        }
        catch
        {
            relationship.Clear();
            foreach (var item in before)
            {
                relationship.Add(item);
            }
            throw;
        }
        finally
        {
            _mutation.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Interlocked.Exchange(ref _request, null)?.Cancel();
        _mutation.Dispose();
    }
}
