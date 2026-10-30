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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested module information.</summary>
    [Description("Response containing the requested module information.")]
    public class GetModuleResponse
    {
        /// <summary>
        /// Module information.
        /// </summary>        
        [Description("Module information.")]
        [ExcludeOpenApi(typeof(GXModule), nameof(GXModule.UserGroups),
            nameof(GXModule.ModuleGroups), nameof(GXModule.Logs),
            nameof(GXModule.Versions), nameof(GXModule.Scripts),
            nameof(GXModule.Assemblies), nameof(GXModule.DeviceParameters),
            nameof(GXModule.ObjectParameters), nameof(GXModule.AttributeParameters),
            nameof(GXModule.Schedules), nameof(GXModule.Workflows),
            nameof(GXModule.Creator))]
        public GXModule? Item { get; set; }
    }


    /// <summary>Response to adding modules.</summary>
    [Description("Response to adding modules.")]
    [DataContract]
    public class AddModuleResponse
    {
        /// <summary>
        /// Added modules.
        /// </summary>
        [Description("Added modules.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXModule), nameof(GXModule.Logs)
            , nameof(GXModule.ObjectParameters), nameof(GXModule.DeviceParameters)
            , nameof(GXModule.AttributeParameters))]
        [IncludeOpenApi(typeof(GXModuleAssembly), nameof(GXModuleAssembly.Id))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXSchedule.Id))]
        [IncludeOpenApi(typeof(GXModuleGroup), nameof(GXModuleGroup.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXModuleVersion), nameof(GXModuleVersion.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [ExcludeOpenApi(typeof(GXDeviceProperty), nameof(GXDeviceProperty.Module))]
        [ExcludeOpenApi(typeof(GXObjectParameter), nameof(GXObjectParameter.Module))]
        [ExcludeOpenApi(typeof(GXAttributeProperty), nameof(GXAttributeProperty.Module))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]

        public IEnumerable<GXModule> Modules { get; set; } = default!;

        /// <summary>
        /// Is restart required.
        /// </summary>
        [Description("Is restart required.")]
        [DataMember]
        public bool Restart { get; set; }
    }


    /// <summary>
    /// Install new module.
    /// </summary>
    [Description("Install new module.")]
    [DataContract]
    public class InstallModule
    {
        /// <summary>Gets or sets the catalog version identifier to install, if specified.</summary>
        [Description("Gets or sets the catalog version identifier to install, if specified.")]
        [DataMember]
        public Guid? VersionId { get; set; }

        /// <summary>
        /// Installed module.
        /// </summary>
        [Description("Installed module.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        public GXModule Module { get; set; } = default!;
    }

    /// <summary>
    /// Install module response.
    /// </summary>
    [Description("Install module response.")]
    [DataContract]
    public class InstallModuleResponse
    {
        /// <summary>
        /// Is restart required.
        /// </summary>
        [Description("Is restart required.")]
        [DataMember]
        public bool Restart { get; set; }
    }


    /// <summary>Requests changes to the specified modules.</summary>
    [Description("Requests changes to the specified modules.")]
    [DataContract]
    public class UpdateModule
    {
        /// <summary>
        /// Module to update.
        /// </summary>
        [Description("Module to update.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXModule), nameof(GXModule.Logs)
            , nameof(GXModule.ObjectParameters), nameof(GXModule.DeviceParameters)
            , nameof(GXModule.AttributeParameters))]
        [IncludeOpenApi(typeof(GXModuleAssembly), nameof(GXModuleAssembly.Id))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXSchedule.Id))]
        [IncludeOpenApi(typeof(GXModuleGroup), nameof(GXModuleGroup.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXModuleVersion), nameof(GXModuleVersion.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [ExcludeOpenApi(typeof(GXDeviceProperty), nameof(GXDeviceProperty.Module))]
        [ExcludeOpenApi(typeof(GXObjectParameter), nameof(GXObjectParameter.Module))]
        [ExcludeOpenApi(typeof(GXAttributeProperty), nameof(GXAttributeProperty.Module))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeOpenApi(typeof(GXModule), nameof(GXModule.CreationTime), nameof(GXModule.Updated))]
        public GXModule Module { get; set; } = default!;
    }

    /// <summary>Response to updating the specified modules.</summary>
    [Description("Response to updating the specified modules.")]
    [DataContract]
    public class UpdateModuleResponse
    {
        /// <summary>
        /// Is restart required.
        /// </summary>
        [Description("Is restart required.")]
        [DataMember]
        public bool Restart { get; set; }
    }

    /// <summary>Requests a list of modules.</summary>
    [Description("Requests a list of modules.")]
    [DataContract]
    public class ListModules : IGXRequest<ListModulesResponse>
    {
        /// <summary>Gets or sets whether to list modules from the update catalog only.</summary>
        [Description("Gets or sets whether to list modules from the update catalog only.")]
        [DataMember]
        public bool CatalogOnly { get; set; }
        /// <summary>Gets or sets whether to include prerelease module versions.</summary>
        [Description("Gets or sets whether to include prerelease module versions.")]
        [DataMember]
        public bool IncludePrereleases { get; set; }

        /// <summary>
        /// Optional criteria used to filter modules.
        /// </summary>
        [Description("Optional criteria used to filter modules.")]
        [ExcludeOpenApi(typeof(GXModule), nameof(GXModule.UserGroups),
            nameof(GXModule.ModuleGroups), nameof(GXModule.Logs),
            nameof(GXModule.Versions), nameof(GXModule.Scripts),
            nameof(GXModule.Assemblies), nameof(GXModule.DeviceParameters),
            nameof(GXModule.ObjectParameters), nameof(GXModule.AttributeParameters),
            nameof(GXModule.Schedules), nameof(GXModule.Workflows),
            nameof(GXModule.Creator))]
        [ExcludeOpenApi(typeof(GXModule),
            nameof(GXModule.Policies), nameof(GXModule.Icon), nameof(GXModule.RuntimeState),
            nameof(GXModule.Path), nameof(GXModule.Properties), nameof(GXModule.TraceLevel),
            nameof(GXModule.ConcurrencyStamp), nameof(GXModule.ReleaseVersion))]
        public GXModule? Filter { get; set; }

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
        /// Admin user can access modules from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all modules are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access modules from all users.")]
        public bool AllUsers { get; set; }

        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of modules to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of modules to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

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
        public IEnumerable<string>? Included { get; set; }
        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<string>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested modules.</summary>
    [Description("Response containing the requested modules.")]
    [DataContract]
    public class ListModulesResponse
    {
        /// <summary>Gets or sets the available catalog releases grouped by module identifier.</summary>
        [Description("Gets or sets the available catalog releases grouped by module identifier.")]
        [DataMember] public Dictionary<string, List<ModuleReleaseInfo>> CatalogVersions { get; set; } = [];

        /// <summary>
        /// Installed modules.
        /// </summary>
        [DataMember]
        [Description("Installed modules.")]
        [ExcludeOpenApi(typeof(GXModule), nameof(GXModule.UserGroups),
            nameof(GXModule.ModuleGroups), nameof(GXModule.Logs),
            nameof(GXModule.Versions), nameof(GXModule.Scripts),
            nameof(GXModule.Assemblies), nameof(GXModule.DeviceParameters),
            nameof(GXModule.ObjectParameters), nameof(GXModule.AttributeParameters),
            nameof(GXModule.Schedules), nameof(GXModule.Workflows),
            nameof(GXModule.Creator))]
        public IEnumerable<GXModule>? Modules { get; set; }

        /// <summary>
        /// Amount of the modules.
        /// </summary>
        [DataMember]
        [Description("Amount of the modules.")]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected modules.</summary>
    [DataContract]
    [Description("Requests deletion of the selected modules.")]
    public class RemoveModule
    {
        /// <summary>
        /// Data removal options.
        /// </summary>
        [Description("Data removal options.")]
        [DataMember]
        public byte DataRemoval { get; set; }

        /// <summary>
        /// Removed modules.
        /// </summary>
        [Description("Removed modules.")]
        [DataMember]
        public IEnumerable<string> Modules { get; set; } = default!;
    }

    /// <summary>Response to deleting the selected modules.</summary>
    [Description("Response to deleting the selected modules.")]
    [DataContract]
    public class RemoveModuleResponse
    {
        /// <summary>Gets or sets the removal outcome for each requested module.</summary>
        [Description("Gets or sets the removal outcome for each requested module.")]
        [DataMember] public List<ModuleRemovalResult> Results { get; set; } = [];

        /// <summary>
        /// Is restart required to remove the module.
        /// </summary>
        [Description("Is restart required to remove the module.")]
        [DataMember]
        public bool Restart { get; set; }
    }

    /// <summary>
    /// Check if there are new module versions available.
    /// </summary>
    [Description("Check if there are new module versions available.")]
    [DataContract]
    public class CheckModule : IGXRequest<CheckModuleResponse>
    {
    }

    /// <summary>
    /// Check module versions reply.
    /// </summary>
    [Description("Check module versions reply.")]
    [DataContract]
    public class CheckModuleResponse
    {
        /// <summary>The completed background execution identifier.</summary>
        [Description("The completed background execution identifier.")]
        [DataMember]
        public Guid RunId { get; set; }
    }

    /// <summary>Reports the removal outcome for a module.</summary>
    /// <param name="ModuleId">The identifier of the module requested for removal.</param>
    /// <param name="Success">Whether the module was removed successfully.</param>
    /// <param name="Error">The error description if removal failed.</param>
    [Description("Reports the removal outcome for a module.")]
    public sealed record ModuleRemovalResult(string ModuleId, bool Success, string? Error = null);

    /// <summary>Describes a catalog release and whether it can be installed.</summary>
    /// <param name="Id">The catalog release identifier.</param>
    /// <param name="Number">The release version number.</param>
    /// <param name="Prerelease">Whether the release is a prerelease.</param>
    /// <param name="Installable">Whether the release can be installed.</param>
    [Description("Describes a catalog release and whether it can be installed.")]
    public sealed record ModuleReleaseInfo(Guid Id, string Number, bool Prerelease, bool Installable);
}
