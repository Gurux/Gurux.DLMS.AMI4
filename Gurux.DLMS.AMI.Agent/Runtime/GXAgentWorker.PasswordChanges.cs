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

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Gurux.DLMS.AMI.Http;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Secure;

namespace Gurux.DLMS.AMI.Agent.Worker;

public partial class GXAgentWorker
{
    private sealed class PasswordChangeLease : IDisposable
    {
        internal PasswordWork? Work;
        public void Dispose()
        {
            Work?.Dispose();
        }
    }

    // Agent-private transport DTOs keep Shared and the agent's client-UI dependency unchanged.
    private sealed class PasswordWork : IDisposable
    {
        public Guid Id { get; set; }
        public string State { get; set; } = "";
        public GXDevice Device { get; set; } = new();
        public string AssociationLogicalName { get; set; } = "";
        public byte[] CurrentPassword { get; set; } = [];
        public byte[] NewPassword { get; set; } = [];
        public void Dispose()
        {
            CryptographicOperations.ZeroMemory(CurrentPassword);
            CryptographicOperations.ZeroMemory(NewPassword);
        }
    }

    private static Exception PasswordChangeError() => new InvalidOperationException(
        "Password change failed. Retry and verify the operation before ordinary meter work. No password material is included in this report.");

    private static bool IsPasswordChange(GXTask task, out Guid id)
    {
        id = Guid.Empty;
        if (task.TaskType != TaskType.Write || string.IsNullOrWhiteSpace(task.Data))
        {
            return false;
        }
        try
        {
            using var json = JsonDocument.Parse(task.Data);
            return json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty("PasswordChangeId", out var value) && value.ValueKind == JsonValueKind.String &&
                value.TryGetGuid(out id) && id != Guid.Empty;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private object PasswordClaim(GXTask task) => new
    {
        TaskId = task.Id,
        AgentId = RuntimeOptions.Id,
        Start = task.Start ?? throw new InvalidOperationException("Password changes require a claimed task.")
    };

    private async Task<PasswordWork> PasswordWorkAsync(GXTask task, CancellationToken token)
    {
        if (!IsPasswordChange(task, out var id))
        {
            throw new InvalidOperationException("Invalid password-change task.");
        }
        var work = await client.PostAsJson<PasswordWork>($"api/PasswordChange/{id}/Work", PasswordClaim(task), token);
        if (work.Id != id || work.Device.Id != task.TargetDevice ||
            work.State is not ("Queued" or "Writing" or "Verified") ||
            work.State == "Queued" && work.CurrentPassword.Length == 0 ||
            work.State != "Verified" && work.NewPassword.Length == 0)
        {
            work.Dispose();
            throw new InvalidOperationException("The server returned an invalid password-change operation.");
        }
        return work;
    }

    private async Task ExecutePasswordChangeAsync(GXDevice device, GXDLMSReader reader,
        GXDLMSSecureClient connection, GXTask task, CancellationToken token)
    {
        using var work = await PasswordWorkAsync(task, token);
        if (work.State == "Verified")
        {
            return;
        }
        if (device.Dynamic == true || connection.Authentication != Gurux.DLMS.Enums.Authentication.Low)
        {
            throw new InvalidOperationException("This association cannot perform the queued LLS password change.");
        }
        if (reader.PacketTracingEnabled)
        {
            throw new InvalidOperationException("Password changes require packet tracing to be disabled.");
        }
        var alreadyNew = work.State == "Writing" && connection.Password != null &&
            CryptographicOperations.FixedTimeEquals(connection.Password, work.NewPassword);
        await GXPasswordChangeExecution.RunAsync(alreadyNew,
            async () =>
            {
                using var response = await client.PostAsJsonAsync($"api/PasswordChange/{work.Id}/Writing", PasswordClaim(task), token);
                await response.EnsureSuccessAsync(token);
            },
            () =>
            {
                token.ThrowIfCancellationRequested();
                var association = new GXDLMSAssociationLogicalName(work.AssociationLogicalName) { Secret = work.NewPassword.ToArray() };
                try
                {
                    reader.Write(association, 7);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(association.Secret);
                }
                return Task.CompletedTask;
            },
            () => reader.ReconnectWithPasswordAsync(work.NewPassword),
            async () =>
            {
                using var response = await client.PostAsJsonAsync($"api/PasswordChange/{work.Id}/Verified", PasswordClaim(task), token);
                await response.EnsureSuccessAsync(token);
            });
    }
}