//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd   
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.DLMS.AMI.Script;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.Net;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Gurux.DLMS.AMI.Agent.Worker
{
    static class Helpers
    {
        public static async Task<T?> GetAsync<T>(string url)
        {
            HttpResponseMessage response = await GXAgentWorker.client.GetAsync(url);
            await AMI.Shared.Helpers.ValidateStatusCode(response, default);
            return await response.Content.ReadFromJsonAsync<T>();
        }

        /// <inheritdoc />
        public static byte[]? Compile(
            string script,
            string? additionalNamespaces)
        {
            if (string.IsNullOrEmpty(script))
            {
                throw new ArgumentNullException("Invalid script.");
            }
            List<GXScriptException> errors = new();
            StringBuilder sb = new();
            List<string> namespaces = new List<string>
            {
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
            };
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
            DateTime start = DateTime.Now;
            return GXAmiScript.Generate(ScriptLanguage.CSharp, Guid.NewGuid().ToString(), sb.ToString(), errors, null);
        }
    }
}
