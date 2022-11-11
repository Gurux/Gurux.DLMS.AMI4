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
using Gurux.Common;
using Gurux.DLMS.AMI.Shared.DTOs;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{   
    /// <summary>
    /// Get site information.
    /// </summary>
    [DataContract]
    public class ListConfiquration : IGXRequest<ListConfiqurationResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the configuration items to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter system info.
        /// </summary>
        public GXConfiguration? Filter
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get site information reply.
    /// </summary>
    [DataContract]
    public class ListConfiqurationResponse
    {
        /// <summary>
        /// List of configuration settings.
        /// </summary>
        [DataMember]
        public GXConfiguration[]? Configurations
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the configuration items.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
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
        public GXConfiguration[]? Configurations
        {
            get;
            set;
        }
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
    /// Run cron.
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
    /// Run cron.
    /// </summary>
    [DataContract]
    public class LoadedAssemblies : IGXRequest<LoadedAssembliesResponse>
    {
        /// <summary>
        /// Filter can be used to filter assemblies.
        /// </summary>
        public GXAssembly? Filter
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Run cron reply.
    /// </summary>
    [DataContract]
    public class LoadedAssembliesResponse
    {
        /// <summary>
        /// Loaded assemblies.
        /// </summary>
        public GXAssembly[]? Assemblies
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Stop application request.
    /// </summary>
    [DataContract]
    public class StopApplicationRequest
    {
    }
}
