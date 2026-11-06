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

using System.Diagnostics;
using System.Net;

namespace Gurux.DLMS.AMI.Agent.Worker;

/// <summary>Measures AMI requests without including addresses, task identifiers or credentials in tags.</summary>
internal sealed class GXAgentHttpMetricsHandler(HttpMessageHandler inner, GXAgentRuntimeMetrics metrics) : DelegatingHandler(inner)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        long started = Stopwatch.GetTimestamp();
        string outcome = "network";
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            outcome = Classify(response.StatusCode);
            return response;
        }
        catch (HttpRequestException error) when (error.StatusCode is HttpStatusCode status)
        {
            outcome = Classify(status);
            throw;
        }
        catch (OperationCanceledException)
        {
            outcome = "cancelled";
            throw;
        }
        finally
        {
            metrics.HttpFinished(started, outcome);
        }
    }

    private static string Classify(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "authorization",
        HttpStatusCode.TooManyRequests => "throttled",
        _ when (int)status >= 500 => "server",
        _ when (int)status < 200 || (int)status >= 300 => "rejected",
        _ => "success"
    };
}
