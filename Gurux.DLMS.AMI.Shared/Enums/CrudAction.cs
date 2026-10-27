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
namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>Resource operations used by permissions, filtering, and audit records.</summary>
    [System.ComponentModel.Description("Resource operations used by permissions, filtering, and audit records.")]
    public enum CrudAction : byte
    {
        /// <summary>No resource operation is specified.</summary>
        [System.ComponentModel.Description("No resource operation is specified.")]
        None = 0,
        /// <summary>Create a resource.</summary>
        [System.ComponentModel.Description("Create a resource.")]
        Create = 0x1,
        /// <summary>Read an individual resource.</summary>
        [System.ComponentModel.Description("Read an individual resource.")]
        Read = 0x2,
        /// <summary>Update an existing resource.</summary>
        [System.ComponentModel.Description("Update an existing resource.")]
        Update = 0x4,
        /// <summary>Delete selected resources.</summary>
        [System.ComponentModel.Description("Delete selected resources.")]
        Delete = 0x8,
        /// <summary>List resources matching a query.</summary>
        [System.ComponentModel.Description("List resources matching a query.")]
        List = 0x10,
        /// <summary>Clear resources within the selected scope.</summary>
        [System.ComponentModel.Description("Clear resources within the selected scope.")]
        Clear = 0x20,
        /// <summary>
        /// Agent asks the next task.
        /// </summary>
        [System.ComponentModel.Description("Agent asks the next task.")]
        Next = 0x40,
        /// <summary>
        /// Clone target object.
        /// </summary>
        [System.ComponentModel.Description("Clone target object.")]
        Clone = Next,
    }
}