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

using System.ComponentModel.DataAnnotations;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Shared.DTOs.Script;

/// <summary>Source retained transactionally before an installed catalog template is replaced.</summary>
[System.ComponentModel.Description("Source retained transactionally before an installed catalog template is replaced.")]
public sealed class GXScriptTemplateHistory : IUnique<Guid>
{
    /// <summary>Revision identifier.</summary>
    [System.ComponentModel.Description("Revision identifier.")]
    public Guid Id { get; set; }
    /// <summary>Script whose previous source is retained.</summary>
    [System.ComponentModel.Description("Script whose previous source is retained.")]
    [ForeignKey(typeof(GXScript), OnDelete = ForeignKeyDelete.Cascade), Index(false)]
    public Guid ScriptId { get; set; }
    /// <summary>Previous catalog version.</summary>
    [System.ComponentModel.Description("Previous catalog version.")]
    [StringLength(64)] public string? CatalogVersion { get; set; }
    /// <summary>Previous user source, including any local edits.</summary>
    [System.ComponentModel.Description("Previous user source, including any local edits.")]
    public string SourceCode { get; set; } = "";
    /// <summary>Previous additional imports.</summary>
    [System.ComponentModel.Description("Previous additional imports.")]
    public string? Namespaces { get; set; }
    /// <summary>User who accepted the replacement.</summary>
    [System.ComponentModel.Description("User who accepted the replacement.")]
    [StringLength(450)] public string UserId { get; set; } = "";
    /// <summary>Time at which the replacement was accepted.</summary>
    [System.ComponentModel.Description("Time at which the replacement was accepted.")]
    public DateTimeOffset CreationTime { get; set; }
}
