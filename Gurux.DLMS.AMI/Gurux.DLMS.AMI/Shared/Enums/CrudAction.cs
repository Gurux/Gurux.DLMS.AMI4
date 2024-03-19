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
    /// <summary>
    /// CRUD action.
    /// </summary>
    public enum CrudAction : byte
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Create 
        /// </summary>
        Create = 0x1,
        /// <summary>
        /// Read object.
        /// </summary>
        Read = 0x2,
        /// <summary>
        /// Edit object.
        /// </summary>
        Update = 0x4,
        /// <summary>
        /// Delete object.
        /// </summary>
        Delete = 0x8,
        /// <summary>
        /// List objects.
        /// </summary>
        List = 0x10,
        /// <summary>
        /// Clear objects.
        /// </summary>
        Clear = 0x20,
        /// <summary>
        /// Agent asks the next task.
        /// </summary>
        Next = 0x40,
        /// <summary>
        /// Clone target object.
        /// </summary>
        Clone = 0x40,
    }
}