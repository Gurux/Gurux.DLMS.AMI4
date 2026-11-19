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
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Immutable routing metadata for an endpoint published by a specific module generation.</summary>
/// <param name="ModuleId">Identity of the module that owns this endpoint.</param>
/// <param name="Generation">Generation identifier used to reject stale endpoint dispatch.</param>
/// <param name="Index">Index of the endpoint definition within the owning generation.</param>
/// <param name="Pattern">Route pattern advertised to ASP.NET Core routing.</param>
/// <param name="Methods">HTTP methods accepted by this endpoint.</param>
/// <param name="Policies">Authorization policies required before dispatch.</param>
public sealed record AmiPublishedEndpoint(string ModuleId, string Generation, int Index, string Pattern, string[] Methods, string[] Policies)
{
    /// <summary>Whether this endpoint generation uses module authentication.</summary>
    public bool HasAuthenticator { get; init; }
    /// <summary>Whether every declared HTTP method is explicitly allowed anonymously by the host.</summary>
    public bool IsAnonymous { get; init; }
}









