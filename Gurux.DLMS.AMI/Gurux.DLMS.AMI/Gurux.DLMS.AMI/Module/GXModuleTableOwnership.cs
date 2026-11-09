// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Services.Database;
using Gurux.DLMS.AMI.Module.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Module;
using Gurux.Service.Orm.Common;
namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Provides module table ownership.</summary>
public sealed class GXModuleTableOwnership : IUnique<string>
{
    /// <summary>Gets or sets the id.</summary>
    [Key, StringLength(64)] public string Id { get; set; } = "";
    /// <summary>Gets or sets the module id.</summary>
    [StringLength(128)] public string ModuleId { get; set; } = "";
    /// <summary>Gets or sets the schema name.</summary>
    [StringLength(128)] public string SchemaName { get; set; } = "";
    /// <summary>Gets or sets the table name.</summary>
    [StringLength(256)] public string TableName { get; set; } = "";
    /// <summary>Gets or sets the kind.</summary>
    public AmiModuleTableKind Kind { get; set; }
    /// <summary>Gets or sets the dropped.</summary>
    public bool Dropped { get; set; }
}
