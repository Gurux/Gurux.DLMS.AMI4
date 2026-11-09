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

using Gurux.DLMS.AMI.Server;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI
{
    internal enum CliCommand
    {
        None,
        Init,
        Reset,
        Update
    }

    internal class GXSettings
    {
        internal static CliCommand GetCliCommand(string[] args, out string[] hostArgs)
        {
            CliCommand command = CliCommand.None;
            List<string> remaining = [];
            foreach (string arg in args)
            {
                if (string.Equals(arg, "Reset", StringComparison.OrdinalIgnoreCase))
                {
                    command = CliCommand.Reset;
                    continue;
                }
                if (string.Equals(arg, "Init", StringComparison.OrdinalIgnoreCase))
                {
                    command = CliCommand.Init;
                    continue;
                }
                if (string.Equals(arg, "Update", StringComparison.OrdinalIgnoreCase))
                {
                    command = CliCommand.Update;
                    continue;
                }
                remaining.Add(arg);
            }
            hostArgs = [.. remaining];
            return command;
        }

        internal static async Task<bool> TryHandleCliCommandAsync(GXDbConnection? connection,
            CliCommand cliCommand, ILogger<GXConfiguration> logger)
        {
            if (cliCommand == CliCommand.None)
            {
                return false;
            }
            ArgumentNullException.ThrowIfNull(connection);
            switch (cliCommand)
            {
                case CliCommand.Init:
                    logger.LogInformation("Initializing database.");
                    await ServerSettings.InitializeDatabaseAsync(connection, logger);
                    logger.LogInformation("Database initialization completed.");
                    return true;
                case CliCommand.Reset:
                    logger.LogInformation("Resetting database.");
                    ServerSettings.DropTables(connection, logger);
                    logger.LogInformation("Initializing database.");
                    await ServerSettings.InitializeDatabaseAsync(connection, logger);
                    logger.LogInformation("Database reset completed.");
                    return true;
                case CliCommand.Update:
                    logger.LogInformation("Updating database.");
                    ServerSettings.UpdateDatabase(connection, logger);
                    logger.LogInformation("Database update completed.");
                    return true;
                default:
                    return false;
            }
        }
    }
}
