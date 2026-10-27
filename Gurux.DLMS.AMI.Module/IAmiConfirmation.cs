//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

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
