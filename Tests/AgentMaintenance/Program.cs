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

using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Http;

var now = new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
Check(new GXMaintenanceException { EndTime = now.AddHours(2), RetryAfter = TimeSpan.FromSeconds(5) }, TimeSpan.FromHours(2));
Check(new GXMaintenanceException(), TimeSpan.FromMinutes(1));
Check(new GXMaintenanceException { RetryAfter = TimeSpan.FromHours(3) }, TimeSpan.FromMinutes(1));
Check(new GXMaintenanceException { EndTime = now.AddMinutes(-1) }, TimeSpan.FromMinutes(1));
Check(new GXMaintenanceException { EndTime = new DateTimeOffset(2026, 11, 3, 15, 0, 0, TimeSpan.FromHours(2)) }, TimeSpan.FromHours(1));
using var cancellation = new CancellationTokenSource();
cancellation.Cancel();
try
{
    await GXMaintenanceRetry.WaitAsync(new GXMaintenanceException(), cancellation.Token);
    throw new Exception("Maintenance wait ignored cancellation.");
}
catch (OperationCanceledException) { }
using var client = new HttpClient(new GXMaintenanceHandler(new MaintenanceResponse()));
try
{
    await client.PostAsync("https://localhost/guruxami/negotiate", null);
    throw new Exception("SignalR maintenance response was not translated.");
}
catch (GXMaintenanceException error)
{
    if (error.EndTime != new DateTimeOffset(2030, 1, 2, 12, 0, 0, TimeSpan.Zero))
        {
        throw new Exception("SignalR maintenance end time was lost.");
    }
}
Console.WriteLine("All maintenance retry checks passed.");

void Check(GXMaintenanceException error, TimeSpan expected)
{
    var actual = GXMaintenanceRetry.GetDelay(error, now);
    if (actual != expected) {
        throw new Exception($"Expected {expected}, got {actual}.");
    }
}

sealed class MaintenanceResponse : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("""{"type":"maintenance","endTime":"2030-01-02T12:00:00Z"}""")
        });
}
