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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;

namespace Gurux.DLMS.AMI.Shared.DTOs.Notification;

/// <summary>A globally unique event source owned by the framework or a module.</summary>
[Description("A globally unique event source owned by the framework or a module.")]
[DataContract, Serializable]
public class GXNotificationSource : IUnique<int>
{
    /// <summary>Source row identifier used by the ORM.</summary>
    [Description("Source row identifier used by the ORM.")]
    [DataMember, IsRequired, Key, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Owning module identifier; null for framework sources.</summary>
    [Description("Owning module identifier; null for framework sources.")]
    [DataMember, StringLength(64)]
    // Module cleanup is explicit to avoid multiple cascade paths through module/notification creators on SQL Server.
    [ForeignKey(typeof(GXModule))]
    public string? ModuleId { get; set; }

    /// <summary>Exact event type or resource prefix, independent of the permission catalog.</summary>
    [Description("Exact event type or resource prefix, independent of the permission catalog.")]
    [DataMember, IsRequired, Required, StringLength(128), Index(Unique = true)]
    public string Source { get; set; } = string.Empty;
}
