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
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Gurux.DLMS.AMI.Client.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Downloaded public content with its remaining freshness lifetime.</summary>
/// <param name="Body">Downloaded response bytes.</param>
/// <param name="ContentType">Validated content type returned by the origin.</param>
/// <param name="ExpiresAt">Time at which the downloaded content becomes stale.</param>
/// <param name="SourceUrl">Origin URL associated with this response.</param>
public sealed record GXHttpProxyResponse(byte[] Body, string ContentType, DateTimeOffset ExpiresAt, string SourceUrl);
