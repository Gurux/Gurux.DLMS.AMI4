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
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get module.
    /// </summary>
    public class GetModuleResponse
    {
        /// <summary>
        /// Module information.
        /// </summary>        
        [ExcludeOpenApi(typeof(GXModule), nameof(GXModule.UserGroups),
            nameof(GXModule.ModuleGroups), nameof(GXModule.Logs),
            nameof(GXModule.Versions), nameof(GXModule.Scripts),
            nameof(GXModule.Assemblies), nameof(GXModule.DeviceParameters),
            nameof(GXModule.ObjectParameters), nameof(GXModule.AttributeParameters),
            nameof(GXModule.Schedules), nameof(GXModule.Workflows),
            nameof(GXModule.Creator))]
        public GXModule? Item
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Add module response.
    /// </summary>
    [DataContract]
    public class AddModuleResponse
    {
        /// <summary>
        /// Added modules.
        /// </summary>
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
        [ExcludeOpenApi(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Module))]
        [ExcludeOpenApi(typeof(GXObjectParameter), nameof(GXObjectParameter.Module))]
        [ExcludeOpenApi(typeof(GXAttributeParameter), nameof(GXAttributeParameter.Module))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]

        public GXModule[] Modules
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// Is restart required.
        /// </summary>
        [DataMember]
        public bool Restart
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Install new module.
    /// </summary>
    [DataContract]
    public class InstallModule
    {
        /// <summary>
        /// Installed module.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        public GXModule Module
        {
            get;
            set;
        } = default!;
    }

    /// <summary>
    /// Install module response.
    /// </summary>
    [DataContract]
    public class InstallModuleResponse
    {
        /// <summary>
        /// Is restart required.
        /// </summary>
        [DataMember]
        public bool Restart
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Update module.
    /// </summary>
    [DataContract]
    public class UpdateModule
    {
        /// <summary>
        /// Module to update.
        /// </summary>
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
        [ExcludeOpenApi(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Module))]
        [ExcludeOpenApi(typeof(GXObjectParameter), nameof(GXObjectParameter.Module))]
        [ExcludeOpenApi(typeof(GXAttributeParameter), nameof(GXAttributeParameter.Module))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        public GXModule Module
        {
            get;
            set;
        } = default!;
    }

    /// <summary>
    /// Update module response.
    /// </summary>
    [DataContract]
    public class UpdateModuleResponse
    {
        /// <summary>
        /// Is restart required.
        /// </summary>
        [DataMember]
        public bool Restart
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get installed modules.
    /// </summary>
    [DataContract]
    public class ListModules : IGXRequest<ListModulesResponse>
    {
        /// <summary>
        /// Filter can be used to filter modules.
        /// </summary>
        [ExcludeOpenApi(typeof(GXModule), nameof(GXModule.UserGroups),
            nameof(GXModule.ModuleGroups), nameof(GXModule.Logs),
            nameof(GXModule.Versions), nameof(GXModule.Scripts),
            nameof(GXModule.Assemblies), nameof(GXModule.DeviceParameters),
            nameof(GXModule.ObjectParameters), nameof(GXModule.AttributeParameters),
            nameof(GXModule.Schedules), nameof(GXModule.Workflows),
            nameof(GXModule.Creator))]
        public GXModule? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access modules from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all modules are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }

        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of the modules to retrieve.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public string[]? Select
        {
            get;
            set;
        }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending
        {
            get;
            set;
        }

        /// <summary>
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public string[]? Included
        {
            get;
            set;
        }
        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
        /// </remarks>
        public string[]? Exclude
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get system errors response.
    /// </summary>
    [Description("Get system errors response.")]
    [DataContract]
    public class ListModulesResponse
    {
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
        public GXModule[]? Modules
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of the modules.
        /// </summary>
        [DataMember]
        [Description("Amount of the modules.")]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete module.
    /// </summary>
    [DataContract]
    [Description("Delete module.")]
    public class RemoveModule
    {
        /// <summary>
        /// Removed modules.
        /// </summary>
        [DataMember]
        public string[] Modules
        {
            get;
            set;
        } = default!;
    }

    /// <summary>
    /// Delete module response.
    /// </summary>
    [DataContract]
    public class RemoveModuleResponse
    {
        /// <summary>
        /// Is restart required to remove the module.
        /// </summary>
        [DataMember]
        public bool Restart
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Check if there are new module versions available.
    /// </summary>
    [DataContract]
    public class CheckModule : IGXRequest<CheckModuleResponse>
    {
    }

    /// <summary>
    /// Check module versions reply.
    /// </summary>
    [DataContract]
    public class CheckModuleResponse
    {
    }
}
