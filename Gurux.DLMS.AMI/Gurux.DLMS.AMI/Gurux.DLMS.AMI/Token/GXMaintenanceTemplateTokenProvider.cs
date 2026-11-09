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

using System.Globalization;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Server.Token;

/// <summary>Offers maintenance schedule tokens only within the maintenance page context.</summary>
public sealed class GXMaintenanceTemplateTokenProvider(IConfigurationRepository configurations) : IGXAmiTemplateTokenProvider
{
    private static bool IsMaintenance(AmiTemplateTokenContext context) => string.Equals(context.Page, "maintenance", StringComparison.OrdinalIgnoreCase);
    private static string Format(DateTimeOffset value, CultureInfo culture) => value.ToString("g", culture) + " " + value.ToString("zzz", culture);

    /// <inheritdoc />
    public Task<IReadOnlyList<AmiTemplateTokenDefinition>> GetDefinitionsAsync(AmiTemplateTokenContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsMaintenance(context))
            {
            return Task.FromResult<IReadOnlyList<AmiTemplateTokenDefinition>>([]);
        }
        return Task.FromResult<IReadOnlyList<AmiTemplateTokenDefinition>>([
            new() { Name = "maintenance:start", Description = "Maintenance start time", ExampleValue = Format(new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero), context.Culture) },
            new() { Name = "maintenance:end", Description = "Maintenance end time", ExampleValue = Format(new(2026, 1, 1, 14, 0, 0, TimeSpan.Zero), context.Culture) }
        ]);
    }

    /// <inheritdoc />
    public async Task<string?> ResolveAsync(string name, AmiTemplateTokenContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsMaintenance(context) || name is not ("maintenance:start" or "maintenance:end"))
            {
            return null;
        }
        var values = context.Values;
        if (values == null)
        {
            var stored = await configurations.ListAsync(context.User,
                new ListConfiguration { Filter = new GXConfiguration { Name = "maintenance" } }, null, cancellationToken);
            var settings = stored.FirstOrDefault()?.Settings;
            values = (string.IsNullOrEmpty(settings) ? new MaintenanceSettings() : JsonSerializer.Deserialize<MaintenanceSettings>(settings) ?? new()).GetTokenValues();
        }
        if (!values.TryGetValue(name, out var value) || string.IsNullOrEmpty(value))
            {
            return string.Empty;
        }
        if (!DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var instant))
            {
            throw new ArgumentException("Maintenance token values must be ISO 8601 timestamps.");
        }
        return Format(instant, context.Culture);
    }
}
