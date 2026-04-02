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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Cluster table.
    /// </summary>
    public class GXRoute : GXTableBase, IUnique<string>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXRoute()
        {
        }

        /// <summary>
        ///Cluster Id.
        /// </summary>
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Description("Cluster Id.")]
        [Filter(FilterType.Exact)]
        [StringLength(62)]
        [DefaultValue(null)]
        public string Id
        {
            get;
            set;
        } = default!;

        /// <summary>
        /// Parent cluster.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public GXCluster? Cluster
        {
            get;
            set;
        }

        /// <summary>
        /// Used Load balancing policy.
        /// </summary>
        [DataMember]
        public LoadBalancingPolicy? LoadBalancingPolicy
        {
            get;
            set;
        }

        /// <summary>
        /// Match path.
        /// </summary>
        [DataMember]
        public string MatchPath { get; set; }

        /// <summary>
        /// Transform path pattern.
        /// </summary>
        [DataMember]
        public TransformPathPattern TransformPathPattern { get; set; }

        /// <summary>
        /// Custom path pattern.
        /// </summary>
        [DataMember]
        public string? CustomPathPattern { get; set; }
        

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When value is last updated.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the attributes.
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
            if (CreationTime == null)
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
            StringBuilder sb = new StringBuilder();
            if (sb.Length == 0)
            {
                return nameof(GXCluster);
            }
            return sb.ToString();
        }
    }
}
