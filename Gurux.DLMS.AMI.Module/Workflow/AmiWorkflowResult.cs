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

namespace Gurux.DLMS.AMI.Module.Workflow;

/// <summary>Outcome of a module handler. Rejection is valid only for the Before phase.</summary>
public sealed record AmiWorkflowResult
{
    /// <summary>Creates the immutable decision returned by a workflow handler.</summary>
    /// <param name="allowed">Whether the proposed operation may continue.</param>
    /// <param name="message">Rejection explanation, or null for successful completion.</param>
    private AmiWorkflowResult(bool allowed, string? message) { Allowed = allowed; Message = message; }
    /// <summary>Whether the operation may continue.</summary>
    public bool Allowed { get; }
    /// <summary>Explanation of a rejection, suitable for returning to the caller.</summary>
    public string? Message { get; }
    /// <summary>Successful completion; allows the operation to continue.</summary>
    public static AmiWorkflowResult Continue { get; } = new(true, null);
    /// <summary>Rejects a proposed change. After handlers must report failures by throwing instead.</summary>
    /// <param name="message">Nonblank explanation of why the proposed change is rejected.</param>
    /// <returns>A result that prevents the proposed operation from continuing.</returns>
    /// <exception cref="ArgumentException">The explanation is null, empty, or whitespace.</exception>
    public static AmiWorkflowResult Reject(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new(false, message);
    }
}
