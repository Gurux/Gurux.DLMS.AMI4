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

namespace Gurux.DLMS.AMI.Module;

/// <summary>Displays the host UI confirmation dialog and awaits the user's answer.</summary>
public interface IAmiConfirmation
{
    /// <summary>Returns true only when the user approves; cancel and close return false.</summary>
    /// <param name="title">Title displayed by the host's confirmation dialog.</param>
    /// <param name="message">Message describing the action that requires user confirmation.</param>
    /// <returns>A task whose result is true when the user confirms, or false when the dialog is canceled or unavailable.</returns>
    Task<bool> ConfirmAsync(string title, string message);

    /// <summary>Displays a confirmation with action text, selected items and an optional permanent deletion choice.</summary>
    /// <param name="title">Dialog title.</param>
    /// <param name="message">Description of the action.</param>
    /// <param name="options">Action text, selected items and available choices.</param>
    /// <returns>The user's approval and permanent deletion choice. Cancel and unavailable UI never approve.</returns>
    Task<AmiConfirmationResult> ConfirmAsync(string title, string message, AmiConfirmationOptions options);
}
