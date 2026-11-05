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

using Gurux.Service.Orm.Common;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested configuration information.</summary>
    [Description("Response containing the requested configuration information.")]
    public class GetConfigurationResponse
    {
        /// <summary>
        /// Configuration information.
        /// </summary>        
        [Description("Configuration information.")]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [ExcludeOpenApi(typeof(GXLocalizedResource),
            nameof(GXLocalizedResource.Language))]
        public GXConfiguration? Item { get; set; }
    }

    /// <summary>Requests a list of configurations.</summary>
    [Description("Requests a list of configurations.")]
    [DataContract]
    public class ListConfiguration : IGXRequest<ListConfigurationResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of configuration items to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of configuration items to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter system info.
        /// </summary>
        [Description("Optional criteria used to filter system info.")]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [ExcludeOpenApi(typeof(GXLocalizedResource),
             nameof(GXLocalizedResource.Language))]
        [ExcludeOpenApi(typeof(GXConfiguration),
            nameof(GXConfiguration.Settings), nameof(GXConfiguration.Enabled), nameof(GXConfiguration.ConcurrencyStamp))]
        public GXConfiguration? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

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
    }

    /// <summary>Response containing the requested configurations.</summary>
    [Description("Response containing the requested configurations.")]
    [DataContract]
    public class ListConfigurationResponse
    {
        /// <summary>
        /// List of configuration settings.
        /// </summary>
        [Description("List of configuration settings.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [ExcludeOpenApi(typeof(GXLocalizedResource),
            nameof(GXLocalizedResource.Language))]
        public IEnumerable<GXConfiguration>? Configurations { get; set; }

        /// <summary>
        /// Total number of matching configuration items before paging.
        /// </summary>
        [Description("Total number of matching configuration items before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>
    /// Update configuration values.
    /// </summary>
    [Description("Update configuration values.")]
    [DataContract]
    public class ConfigurationUpdate : IGXRequest<ConfigurationUpdateResponse>
    {
        /// <summary>
        /// Updated configurations.
        /// </summary>
        [Description("Updated configurations.")]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [ExcludeOpenApi(typeof(GXConfiguration), nameof(GXConfiguration.CreationTime), nameof(GXConfiguration.Updated))]
        public IEnumerable<GXConfiguration>? Configurations { get; set; }
    }

    /// <summary>
    ///Update configurations reply.
    /// </summary>
    [DataContract]
    [Description("Update configurations reply.")]
    public class ConfigurationUpdateResponse
    {
    }

    /// <summary>
    /// Requests execution of scheduled cron work.
    /// </summary>
    [Description("Requests execution of scheduled cron work.")]
    [DataContract]
    public class ConfigurationRunCron : IGXRequest<ConfigurationRunCronResponse>
    {
    }

    /// <summary>
    /// Run cron reply.
    /// </summary>
    [Description("Run cron reply.")]
    [DataContract]
    public class ConfigurationRunCronResponse
    {
    }

    /// <summary>
    /// Run prune reply.
    /// </summary>
    [Description("Run prune reply.")]
    [DataContract]
    public class ConfigurationRunPruneResponse
    {
    }

    /// <summary>
    /// Run prune.
    /// </summary>
    [Description("Run prune.")]
    [DataContract]
    public class ConfigurationRunPrune : IGXRequest<ConfigurationRunPruneResponse>
    {
    }

    /// <summary>
    /// Requests information about assemblies loaded by the server.
    /// </summary>
    [Description("Requests information about assemblies loaded by the server.")]
    [DataContract]
    public class LoadedAssemblies : IGXRequest<LoadedAssembliesResponse>
    {
        /// <summary>
        /// Optional criteria used to filter assemblies.
        /// </summary>
        [Description("Optional criteria used to filter assemblies.")]
        public GXAssembly? Filter { get; set; }
    }

    /// <summary>
    /// Assemblies loaded by the server.
    /// </summary>
    [Description("Assemblies loaded by the server.")]
    [DataContract]
    public class LoadedAssembliesResponse
    {
        /// <summary>
        /// Loaded assemblies.
        /// </summary>
        [Description("Loaded assemblies.")]
        public IEnumerable<GXAssembly>? Assemblies { get; set; }
    }

    /// <summary>
    /// Stop application request.
    /// </summary>
    [Description("Stop application request.")]
    [DataContract]
    public class StopApplicationRequest
    {
    }

    /// <summary>
    /// Resets configuration settings to built-in defaults.
    /// </summary>
    [Description("Resets configuration settings to built-in defaults.")]
    [DataContract]
    public class RegenerateConfiguration : IGXRequest<RegenerateConfigurationResponse>
    {
        /// <summary>Existing configuration names or identifier strings to reset to built-in defaults. Null or empty adds only missing defaults.</summary>
        [Description("Existing configuration names or identifier strings to reset to built-in defaults. Null or empty adds only missing defaults.")]
        [DataMember]
        public IEnumerable<string>? Ids { get; set; }
    }

    /// <summary>
    /// Response to resetting configuration settings to their built-in defaults.
    /// </summary>
    [Description("Response to resetting configuration settings to their built-in defaults.")]
    [DataContract]
    public class RegenerateConfigurationResponse
    {
    }
}
