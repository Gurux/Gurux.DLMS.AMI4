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
using Gurux.Common.Db;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Installed modules
    /// </summary>
    public class GXModule : GXTableBase, IUnique<string>
    {
        [DataMember(Name = "ID")]
        [Description("Module identifier.")]
        [StringLength(64)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string Id
        {
            get;
            set;
        } = "";

        /// <summary>
        /// The module creator.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator
        {
            get;
            set;
        }

        /// <summary>
        /// Module Description.
        /// </summary>
        [StringLength(256)]
        [Description("Module Description.")]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Is module active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Is module installed.
        /// </summary>
        /// <remarks>
        /// False, if module is a custom build.
        /// </remarks>
        [DefaultValue(ModuleStatus.Installable)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public ModuleStatus? Status { get; set; }

        /// <summary>
        /// Installation Url.
        /// </summary>
        /// <remarks>
        /// If module is added manually this is null.
        /// </remarks>
        [StringLength(256)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Url
        {
            get;
            set;
        }

        /// <summary>
        /// Module file name.
        /// </summary>
        /// <remarks>
        /// This is server file name. Ex. Gurux.LoginNotifier.Server.
        /// </remarks>
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? FileName
        {
            get;
            set;
        }

        /// <summary>
        /// Full Name of the module class.
        /// </summary>
        /// <remarks>
        /// This is server main class name. Ex. Gurux.LoginNotifier.Server.Smtp
        /// </remarks>
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? Class
        {
            get;
            set;
        }

        /// <summary>
        /// Configuration UI.
        /// </summary>
        /// <remarks>
        /// Optional configuration UI that server offers.
        /// </remarks>
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? ConfigurationUI
        {
            get;
            set;
        }

        /// <summary>
        /// Module settings.
        /// </summary>
        [DataMember]
        public string? Settings
        {
            get;
            set;
        }

        /// <summary>
        /// List of external assemblies.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXModuleAssembly))]
        public List<GXModuleAssembly> Assemblies
        {
            get;
            set;
        }

        /// <summary>
        /// List of scripts that this module has created.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScript))]
        public List<GXScript> Scripts
        {
            get;
            set;
        }

        /// <summary>
        /// List of schedule that are calling this module.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSchedule))]
        public List<GXSchedule> Schedules
        {
            get;
            set;
        }

        /// <summary>
        /// List of workflows that are calling this module.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXWorkflow))]
        public List<GXWorkflow> Workflows
        {
            get;
            set;
        }

        /// <summary>
        /// List of user groups that this module belongs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXUserGroup), typeof(GXUserGroupDeviceGroup))]
        public List<GXUserGroup> UserGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of module groups where this module belongs.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXModuleGroup), typeof(GXModuleGroupModule))]
        public List<GXModuleGroup> ModuleGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Module logs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXModuleLog))]
        public List<GXModuleLog> Logs
        {
            get;
            set;
        }

        /// <summary>
        /// Module versions.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXModuleVersion))]
        public List<GXModuleVersion> Versions
        {
            get;
            set;
        }

        /// <summary>
        /// List of device parameters that are using this module.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceParameter))]
        public List<GXDeviceParameter> DeviceParameters
        {
            get;
            set;
        }

        /// <summary>
        /// List of object parameters that are using this module.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXObjectParameter))]
        public List<GXObjectParameter> ObjectParameters
        {
            get;
            set;
        }

        /// <summary>
        /// List of attribute template parameters that are using this module.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXAttributeParameter))]
        public List<GXAttributeParameter> AttributeParameters
        {
            get;
            set;
        }

        /// <summary>
        /// Module active version number.
        /// </summary>
        [DefaultValue(null)]
        [StringLength(20)]
        [Filter(FilterType.Contains)]
        public string? Version
        {
            get;
            set;
        }

        /// <summary>
        /// Latest available module version.
        /// </summary>
        [DefaultValue(null)]
        [StringLength(20)]
        [Filter(FilterType.Contains)]
        public string? AvailableVersion
        {
            get;
            set;
        }


        /// <summary>
        /// The new version is available.
        /// </summary>
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? NewVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When the module is updated for the last time.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the item.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        public bool Modified
        {
            get;
            set;
        }


        /// <summary>
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        public string? ConcurrencyStamp
        {
            get;
            set;
        }

        /// <summary>
        /// Icon name.
        /// </summary>
        public string? Icon
        {
            get;
            set;
        }

        /// <summary>
        /// Localized strings for this module.
        /// </summary>
        /// <remarks>
        /// This is used only for database and it's not send for the user.
        /// </remarks>
        [DataMember]
        [JsonIgnore]
        public GXLocalizedResource[]? Resources
        {
            get;
            set;
        }

        /// <summary>
        /// Localized resources for this module.
        /// </summary>
        /// <remarks>
        /// Localized resources are return with this.
        /// </remarks>
        [DataMember]
        [Ignore(IgnoreType.Db)]
        public GXLanguage[]? Languages
        {
            get;
            set;
        }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Update concurrency stamp.
        /// </summary>
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXModule()
        {
            UserGroups = new List<GXUserGroup>();
            ModuleGroups = new List<GXModuleGroup>();
            Logs = new List<GXModuleLog>();
            Versions = new List<GXModuleVersion>();
            Scripts = new List<GXScript>();
            Assemblies = new List<GXModuleAssembly>();
            DeviceParameters = new List<GXDeviceParameter>();
            ObjectParameters = new List<GXObjectParameter>();
            AttributeParameters = new List<GXAttributeParameter>();
            Schedules = new List<GXSchedule>();
            Workflows = new List<GXWorkflow>();
        }

        ///<inheritdoc cref="string.ToString()"/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Id))
            {
                return Id + " " + Description;
            }
            return nameof(GXModule);
        }
    }
}
