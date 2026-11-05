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

using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.KeyManagement
{
    /// <summary>A key management configuration with its provider and associated keys.</summary>
    [Description("A key management configuration with its provider and associated keys.")]
    [DataContract(Name = "GXKeyManagement"), Serializable]
    public partial class GXKeyManagement : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXKeyManagement.</summary>
        [Description("Initializes a new instance of GXKeyManagement.")]
        public GXKeyManagement()
        {
        }

        /// <summary>Initializes a new instance of GXKeyManagement.</summary>
        /// <remarks>
        /// This constructor is called when a new key management is created. It will create all required collections.
        /// </remarks>
        /// <param name="systemTitle">System title.</param>
        [Description("Initializes a new instance of GXKeyManagement.")]
        public GXKeyManagement(byte[]? systemTitle)
        {
            SystemTitle = systemTitle;
            KeyManagementGroups = [];
            Logs = [];
            Keys = [];
            TraceLevel = System.Diagnostics.TraceLevel.Error;
            Properties = [];
        }

        /// <summary>Unique identifier of the key management.</summary>
        [Description("Unique identifier of the key management.")]
        [Key]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Key management name.
        /// </summary>
        [DataMember]
        [Description("Key management name.")]
        [StringLength(64, ErrorMessage = "Name is too long.")]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Target device.
        /// </summary>
        [Description("Target device.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXDevice? Device { get; set; }

        /// <summary>
        /// Target device group.
        /// </summary>
        [Description("Target device group.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        //ForeignKeyDelete is None because Device will handle the deletion.
        public GXDeviceGroup? DeviceGroup { get; set; }

        /// <summary>
        /// Target device template.
        /// </summary>
        [Description("Target device template.")]
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXDeviceTemplate? Template { get; set; }

        /// <summary>
        /// The creator of the key management.
        /// </summary>
        [Description("The creator of the key management.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        //ForeignKeyDelete is None because Device will handle the deletion.
        public GXUser? Creator { get; set; }

        /// <summary>
        /// System title.
        /// </summary>
        [Description("System title.")]
        [Index(Unique = false)]
        [DataMember]
        [IsRequired]
        [Filter(FilterType.Contains)]
        [MaxLength(16)]
        public byte[]? SystemTitle { get; set; }

        /// <summary>Default key provider; null means local storage.</summary>
        [Description("Default key provider; null means local storage.")]
        [DataMember, DefaultValue(null), StringLength(512)]
        public string? ProviderId { get; set; }

        /// <summary>Named provider connection, without credentials.</summary>
        [Description("Named provider connection, without credentials.")]
        [DataMember, DefaultValue(null), StringLength(512)]
        public string? ProviderConnectionId { get; set; }

        /// <summary>
        /// Key management keys.
        /// </summary>
        [Description("Key management keys.")]
        [DataMember, ForeignKey(typeof(GXKeyManagementKey))]
        [Filter(FilterType.Contains)]
        public List<GXKeyManagementKey>? Keys { get; set; }

        /// <summary>Key management groups containing this configuration.</summary>
        [Description("Key management groups containing this configuration.")]
        [DataMember]
        [ForeignKey(typeof(GXKeyManagementGroup), typeof(GXKeyManagementGroupKeyManagement))]
        [Filter(FilterType.Contains)]
        public List<GXKeyManagementGroup>? KeyManagementGroups { get; set; }

        /// <summary>
        /// Key management logs.
        /// </summary>
        [Description("Key management logs.")]
        [DataMember, Ignore(IgnoreType.Db)]
        [Filter(FilterType.Contains)]
        public List<GXLog>? Logs { get; set; }


        /// <summary>
        /// Saved properties belonging to this key management.
        /// </summary>
        [Description("Saved properties belonging to this key management.")]
        [DataMember]
        [ForeignKey(typeof(GXKeyManagementProperty))]
        [Filter(FilterType.Contains)]
        public List<GXKeyManagementProperty>? Properties { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [Index(false, Descend = true)]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the key management was last updated.</summary>
        [Description("Time when the key management was last updated.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


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

        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

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
            string str = string.Empty;
            if (!string.IsNullOrEmpty(Name))
            {
                str = Name;
            }
            if (SystemTitle is { Length: > 0 })
            {
                str += " " + Convert.ToHexString(SystemTitle);
            }
            if (str == string.Empty)
            {
                str = nameof(GXKeyManagement);
            }
            return str;
        }
    }
}
