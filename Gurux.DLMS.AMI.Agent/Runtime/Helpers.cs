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

using Gurux.DLMS.AMI.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.Text;

namespace Gurux.DLMS.AMI.Agent.Worker
{
    static class Helpers
    {
        /// <inheritdoc />
        public static byte[]? Compile(
            string script,
            string? additionalNamespaces)
        {
            if (string.IsNullOrEmpty(script))
            {
                throw new ArgumentNullException(nameof(script), "Invalid script.");
            }
            List<GXScriptException> errors = [];
            StringBuilder sb = new();
            List<string> namespaces =
            [
                "System",
                "System.Linq",
                "System.Linq.Expressions",
                "System.Collections.Generic",
                "Gurux.Common",
                "Gurux.DLMS.Enums",
                "Gurux.DLMS.AMI.Script",
                "Gurux.DLMS.AMI.Shared.DIs",
                "Gurux.DLMS.AMI.Shared.DTOs",
                "Gurux.DLMS.AMI.Shared.DTOs.Enums",
                "Gurux.DLMS.AMI.Shared.DTOs.Agent",
                "Gurux.DLMS.AMI.Shared.DTOs.Authentication",
                "Gurux.DLMS.AMI.Shared.DTOs.Content",
                "Gurux.DLMS.AMI.Shared.DTOs.ComponentView",
                "Gurux.DLMS.AMI.Shared.DTOs.Device",
                "Gurux.DLMS.AMI.Shared.DTOs.Gateway",
                "Gurux.DLMS.AMI.Shared.DTOs.KeyManagement",
                "Gurux.DLMS.AMI.Shared.DTOs.Manufacturer",
                "Gurux.DLMS.AMI.Shared.DTOs.Module",
                "Gurux.DLMS.AMI.Shared.DTOs.Schedule",
                "Gurux.DLMS.AMI.Shared.DTOs.Script",
                "Gurux.DLMS.AMI.Shared.DTOs.Subtotal",
                "Gurux.DLMS.AMI.Shared.DTOs.Report",
                "Gurux.DLMS.AMI.Shared.DTOs.Trigger",
                "Gurux.DLMS.AMI.Shared.DTOs.User",
                "Gurux.DLMS.AMI.Shared.DTOs.Workflow",
            ];
            if (!string.IsNullOrEmpty(additionalNamespaces))
            {
                namespaces.AddRange(additionalNamespaces.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }
            foreach (string it in namespaces)
            {
                if (!string.IsNullOrEmpty(it))
                {
                    sb.AppendLine("using " + it + ";");
                }
            }
            sb.AppendLine("namespace Gurux.DLMS.AMI.GeneratedScript");
            sb.AppendLine("{");
            sb.AppendLine("public class GXAmiScript");
            sb.AppendLine("{");
            sb.AppendLine("public GXAmiScript(IGXAmi ami)");
            sb.AppendLine("{");
            sb.AppendLine("Ami = ami;");
            sb.AppendLine("}");
            sb.AppendLine("public IGXAmi Ami{get;private set;}");
            sb.AppendLine(script);
            sb.AppendLine("}");
            sb.AppendLine("}");

            _ = DateTime.Now;
            return GXAmiScript.Generate(ScriptLanguage.CSharp, Guid.NewGuid().ToString(), sb.ToString(), errors, null);
        }
    }
}
