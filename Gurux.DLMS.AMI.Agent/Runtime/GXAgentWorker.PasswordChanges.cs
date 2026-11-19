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

/// <summary>Coordinates agent lifecycle, validated configuration and device task execution.</summary>
public partial class GXAgentWorker
{
    /// <summary>Owns one password change claim and clears its sensitive work data on disposal.</summary>
    private sealed class PasswordChangeLease : IDisposable
    {
        /// <summary>Password change operation owned by this lease.</summary>
        internal PasswordWork? Work;
        /// <inheritdoc/>
        public void Dispose()
        {
            Work?.Dispose();
        }
    }

    // Agent-private transport DTOs keep Shared and the agent's client-UI dependency unchanged.
    /// <summary>Carries leased password change state and sensitive credentials for one operation.</summary>
    private sealed class PasswordWork : IDisposable
    {
        /// <summary>Gets or sets the identity associated with this registered work or stream.</summary>
        public Guid Id { get; set; }
        /// <summary>Gets or sets the server-side password change workflow state.</summary>
        public string State { get; set; } = "";
        /// <summary>Gets or sets the device whose authentication password will be changed.</summary>
        public GXDevice Device { get; set; } = new();
        /// <summary>Gets or sets the association object whose authentication secret is updated.</summary>
        public string AssociationLogicalName { get; set; } = "";
        /// <summary>Gets or sets the secret used to establish the current device association.</summary>
        public byte[] CurrentPassword { get; set; } = [];
        /// <summary>Gets or sets the replacement authentication secret written to the device.</summary>
        public byte[] NewPassword { get; set; } = [];
        /// <inheritdoc/>
        public void Dispose()
        {
            CryptographicOperations.ZeroMemory(CurrentPassword);
            CryptographicOperations.ZeroMemory(NewPassword);
        }
    }

    /// <summary>Creates a diagnostic explaining unsupported or malformed password change work.</summary>
    /// <returns>An exception containing the safe password-change failure diagnostic.</returns>
    private static InvalidOperationException PasswordChangeError() => new InvalidOperationException(
        "Password change failed. Retry and verify the operation before ordinary meter work. No password material is included in this report.");

    /// <summary>Identifies password change JSON without parsing ordinary XML write values.</summary>
    /// <param name="task">Task whose operation kind and serialized data are inspected.</param>
    /// <param name="id">Receives the password change identifier when one is present.</param>
    /// <returns>Whether the task writes a JSON object containing a nonempty password change identifier.</returns>
    private static bool IsPasswordChange(GXTask task, out Guid id)
    {
        id = Guid.Empty;
        if (task.TaskType != TaskType.Write || string.IsNullOrWhiteSpace(task.Data) ||
            !task.Data.AsSpan().TrimStart().StartsWith("{", StringComparison.Ordinal))
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

    /// <summary>Creates the task lease identity sent with a password change request.</summary>
    /// <param name="task">Task or asynchronous operation whose execution is being coordinated.</param>
    /// <returns>The task identifier and lease identity required by the password-change server workflow.</returns>
    private object PasswordClaim(GXTask task) => new
    {
        TaskId = task.Id,
        AgentId = RuntimeOptions.Id,
        Start = task.Start ?? throw new InvalidOperationException("Password changes require a claimed task.")
    };

    /// <summary>Claims password change work and loads the credentials needed for execution.</summary>
    /// <param name="task">Task or asynchronous operation whose execution is being coordinated.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>Leased password-change work including the credentials needed by the device association.</returns>
    private async Task<PasswordWork> PasswordWorkAsync(GXTask task, CancellationToken token)
    {
        if (!IsPasswordChange(task, out var id))
        {
            throw new InvalidOperationException("Invalid password-change task.");
        }
        var work = await Client.PostAsJson<PasswordWork>($"api/PasswordChange/{id}/Work", PasswordClaim(task), token);
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

    /// <summary>Executes the leased password change and reports durable workflow transitions.</summary>
    /// <param name="device">Device associated with the task or diagnostic record.</param>
    /// <param name="reader">Protocol reader connected to the target device.</param>
    /// <param name="connection">Connection whose work or persisted records are processed.</param>
    /// <param name="task">Task or asynchronous operation whose execution is being coordinated.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Executes the leased password change and reports durable workflow transitions.</returns>
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
                using var response = await Client.PostAsJsonAsync($"api/PasswordChange/{work.Id}/Writing", PasswordClaim(task), token);
                await response.EnsureSuccessAsync(token);
            },
            () =>
            {
                token.ThrowIfCancellationRequested();
                var association = new GXDLMSAssociationLogicalName(work.AssociationLogicalName) { Secret = [.. work.NewPassword] };
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
                using var response = await Client.PostAsJsonAsync($"api/PasswordChange/{work.Id}/Verified", PasswordClaim(task), token);
                await response.EnsureSuccessAsync(token);
            });
    }
}