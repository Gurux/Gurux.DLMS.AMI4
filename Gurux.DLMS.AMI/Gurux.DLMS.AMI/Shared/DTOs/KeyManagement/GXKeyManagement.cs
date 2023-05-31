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
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Shared.DTOs.KeyManagement
{
    /// <summary>
    /// Keys table.
    /// </summary>
    [DataContract(Name = "GXKeyManagement"), Serializable]
    public partial class GXKeyManagement : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXKeyManagement()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new key management is created. It will create all needed lists.
        /// </remarks>
        /// <param name="systemTitle">System title.</param>
        public GXKeyManagement(string? systemTitle)
        {
            SystemTitle = systemTitle;
            KeyManagementGroups = new List<GXKeyManagementGroup>();
            Logs = new List<GXKeyManagementLog>();
            Keys = new List<GXKeyManagementKey>();
        }

        /// <summary>
        /// Identifier.
        /// </summary>
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Key management name.
        /// </summary>
        [DataMember]
        [Description("Key management name")]
        [StringLength(64, ErrorMessage = "Name is too long.")]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Target device.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXDevice? Device
        {
            get;
            set;
        }

        /// <summary>
        /// Target device group.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXDeviceGroup? DeviceGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Target device template.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXDeviceTemplate? Template
        {
            get;
            set;
        }

        /// <summary>
        /// The creator of the key management.
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
        /// System title.
        /// </summary>
        [Index(Unique = true)]
        [DataMember]
        [IsRequired]
        [Filter(FilterType.Contains)]
        [StringLength(16)]
        public string? SystemTitle
        {
            get;
            set;
        }

        /// <summary>
        /// Key management keys.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXKeyManagementKey))]
        [Filter(FilterType.Contains)]
        public List<GXKeyManagementKey>? Keys
        {
            get;
            set;
        }

        /// <summary>
        /// List of key management groups where this key management belongs.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXKeyManagementGroup), typeof(GXKeyManagementGroupKeyManagement))]
        [Filter(FilterType.Contains)]
        public List<GXKeyManagementGroup>? KeyManagementGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Key management logs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXKeyManagementLog))]
        [Filter(FilterType.Contains)]
        public List<GXKeyManagementLog>? Logs
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [Index(false, Descend = true)]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When was the key management last updated.
        /// </summary>
        [Description("When was the key management last updated.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the key management.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        [JsonIgnore]
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
        /// Remove time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed
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

        /// <inheritdoc/>
        public override string ToString()
        {
            string str = string.Empty;
            if (!string.IsNullOrEmpty(Name))
            {
                str = Name;
            }
            if (!string.IsNullOrEmpty(SystemTitle))
            {
                str += " " + SystemTitle;
            }
            if (str == string.Empty)
            {
                str = nameof(GXKeyManagement);
            }
            return str;
        }
    }
}
