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
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using System.Runtime.Loader;

namespace Gurux.DLMS.AMI.Server.Services
{
    /// <summary>
    /// This interface handles workflow operations.
    /// </summary>
    public interface IWorkflowHandler
    {
        /// <summary>
        /// Add new workflow to executed.
        /// </summary>
        /// <param name="item">Item to add.</param>
        void Add(GXWorkflow item);

        /// <summary>
        /// Remove workflow.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        void Delete(GXWorkflow item);
       
        /// <summary>
        /// The workflow item has changed and for example, the script needs to reload.
        /// </summary>
        /// <param name="source">Changed item.</param>
        void Update(object source);

        /// <summary>
        /// Invoke trigger.
        /// </summary>
        /// <param name="trigger">Trigger to execute.</param>
        /// <param name="activity">Selected activity.</param>
        /// <param name="source">Invoker.</param>
        void Execute(Type trigger, string activity, object? source);

        /// <summary>
        /// Add loaded script.
        /// </summary>
        /// <param name="id">Script to add.</param>
        /// <param name="alc">Assembly load context.</param>
        void AddScript(Guid id, AssemblyLoadContext alc);

        /// <summary>
        /// Unload script
        /// </summary>
        /// <param name="id">Script to remove.</param>
        void UnloadScript(Guid id);

        /// <summary>
        /// Get loaded script.
        /// </summary>
        /// <param name="id">Script Id.</param>
        /// <returns>Assembly load context.</returns>
        AssemblyLoadContext? GetScript(Guid id);
    }
}
