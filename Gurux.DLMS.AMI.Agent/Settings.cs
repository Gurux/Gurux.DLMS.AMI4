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

using Gurux.Common;

namespace Gurux.DLMS.AMI.Agent
{
    /// <summary>
    /// Contains command-line overrides for the agent host.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Gets or sets the AMI server address supplied with <c>-h</c>.
        /// </summary>
        /// <value>The address override, or <see langword="null"/> to use the host's configured address.</value>
        public string? Host { get; set; }
        /// <summary>
        /// Parses command-line options into the supplied settings instance.
        /// </summary>
        /// <param name="args">Command-line options, excluding the host's <c>Configure</c> command.</param>
        /// <param name="settings">The instance that receives parsed overrides.</param>
        /// <returns>Zero on success; one when an unsupported option causes help to be displayed.</returns>
        /// <remarks>This method parses options only. The host validates the resulting server address before use.</remarks>
        public static int GetParameters(string[] args, Settings settings)
        {
            List<GXCmdParameter> parameters = GXCommon.GetParameters(args, "h:");
            foreach (GXCmdParameter it in parameters)
            {
                switch (it.Tag)
                {
                    case 'h':
                        settings.Host = it.Value;
                        break;
                    default:
                        ShowHelp();
                        return 1;
                }
            }
            return 0;
        }

        static void ShowHelp()
        {
            Console.WriteLine("Gurux.DLMS.AMI.Agent is used to read DLMS devices.");
            Console.WriteLine("GuruxDlmsSample -h [Gurux.DLMS.AMI server address]");
            Console.WriteLine(" -h \t host name or IP address.");
        }
    }
}
