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
using System.Runtime.Serialization;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.DTOs.Script;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested script information.</summary>
    [Description("Response containing the requested script information.")]
    public class GetScriptResponse
    {
        /// <summary>
        /// Script information.
        /// </summary>
        [Description("Script information.")]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id),
            nameof(GXScriptGroup.Name))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id),
            nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id),
            nameof(GXScriptMethod.Name))]
        [ExcludeOpenApi(typeof(GXScript), nameof(GXScript.Logs),
            nameof(GXScript.ByteAssembly), nameof(GXScript.Module))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXScript Item { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }

    /// <summary>Requests a list of scripts.</summary>
    [Description("Requests a list of scripts.")]
    [DataContract]
    public class ListScripts : IGXRequest<ListScriptsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of scripts to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of scripts to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter scripts.
        /// </summary>
        [Description("Optional criteria used to filter scripts.")]
        [ExcludeOpenApi(typeof(GXScript),
        nameof(GXScript.ScriptGroups),
             nameof(GXScript.Workflows),
             nameof(GXScript.Methods),
             nameof(GXScript.Logs),
             nameof(GXScript.Creator),
             nameof(GXScript.Module))]
        [ExcludeOpenApi(typeof(GXScript),
            nameof(GXScript.Language), nameof(GXScript.SourceCode), nameof(GXScript.CatalogVersion),
            nameof(GXScript.CatalogSha256), nameof(GXScript.CatalogSourceCode), nameof(GXScript.Namespaces),
            nameof(GXScript.ServerSide), nameof(GXScript.ByteAssembly), nameof(GXScript.TraceLevel),
            nameof(GXScript.ConcurrencyStamp))]
        public GXScript? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>
        /// Admin user can access scripts from all users.
        /// </summary>
        /// <remarks>
        /// If true, scripts from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access scripts from all users.")]
        public bool AllUsers { get; set; }

        /// <summary>Additional related information to include in the response. Reserved for future use.</summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [Description("Additional related information to include in the response. Reserved for future use.")]
        public IEnumerable<string>? Select { get; set; }

        /// <summary>Name of the property to sort by. If omitted, the endpoint uses its default ordering.</summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        [Description("Name of the property to sort by. If omitted, the endpoint uses its default ordering.")]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        [Description("Whether to sort results in descending order.")]
        public bool Descending { get; set; }

        /// <summary>Identifiers to include in the results. A nonempty collection restricts results to these identifiers.</summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Identifiers to include in the results. A nonempty collection restricts results to these identifiers.")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<Guid>? Exclude { get; set; }
        /// <summary>
        /// Byte assembly is send for the client.
        /// </summary>
        [DataMember]
        [Description("Byte assembly is send for the client.")]
        public bool Assembly { get; set; }
    }

    /// <summary>Response containing the requested scripts.</summary>
    [DataContract]
    [Description("Response containing the requested scripts.")]
    public class ListScriptsResponse
    {
        /// <summary>
        /// List of script items.
        /// </summary>
        [Description("List of script items.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXScript),
        nameof(GXScript.ScriptGroups),
            nameof(GXScript.Workflows),
            nameof(GXScript.Methods),
            nameof(GXScript.Logs),
            nameof(GXScript.Module))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        public IEnumerable<GXScript>? Scripts { get; set; }

        /// <summary>
        /// Total number of matching scriptrs before paging.
        /// </summary>
        [DataMember]
        [Description("Total number of matching scriptrs before paging.")]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified scripts.</summary>
    [Description("Requests changes to the specified scripts.")]
    [DataContract]
    public class UpdateScript : IGXRequest<UpdateScriptResponse>
    {
        /// <summary>
        /// Scripts to update.
        /// </summary>
        [DataMember]
        [Description("Scripts to update.")]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXScript),
            nameof(GXScript.Workflows),
            nameof(GXScript.Methods),
            nameof(GXScript.Logs),
            nameof(GXScript.Creator),
            nameof(GXScript.Module))]
        [ExcludeOpenApi(typeof(GXScript), nameof(GXScript.CreationTime), nameof(GXScript.Updated))]
        public IEnumerable<GXScript> Scripts { get; set; } = default!;
    }

    /// <summary>Response to updating the specified scripts.</summary>
    [Description("Response to updating the specified scripts.")]
    [DataContract]
    public class UpdateScriptResponse
    {
        /// <summary>Identifiers of the added or updated script records.</summary>
        [DataMember]
        [Description("Identifiers of the added or updated script records.")]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected scripts.</summary>
    [Description("Requests deletion of the selected scripts.")]
    [DataContract]
    public class RemoveScript : IGXRequest<RemoveScriptResponse>
    {
        /// <summary>Identifiers of the script records to delete.</summary>
        [Description("Identifiers of the script records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected scripts.</summary>
    [Description("Response to deleting the selected scripts.")]
    [DataContract]
    public class RemoveScriptResponse
    {
    }

    /// <summary>
    /// Validate script.
    /// </summary>
    [Description("Validate script.")]
    [DataContract]
    public class ValidateScript : IGXRequest<ValidateScriptResponse>
    {
        /// <summary>
        /// Validated string.
        /// </summary>
        [Description("Validated string.")]
        public string Script { get; set; } = default!;

        /// <summary>
        /// Additional name spaces.
        /// </summary>
        [Description("Additional name spaces.")]
        public string? NameSpaces { get; set; }
    }

    /// <summary>Response containing the script validation result.</summary>
    [Description("Response containing the script validation result.")]
    [DataContract]
    public class ValidateScriptResponse
    {
        /// <summary>
        /// Script errors in JSON, or null if there are no errors.
        /// </summary>
        [Description("Script errors in JSON, or null if there are no errors.")]
        public string? Errors { get; set; }

        /// <summary>
        /// Compile time.
        /// </summary>
        [Description("Compile time.")]
        public int CompileTime { get; set; }
    }

    /// <summary>
    /// Run the script.
    /// </summary>
    [Description("Run the script.")]
    [DataContract]
    public class RunScript : IGXRequest<RunScriptResponse>
    {
        /// <summary>Script method identifier.</summary>
        [Description("Script method identifier.")]
        public Guid MethodId { get; set; }
    }

    /// <summary>
    /// Run script reply.
    /// </summary>
    [Description("Run script reply.")]
    [DataContract]
    public class RunScriptResponse
    {
        /// <summary>
        /// Result value.
        /// </summary>
        [Description("Result value.")]
        public object? Result { get; set; }
    }

    /// <summary>
    /// Rebuild the scripts.
    /// </summary>
    [Description("Rebuild the scripts.")]
    [DataContract]
    public class RebuildScript : IGXRequest<RebuildScriptResponse>
    {
        /// <summary>Rebuild script identifiers.</summary>
        [Description("Rebuild script identifiers.")]
        public IEnumerable<Guid>? Scripts { get; set; }
    }

    /// <summary>
    /// Rebuild the scripts reply.
    /// </summary>
    [Description("Rebuild the scripts reply.")]
    [DataContract]
    public class RebuildScriptResponse
    {
    }
}
