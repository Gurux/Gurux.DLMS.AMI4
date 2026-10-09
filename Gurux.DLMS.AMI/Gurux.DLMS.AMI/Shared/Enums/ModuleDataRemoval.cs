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

namespace Gurux.DLMS.AMI.Shared.Enums;

/// <summary>
/// Explicit scope of a module uninstall. 
/// Omission preserves all module data.
/// </summary>
public enum ModuleDataRemoval
{
    /// <summary>
    /// Preserve all module data. 
    /// This is the default behavior if no explicit scope is specified.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Delete all module data, 
    /// but preserve any tables that may have been created by the module.
    /// </summary>
    DeleteData = 1,
    /// <summary>
    /// Delete all module data, 
    /// including any tables that may have been created by the module.
    /// </summary>
    DeleteDataAndTables = 2
}
