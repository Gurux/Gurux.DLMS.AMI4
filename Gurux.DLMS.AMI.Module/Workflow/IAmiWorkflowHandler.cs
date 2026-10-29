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

/// <summary>Executes a module-defined workflow action under the host's invocation context.</summary>
public interface IAmiWorkflowHandler
{
    /// <summary>Stable action identifier, unique within the module.</summary>
    string Code { get; }
    /// <summary>Executes an action. Before is awaited inline and may reject; After cannot undo a commit.
    /// The host supplies cancellation for shutdown and time limits. Handlers must observe it.</summary>
    /// <param name="context">Trigger, user, target, and value snapshots for this invocation.</param>
    /// <param name="cancellationToken">Cancels execution when the host shuts down or the invocation times out.</param>
    /// <returns>The handler's continuation or rejection decision; rejection applies only before commit.</returns>
    ValueTask<AmiWorkflowResult> ExecuteAsync(AmiWorkflowContext context, CancellationToken cancellationToken = default);
}
