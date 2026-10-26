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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get configuration.
    /// </summary>
    public class GetConfigurationResponse
    {
        /// <summary>
        /// Configuration information.
        /// </summary>        
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [ExcludeOpenApi(typeof(GXLocalizedResource),
            nameof(GXLocalizedResource.Language))]
        public GXConfiguration? Item { get; set; }
    }

    /// <summary>
    /// Get site information.
    /// </summary>
    [DataContract]
    public class ListConfiguration : IGXRequest<ListConfigurationResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of configuration items to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter system info.
        /// </summary>
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [ExcludeOpenApi(typeof(GXLocalizedResource),
             nameof(GXLocalizedResource.Language))]
        public GXConfiguration? Filter { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// Get site information reply.
    /// </summary>
    [DataContract]
    public class ListConfigurationResponse
    {
        /// <summary>
        /// List of configuration settings.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [ExcludeOpenApi(typeof(GXLocalizedResource),
            nameof(GXLocalizedResource.Language))]
        public IEnumerable<GXConfiguration>? Configurations { get; set; }

        /// <summary>
        /// Total number of matching configuration items before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Update configuration values.
    /// </summary>
    [DataContract]
    public class ConfigurationUpdate : IGXRequest<ConfigurationUpdateResponse>
    {
        /// <summary>
        /// Updated configurations.
        /// </summary>
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
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
    [DataContract]
    public class ConfigurationRunCron : IGXRequest<ConfigurationRunCronResponse>
    {
    }

    /// <summary>
    /// Run cron reply.
    /// </summary>
    [DataContract]
    public class ConfigurationRunCronResponse
    {
    }

    /// <summary>
    /// Run prune reply.
    /// </summary>
    [DataContract]
    public class ConfigurationRunPruneResponse
    {
    }

    /// <summary>
    /// Run prune.
    /// </summary>
    [DataContract]
    public class ConfigurationRunPrune : IGXRequest<ConfigurationRunPruneResponse>
    {
    }

    /// <summary>
    /// Requests information about assemblies loaded by the server.
    /// </summary>
    [DataContract]
    public class LoadedAssemblies : IGXRequest<LoadedAssembliesResponse>
    {
        /// <summary>
        /// Optional criteria used to filter assemblies.
        /// </summary>
        public GXAssembly? Filter { get; set; }
    }

    /// <summary>
    /// Assemblies loaded by the server.
    /// </summary>
    [DataContract]
    public class LoadedAssembliesResponse
    {
        /// <summary>
        /// Loaded assemblies.
        /// </summary>
        public IEnumerable<GXAssembly>? Assemblies { get; set; }
    }

    /// <summary>
    /// Stop application request.
    /// </summary>
    [DataContract]
    public class StopApplicationRequest
    {
    }

    /// <summary>
    /// Resets configuration settings to built-in defaults.
    /// </summary>
    [DataContract]
    public class RegenerateConfiguration : IGXRequest<RegenerateConfigurationResponse>
    {
        /// <summary>
        /// Configuration names or ID strings to reset. Null or empty recreates all configurations.
        /// </summary>
        [DataMember]
        public IEnumerable<string>? Ids { get; set; }
    }

    /// <summary>
    /// Response to resetting configuration settings to their built-in defaults.
    /// </summary>
    [DataContract]
    public class RegenerateConfigurationResponse
    {
    }
}
