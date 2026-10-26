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

using Gurux.DLMS.AMI.Shared.DTOs.Log;
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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Script
{
    /// <summary>Source code and metadata for an AMI script, including its exposed methods.</summary>
    public class GXScript : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXScript()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new script is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Script name.</param>
        public GXScript(string? name)
        {
            Name = name;
            ServerSide = true;
            ScriptGroups = new List<GXScriptGroup>();
            Workflows = new List<GXWorkflow>();
            Methods = new List<GXScriptMethod>();
            Logs = new List<GXLog>();
            TraceLevel = System.Diagnostics.TraceLevel.Error;
        }

        /// <summary>
        /// Script identifier.
        /// </summary>
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Is script enabled.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }


        /// <summary>
        /// Script name.
        /// </summary>
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Script description.
        /// </summary>
        [DataMember]
        [Filter(FilterType.Contains)]
        [StringLength(256)]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description { get; set; }

        /// <summary>
        /// Script language.
        /// </summary>
        public ScriptLanguage Language { get; set; }

        /// <summary>
        /// Source code of the script.
        /// </summary>
        [DataMember]
        public string? SourceCode { get; set; }

        /// <summary>Stable identifier of the catalog template, or null for an independent script.</summary>
        [DataMember, StringLength(200), Filter(FilterType.Exact), Index(false)]
        public string? CatalogId { get; set; }

        /// <summary>Installed immutable template version; local source edits do not change this value.</summary>
        [DataMember, StringLength(64)]
        public string? CatalogVersion { get; set; }

        /// <summary>SHA-256 of the verified downloaded template bytes.</summary>
        [DataMember, StringLength(64)]
        public string? CatalogSha256 { get; set; }

        /// <summary>Installed source baseline used for local-change detection, owned by the catalog installer.</summary>
        [JsonIgnore]
        public string? CatalogSourceCode { get; set; }
        /// <summary>
        /// Comma separated list from additional namespaces.
        /// </summary>
        [DataMember]
        public string? Namespaces { get; set; }

        /// <summary>
        /// Is script run on server or in the client end.
        /// </summary>
        /// <remarks>
        /// Usually the script is run on the server side, but sometimes there are cases that doesn't
        /// need the server resources and it's possible to run the script on the client side.
        /// </remarks>
        public bool ServerSide { get; set; } = true;

        /// <summary>
        /// Generated byte assembly.
        /// </summary>
        [DataMember]
        public byte[]? ByteAssembly { get; set; }

        /// <summary>
        /// The creator of the script.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// The module that created this script.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        //ForeignKeyDelete is None because creator of the module is causing multiple cascade paths error in MSSQL.
        public GXModule? Module { get; set; }

        /// <summary>Methods exposed by this script for invocation by other resources.</summary>
        [DataMember]
        [Filter(FilterType.Contains)]
        public List<GXScriptMethod>? Methods { get; set; }

        /// <summary>Script groups containing this script.</summary>
        [DataMember]
        [ForeignKey(typeof(GXScriptGroup), typeof(GXScriptGroupScript))]
        [Filter(FilterType.Contains)]
        public List<GXScriptGroup>? ScriptGroups { get; set; }

        /// <summary>
        /// List of workflows where this script belongs.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXWorkflow), typeof(GXWorkflowScriptMethod))]
        [Filter(FilterType.Contains)]
        public List<GXWorkflow>? Workflows { get; set; }

        /// <summary>
        /// Script logs.
        /// </summary>
        [DataMember, Ignore(IgnoreType.Db)]
        [Filter(FilterType.Contains)]
        public List<GXLog>? Logs { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// When the Script is updated for the last time.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>
        /// Remove time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }


        /// <summary>
        /// Used trace level.
        /// </summary>
        [DataMember]
        [DefaultValue(System.Diagnostics.TraceLevel.Error)]
        [Description("Used trace level.")]
        [IsRequired]
        public TraceLevel? TraceLevel { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == null)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Updates the last-modified timestamp before persisting changes.
        /// </summary>
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXScript);
        }
    }
}
