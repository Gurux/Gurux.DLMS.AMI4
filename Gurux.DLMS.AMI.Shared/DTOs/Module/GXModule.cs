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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Gurux.DLMS.AMI.Module.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Module
{
    /// <summary>A module's catalog identity, installation state, settings, and associated resources.</summary>
    public class GXModule : GXTableBase, IUnique<string>
    {
        /// <summary>
        /// Module identifier.
        /// </summary>
        [DataMember(Name = "ID")]
        [Description("Module identifier.")]
        [StringLength(64)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string Id { get; set; } = "";

        /// <summary>Alternative visibility policies; empty inherits the surrounding permissions.</summary>
        [DataMember, Ignore(IgnoreType.Db)]
        public List<string>? Policies { get; set; }

        /// <summary>
        /// Module name.
        /// </summary>
        [DataMember(Name = "Name")]
        [Description("Module Name.")]
        [StringLength(128)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Icon supplied by the module.
        /// </summary>
        [DataMember, DefaultValue(null)]
        public string? Icon { get; set; }

        /// <summary>
        /// Protocols that can use this module.
        /// </summary>
        [DataMember(Name = "Protocols")]
        [Description("Protocols that can use this module.")]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Protocols { get; set; }

        /// <summary>
        /// The creator of the module.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Module Description.
        /// </summary>
        [StringLength(256)]
        [Description("Module Description.")]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Description { get; set; }

        /// <summary>Persisted deployment choice, independent of runtime success or failure.</summary>
        /// <remarks>Null leaves the choice unspecified in filters and partial updates.</remarks>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>Installation state, distinguishing available, installed, and custom-built modules.</summary>
        /// <remarks>
        /// Custom builds are represented by <see cref="ModuleStatus.CustomBuild"/>.
        /// </remarks>
        [DefaultValue(ModuleStatus.Installable)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public ModuleStatus? Status { get; set; }

        /// <summary>Server runtime snapshot; null when this host has no runtime record for the module.</summary>
        /// <remarks>Transient and server-owned. Never persisted or accepted as a configuration setting.</remarks>
        [DataMember, Ignore(IgnoreType.Db)]
        public AmiModuleState? RuntimeState { get; set; }

        /// <summary>Capabilities describing where and how the module can be used.</summary>
        [DefaultValue(ModuleType.None)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public ModuleType? Type { get; set; }

        /// <summary>
        /// Module file name.
        /// </summary>
        /// <remarks>
        /// This is server file name. Ex. Gurux.LoginNotifier.Server.
        /// </remarks>
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? FileName { get; set; }

        /// <summary>
        /// Module development path.
        /// </summary>
        [Description("Module development path.")]
        [DefaultValue(null)]
        public string? Path { get; set; }

        /// <summary>Serialized configuration supplied to the module.</summary>
        public string? Settings { get; set; }

        /// <summary>External assemblies associated with the module.</summary>
        [DataMember]
        [ForeignKey(typeof(GXModuleAssembly))]
        public List<GXModuleAssembly>? Assemblies { get; set; }

        /// <summary>
        /// List of scripts that this module has created.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXScript))]
        public List<GXScript>? Scripts { get; set; }

        /// <summary>Schedules that invoke this module.</summary>
        [DataMember]
        [ForeignKey(typeof(GXSchedule))]
        public List<GXSchedule>? Schedules { get; set; }

        /// <summary>Workflows that invoke this module.</summary>
        [DataMember]
        [ForeignKey(typeof(GXWorkflow))]
        public List<GXWorkflow>? Workflows { get; set; }

        /// <summary>
        /// List of user groups that this module belongs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXUserGroup), typeof(GXUserGroupDeviceGroup))]
        public List<GXUserGroup>? UserGroups { get; set; }

        /// <summary>Module groups containing this module.</summary>
        [DataMember]
        [ForeignKey(typeof(GXModuleGroup), typeof(GXModuleGroupModule))]
        public List<GXModuleGroup>? ModuleGroups { get; set; }

        /// <summary>
        /// Module logs.
        /// </summary>
        [DataMember, Ignore(IgnoreType.Db)]
        public List<GXLog>? Logs { get; set; }

        /// <summary>
        /// Module versions.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXModuleVersion))]
        public List<GXModuleVersion>? Versions { get; set; }

        /// <summary>
        /// List of device parameters that are using this module.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceProperty))]
        public List<GXDeviceProperty>? DeviceParameters { get; set; }

        /// <summary>
        /// List of object parameters that are using this module.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXObjectParameter))]
        public List<GXObjectParameter>? ObjectParameters { get; set; }

        /// <summary>
        /// List of attribute template parameters that are using this module.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXAttributeProperty))]
        public List<GXAttributeProperty>? AttributeParameters { get; set; }

        /// <summary>
        /// Module active version number.
        /// </summary>
        [DefaultValue(null)]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? Version { get; set; }

        /// <summary>
        /// Latest available module version.
        /// </summary>
        [DefaultValue(null)]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? AvailableVersion { get; set; }


        /// <summary>Whether a newer module version is available.</summary>
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? NewVersion { get; set; }

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
        /// When the module is updated for the last time.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


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
        /// Constructor.
        /// </summary>
        public GXModule()
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Module name</param>
        public GXModule(string name)
        {
            Id = name;
            UserGroups = new List<GXUserGroup>();
            ModuleGroups = new List<GXModuleGroup>();
            Logs = new List<GXLog>();
            Versions = new List<GXModuleVersion>();
            Scripts = new List<GXScript>();
            Assemblies = new List<GXModuleAssembly>();
            DeviceParameters = new List<GXDeviceProperty>();
            ObjectParameters = new List<GXObjectParameter>();
            AttributeParameters = new List<GXAttributeProperty>();
            Schedules = new List<GXSchedule>();
            Workflows = new List<GXWorkflow>();
            TraceLevel = System.Diagnostics.TraceLevel.Error;
        }

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

        ///<inheritdoc />
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Id))
            {
                return Id + " " + Description;
            }
            return nameof(GXModule);
        }
        /// <summary>Installed catalog release label, independent of the assembly/package directory version.</summary>
        [DataMember, StringLength(128)]
        public string? ReleaseVersion { get; set; }
    }
}
