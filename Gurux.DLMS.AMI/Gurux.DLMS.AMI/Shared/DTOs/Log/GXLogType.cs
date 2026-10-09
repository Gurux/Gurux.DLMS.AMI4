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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Log
{
    /// <summary>
    /// Log type table.
    /// </summary>
    [DataContract(Name = "GXLogType"), Serializable]
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class GXLogType : IUnique<long>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXLogType()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new log group is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Script group name.</param>
        public GXLogType(string? name)
        {
            Active = true;
            Name = name;
            Logs = new List<GXLog>();
        }

        /// <summary>
        /// Log group ID.
        /// </summary>
        [DataMember(IsRequired = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        [Index(true)]
        [IsRequired]
        public Guid PublicId { get; set; }

        /// <summary>Database-generated log type storage key.</summary>
        [DataMember, PrimaryKey, AutoIncrement]
        [Filter(FilterType.Exact)]
        public long Id { get; set; }
        /// <summary>
        /// Is log type active.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Minimum log severity level that is saved.
        /// </summary>
        [DataMember]
        [DefaultValue(1)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public int? Level { get; set; }

        /// <summary>
        /// The creator of the log type.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Name of the log group.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Normalized name.
        /// </summary>
        /// <remarks>
        /// Normalized name is unique and it's used for case-insensitive comparisons and searches.
        /// </remarks>
        [DataMember]
        [Index]
        [StringLength(64)]
        [IsRequired]
        public string? NormalizedName { get; set; }

        /// <summary>
        /// Log group description.
        /// </summary>
        [DataMember]
        public string? Description { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when log group was removed.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// When was the log group last updated.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>
        /// User has modified the item.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        [JsonIgnore]
        public bool Modified { get; set; }

        /// <summary>
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }


        /// <summary>
        /// Authorization policies that allow viewing this resource (alternative named policies).
        /// </summary>
        [DataMember]
        [Ignore(IgnoreType.Db)]
        public List<string>? Policies { get; set; }

        /// <summary>
        /// List of logs that belong to this log group.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXLog))]
        public List<GXLog>? Logs { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXLogType);
        }
    }
}



