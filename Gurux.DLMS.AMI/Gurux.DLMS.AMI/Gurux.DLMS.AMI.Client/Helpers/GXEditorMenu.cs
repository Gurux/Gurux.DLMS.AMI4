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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.UI.Components;
using Gurux.UI.Components.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Owns a routed editor's commands and form state independently of its selected tab.</summary>
public sealed class GXEditorMenu(IGXMenuService menus) : IDisposable
{
    public static readonly Guid MenuId = new("08a7e6dc-6589-43fd-8bb7-4beb05b339c1");
    private readonly GXPageMenu _menu = new(menus, menuId: MenuId);
    private ValidationMessageStore? _messages;
    private bool _busy;
    private bool _disposed;
    private static readonly AsyncLocal<EditContext?> ActiveSave = new();
    public EditContext Context { get; private set; } = default!;

    // Only return navigation issued by the Save callback acknowledges the saved model.
    // Other UI events, including navigation during a pending save, retain their protection.
    internal static void AcceptSavedNavigation() => ActiveSave.Value?.MarkAsUnmodified();

    public void Set(object model, CrudAction action, Func<Task> save, Func<Task> cancel) =>
        Set(model, action, save, cancel, null);

    /// <summary>Sets editor commands with an optional explicit command policy for custom models.</summary>
    public void Set(object model, CrudAction action, Func<Task> save, Func<Task> cancel, string? policy)
    {
        if (_disposed)
        {
            return;
        }
        _messages?.Clear();
        Context = GXEditContexts.For(model);
        _messages = new ValidationMessageStore(Context);
        _menu.Clear();
        _menu.EditContext = Context;
        _menu.AddMenuItems([
            new GXMenuItem(action == CrudAction.Delete ? Properties.Resources.Remove : Properties.Resources.Save,
                action == CrudAction.Delete ? "Remove" : "Save", EventCallback.Factory.Create(this, async () =>
                {
                    if (_disposed || _busy)
                    {
                        return;
                    }
                    _busy = true;
                    var previousSave = ActiveSave.Value;
                    try
                    {
                        if (action == CrudAction.Delete || Validate())
                        {
                            ActiveSave.Value = Context;
                            await save();
                        }
                    }
                    finally
                    {
                        ActiveSave.Value = previousSave;
                        _busy = false;
                    }
                }), action == CrudAction.Delete ? EnableStyle.Always : EnableStyle.Modified)
                { Policy = policy ?? GXEditorPolicies.For(model, action) },
            new GXMenuItem(Properties.Resources.Cancel, "Cancel", EventCallback.Factory.Create(this, async () =>
            {
                if (!_disposed && !_busy)
                {
                    await cancel();
                }
            }))
        ]);
    }

    public bool Validate()
    {
        _messages?.Clear();
        var results = new List<ValidationResult>();
        bool valid = Validator.TryValidateObject(Context.Model, new ValidationContext(Context.Model), results, true);
        foreach (var result in results)
            {
            foreach (var member in result.MemberNames.DefaultIfEmpty(string.Empty))
                {
                _messages?.Add(new FieldIdentifier(Context.Model, member), result.ErrorMessage ?? string.Empty);
            }
        }
        Context.NotifyValidationStateChanged();
        return Context.Validate() && valid;
    }

    public void AddValidationError(string field, string message)
    {
        _messages?.Add(new FieldIdentifier(Context.Model, field), message);
        Context.NotifyValidationStateChanged();
    }

    public void Clear() => _menu.Clear();
    public void Dispose()
    {
        _disposed = true;
        _messages?.Clear();
        _menu.Dispose();
    }
}
