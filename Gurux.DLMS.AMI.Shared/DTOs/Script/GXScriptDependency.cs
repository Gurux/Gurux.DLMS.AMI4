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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Script;

/// <summary>An assembly reference recorded from a successfully compiled script.</summary>
[Description("An assembly reference recorded from a successfully compiled script.")]
[DataContract, Serializable]
[IndexCollection(true, nameof(ScriptId), nameof(Name))]
public sealed class GXScriptDependency : IUnique<Guid>
{
    /// <summary>Unique dependency record identifier.</summary>
    [DataMember(Name = "ID")]
    public Guid Id { get; set; }

    /// <summary>Script owning this compilation dependency.</summary>
    [DataMember, IsRequired, Index(false)]
    [ForeignKey(typeof(GXScript), OnDelete = ForeignKeyDelete.Cascade)]
    public Guid ScriptId { get; set; }

    /// <summary>Simple assembly name.</summary>
    [DataMember, IsRequired, StringLength(256), Index(false)]
    public string Name { get; set; } = "";

    /// <summary>Assembly version used by the compiler.</summary>
    [DataMember, IsRequired, StringLength(64)]
    public string AssemblyVersion { get; set; } = "";
}
