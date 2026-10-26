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

using System.ComponentModel.DataAnnotations;
using Gurux.Service.Orm.Common;
namespace Gurux.DLMS.AMI.Shared.DTOs.Update;

/// <summary>
/// Persistent catalog identity, independent of mutable display names and agent GUIDs.
/// </summary>
public sealed class GXCatalogProduct : IUnique<string>
{
    /// <summary>Gets or sets the stable product identifier in the update catalog.</summary>
    [Key, StringLength(256)] public string Id { get; set; } = "";

    /// <summary>Gets or sets the identifier of the application entity associated with the catalog product.</summary>
    [StringLength(128)] public string EntityId { get; set; } = "";
}
