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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Gurux.DLMS.AMI.Agent;

/// <summary>Writes console messages without logger categories or event identifiers.</summary>
/// <param name="options">Validated configuration used by the runtime operation.</param>
internal sealed class GXAgentConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> options)
    : ConsoleFormatter(FormatterName)
{
    /// <summary>Configuration name used to select the agent console formatter.</summary>
    internal const string FormatterName = "agent";

    /// <inheritdoc/>
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        string? message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (message == null && logEntry.Exception == null)
        {
            return;
        }
        var settings = options.CurrentValue;
        if (settings.TimestampFormat != null)
        {
            var now = settings.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
            textWriter.Write(now.ToString(settings.TimestampFormat, CultureInfo.InvariantCulture));
        }
        textWriter.Write(logEntry.LogLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => logEntry.LogLevel.ToString()
        });
        textWriter.Write(": ");
        if (settings.IncludeScopes && scopeProvider != null)
        {
            scopeProvider.ForEachScope((scope, writer) =>
            {
                writer.Write(scope);
                writer.Write(" => ");
            }, textWriter);
        }
        textWriter.WriteLine(message);
        if (logEntry.Exception != null)
        {
            textWriter.WriteLine(logEntry.Exception);
        }
    }
}
