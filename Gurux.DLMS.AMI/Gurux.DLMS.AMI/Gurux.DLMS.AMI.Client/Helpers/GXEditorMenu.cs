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
/// <param name="menus">Service that registers the routed editor&apos;s command menu.</param>
public sealed class GXEditorMenu(IGXMenuService menus) : IDisposable
{
    /// <summary>Stable identifier of the routed editor command menu.</summary>
    public static readonly Guid MenuId = new("08a7e6dc-6589-43fd-8bb7-4beb05b339c1");
    /// <summary>Menu registration that owns the editor&apos;s Save, Remove and Cancel commands.</summary>
    private readonly GXPageMenu _menu = new(menus, menuId: MenuId);
    /// <summary>Validation messages associated with the current editor model.</summary>
    private ValidationMessageStore? _messages;
    /// <summary>Prevents overlapping save and cancel operations.</summary>
    private bool _busy;
    /// <summary>Prevents command callbacks from acting after the menu has been released.</summary>
    private bool _disposed;
    /// <summary>Invalidates pending validation and save callbacks when the editor is replaced or disposed.</summary>
    private int _generation;
    /// <summary>Form context of the save callback currently issuing navigation on this asynchronous flow.</summary>
    private static readonly AsyncLocal<EditContext?> _activeSave = new();
    /// <summary>Form state shared by all tabs editing the current model.</summary>
    public EditContext Context { get; private set; } = default!;

    // Only return navigation issued by the Save callback acknowledges the saved model.
    // Other UI events, including navigation during a pending save, retain their protection.
    /// <summary>Marks the active save context unmodified so navigation initiated by a successful save is accepted.</summary>
    internal static void AcceptSavedNavigation() => _activeSave.Value?.MarkAsUnmodified();

    /// <summary>Binds an editor model and its Save or Remove and Cancel callbacks to the shared command menu.</summary>
    /// <param name="model">Model instance whose editor state or command policy is needed.</param>
    /// <param name="action">Create, update or delete operation represented by the command.</param>
    /// <param name="save">Callback that persists the model or performs the requested removal.</param>
    /// <param name="cancel">Callback that returns from the editor without saving.</param>
    public void Set(object model, CrudAction action, Func<Task> save, Func<Task> cancel) =>
        Set(model, action, save, cancel, null);

    /// <summary>Sets editor commands with an optional explicit command policy for custom models.</summary>
    /// <param name="model">Model instance whose editor state or command policy is needed.</param>
    /// <param name="action">Create, update or delete operation represented by the command.</param>
    /// <param name="save">Callback that persists the model or performs the requested removal.</param>
    /// <param name="cancel">Callback that returns from the editor without saving.</param>
    /// <param name="policy">Stable policy identifier to check or assign to the command.</param>
    public void Set(object model, CrudAction action, Func<Task> save, Func<Task> cancel, string? policy)
    {
        if (_disposed)
        {
            return;
        }
        ++_generation;
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
                    var context = Context;
                    var generation = _generation;
                    var previousSave = _activeSave.Value;
                    try
                    {
                        if (action == CrudAction.Delete || await ValidateAsync())
                        {
                            if (_disposed || generation != _generation || !ReferenceEquals(Context, context))
                            {
                                return;
                            }
                            _activeSave.Value = context;
                            await save();
                        }
                    }
                    finally
                    {
                        _activeSave.Value = previousSave;
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

    /// <summary>Validates forms whose validators complete synchronously without blocking the UI thread.</summary>
    /// <returns>True when annotation and form validation accept the current model.</returns>
    /// <exception cref="InvalidOperationException">The form requires asynchronous validation; use <see cref="ValidateAsync"/> instead.</exception>
    public bool Validate()
    {
        var validation = ValidateAsync();
        if (!validation.IsCompleted)
        {
            throw new InvalidOperationException(Properties.Resources.ResourceManager.GetString("AsyncValidationRequired"));
        }
        return validation.GetAwaiter().GetResult();
    }

    /// <summary>Combines annotation and form validation, awaiting asynchronous validators when supported.</summary>
    /// <returns>True when both validation passes accept the current model.</returns>
    public async Task<bool> ValidateAsync()
    {
        var context = Context;
        var messages = _messages;
        var generation = _generation;
        messages?.Clear();
        var results = new List<ValidationResult>();
#if NET11_0_OR_GREATER
        bool valid = await Validator.TryValidateObjectAsync(context.Model, new ValidationContext(context.Model), results, true);
#else
        bool valid = Validator.TryValidateObject(context.Model, new ValidationContext(context.Model), results, true);
#endif
        if (_disposed || generation != _generation || !ReferenceEquals(Context, context))
        {
            return false;
        }
        foreach (var result in results)
            {
            foreach (var member in result.MemberNames.DefaultIfEmpty(string.Empty))
                {
                messages?.Add(new FieldIdentifier(context.Model, member), result.ErrorMessage ?? string.Empty);
            }
        }
        context.NotifyValidationStateChanged();
        bool formValid = await GXEditContexts.ValidateAsync(context);
        return !_disposed && generation == _generation && ReferenceEquals(Context, context) && formValid && valid;
    }

    /// <summary>Adds a server or application validation message to a field and notifies the form.</summary>
    /// <param name="field">Model field name associated with the validation error.</param>
    /// <param name="message">Localized message displayed to the user.</param>
    public void AddValidationError(string field, string message)
    {
        _messages?.Add(new FieldIdentifier(Context.Model, field), message);
        Context.NotifyValidationStateChanged();
    }

    /// <summary>Removes the currently registered editor commands.</summary>
    public void Clear() => _menu.Clear();
    /// <summary>Releases command registrations and validation messages and prevents subsequent command execution.</summary>
    public void Dispose()
    {
        ++_generation;
        _disposed = true;
        _messages?.Clear();
        _menu.Dispose();
    }
}
