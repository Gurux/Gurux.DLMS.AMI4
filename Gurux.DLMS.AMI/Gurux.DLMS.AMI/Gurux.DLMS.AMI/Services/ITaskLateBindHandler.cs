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
using Gurux.DLMS.AMI.Shared.DTOs;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Services
{
    /// <summary>
    /// This interface is used to handle late bind tasks.
    /// </summary>
    public interface ITaskLateBindHandler
    {
        /// <summary>
        /// Add new task to executed.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="task">Task to add.</param>
        void Add(ClaimsPrincipal user, GXTask task);
        /// <summary>
        /// Add new task to executed.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="tasks">Tasks to add.</param>
        void AddRange(ClaimsPrincipal user, IEnumerable<GXTask> tasks);
    }
}
