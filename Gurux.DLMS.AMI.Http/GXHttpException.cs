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

using System.Net;
using System.Collections.ObjectModel;

namespace Gurux.DLMS.AMI.Http;

/// <summary>An HTTP error retaining structured API diagnostics.</summary>
public class GXHttpException : HttpRequestException
{
    /// <summary>Short problem description.</summary>
    public string? Title { get; }
    /// <summary>Explanation of this occurrence.</summary>
    public string? Detail { get; }
    /// <summary>Server trace identifier for diagnostics.</summary>
    public string? TraceId { get; }
    /// <summary>Validation messages grouped by field.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

    /// <summary>Creates an HTTP error with an immutable snapshot of validation messages.</summary>
    public GXHttpException(string message, HttpStatusCode statusCode, string? title = null,
        string? detail = null, string? traceId = null, IDictionary<string, string[]>? errors = null)
        : base(message, null, statusCode)
    {
        Title = title;
        Detail = detail;
        TraceId = traceId;
        Errors = new ReadOnlyDictionary<string, IReadOnlyList<string>>(
            (errors ?? new Dictionary<string, string[]>()).ToDictionary(p => p.Key,
                p => (IReadOnlyList<string>)Array.AsReadOnly(p.Value.ToArray()), StringComparer.Ordinal));
    }
}
