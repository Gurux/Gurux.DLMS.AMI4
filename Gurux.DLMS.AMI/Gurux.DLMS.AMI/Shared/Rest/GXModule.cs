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
using System.Runtime.Serialization;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Add module response.
    /// </summary>
    [DataContract]
    public class AddModuleResponse
    {
        /// <summary>
        /// Modules to update.
        /// </summary>
        [DataMember]
        public GXModule[] Modules
        {
            get;
            set;
        }

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
        /// Module to update.
        /// </summary>
        [DataMember]
        public GXModule? Module
        {
            get;
            set;
        }
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
        public GXModule? Module
        {
            get;
            set;
        }
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
        /// Amount of the modules to retreave.
        /// </summary>
        public int Count
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
        public GXModule[] Modules
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
    public class DeleteModule 
    {
        /// <summary>
        /// Removed modules.
        /// </summary>
        [DataMember]
        public string[] Modules
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete module response.
    /// </summary>
    [DataContract]
    public class DeleteModuleResponse
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
    /// Get module configuration UI.
    /// </summary>
    [DataContract]
    public class ModuleConfigurationUI : IGXRequest<ModuleConfigurationUIResponse>
    {
        /// <summary>
        /// Module name.
        /// </summary>
        public string Name
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get module configuration UI response.
    /// </summary>
    [DataContract]
    public class ModuleConfigurationUIResponse
    {
        /// <summary>
        /// UI module and dependencies.
        /// </summary>
        public string[] Modules
        {
            get;
            set;
        }
    }

}
