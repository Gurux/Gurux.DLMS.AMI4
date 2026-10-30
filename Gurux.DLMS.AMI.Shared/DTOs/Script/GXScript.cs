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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Script
{
    /// <summary>Source code and metadata for an AMI script, including its exposed methods.</summary>
    [Description("Source code and metadata for an AMI script, including its exposed methods.")]
    public class GXScript : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXScript.</summary>
        [Description("Initializes a new instance of GXScript.")]
        public GXScript()
        {
        }

        /// <summary>Initializes a new instance of GXScript.</summary>
        /// <remarks>
        /// This constructor is called when a new script is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Script name.</param>
        [Description("Initializes a new instance of GXScript.")]
        public GXScript(string? name)
        {
            Name = name;
            ServerSide = true;
            ScriptGroups = [];
            Workflows = [];
            Methods = [];
            Logs = [];
            Properties = [];
            TraceLevel = System.Diagnostics.TraceLevel.Error;
        }

        /// <summary>
        /// Script identifier.
        /// </summary>
        [Description("Script identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>Whether the script is enabled.</summary>
        [Description("Whether the script is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>Additional access policies; empty retains existing permissions, otherwise any assigned policy must pass.</summary>
        [Description("Additional access policies; empty retains existing permissions, otherwise any assigned policy must pass.")]
        [DataMember, Ignore(IgnoreType.Db)]
        public List<string>? Policies { get; set; }

        /// <summary>
        /// Script properties.
        /// </summary>
        [Description("Script properties.")]
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXScriptProperty>? Properties { get; set; }

        /// <summary>
        /// Script name.
        /// </summary>
        [Description("Script name.")]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Script description.
        /// </summary>
        [Description("Script description.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        [StringLength(256)]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description { get; set; }

        /// <summary>
        /// Script language.
        /// </summary>
        [Description("Script language.")]
        public ScriptLanguage Language { get; set; }

        /// <summary>
        /// Source code of the script.
        /// </summary>
        [Description("Source code of the script.")]
        [DataMember]
        public string? SourceCode { get; set; }

        /// <summary>Stable identifier of the catalog template, or null for an independent script.</summary>
        [Description("Stable identifier of the catalog template, or null for an independent script.")]
        [DataMember, StringLength(200), Filter(FilterType.Exact), Index(false)]
        public string? CatalogId { get; set; }

        /// <summary>Installed immutable template version; local source edits do not change this value.</summary>
        [Description("Installed immutable template version; local source edits do not change this value.")]
        [DataMember, StringLength(64)]
        public string? CatalogVersion { get; set; }

        /// <summary>SHA-256 of the verified downloaded template bytes.</summary>
        [Description("SHA-256 of the verified downloaded template bytes.")]
        [DataMember, StringLength(64)]
        public string? CatalogSha256 { get; set; }

        /// <summary>Installed source baseline used for local-change detection, owned by the catalog installer.</summary>
        [Description("Installed source baseline used for local-change detection, owned by the catalog installer.")]
        [JsonIgnore]
        public string? CatalogSourceCode { get; set; }
        /// <summary>
        /// Comma separated list from additional namespaces.
        /// </summary>
        [Description("Comma separated list from additional namespaces.")]
        [DataMember]
        public string? Namespaces { get; set; }

        /// <summary>Whether the script runs on the server rather than in the client.</summary>
        /// <remarks>
        /// Usually the script is run on the server side, but sometimes there are cases that doesn't
        /// need the server resources and it's possible to run the script on the client side.
        /// </remarks>
        [Description("Whether the script runs on the server rather than in the client.")]
        public bool ServerSide { get; set; } = true;

        /// <summary>
        /// Generated byte assembly.
        /// </summary>
        [Description("Generated byte assembly.")]
        [DataMember]
        public byte[]? ByteAssembly { get; set; }

        /// <summary>
        /// The creator of the script.
        /// </summary>
        [Description("The creator of the script.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Gets or sets who can view the schedule.
        /// </summary>
        /// <remarks>
        /// Modification, deletion and operation permissions are evaluated separately.
        /// </remarks>
        [Description("Gets or sets who can view the schedule.")]
        [DataMember]
        [DefaultValue(ResourceVisibility.Inherit)]
        [IsRequired]
        public ResourceVisibility? AccessVisibility { get; set; }

        /// <summary>
        /// The module that created this script.
        /// </summary>
        [Description("The module that created this script.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        //ForeignKeyDelete is None because creator of the module is causing multiple cascade paths error in MSSQL.
        public GXModule? Module { get; set; }

        /// <summary>Methods exposed by this script for invocation by other resources.</summary>
        [Description("Methods exposed by this script for invocation by other resources.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        public List<GXScriptMethod>? Methods { get; set; }

        /// <summary>Script groups containing this script.</summary>
        [Description("Script groups containing this script.")]
        [DataMember]
        [ForeignKey(typeof(GXScriptGroup), typeof(GXScriptGroupScript))]
        [Filter(FilterType.Contains)]
        public List<GXScriptGroup>? ScriptGroups { get; set; }

        /// <summary>
        /// List of workflows where this script belongs.
        /// </summary>
        [Description("List of workflows where this script belongs.")]
        [DataMember]
        [ForeignKey(typeof(GXWorkflow), typeof(GXWorkflowScriptMethod))]
        [Filter(FilterType.Contains)]
        public List<GXWorkflow>? Workflows { get; set; }

        /// <summary>
        /// Script logs.
        /// </summary>
        [Description("Script logs.")]
        [DataMember, Ignore(IgnoreType.Db)]
        [Filter(FilterType.Contains)]
        public List<GXLog>? Logs { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the script was last updated.</summary>
        [Description("Time when the script was last updated.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }


        /// <summary>Trace level used for communication diagnostics.</summary>
        [DataMember]
        [DefaultValue(System.Diagnostics.TraceLevel.Error)]
        [Description("Trace level used for communication diagnostics.")]
        [IsRequired]
        public TraceLevel? TraceLevel { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
        [Description("Initializes the creation timestamp before insertion if it has not been set.")]
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
        [Description("Updates the last-modified timestamp before persisting changes.")]
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
