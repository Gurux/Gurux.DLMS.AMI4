﻿//
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
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Report
{
    /// <summary>
    /// A data contract class representing report to gateway binding object.
    /// </summary>
    [DataContract(Name = "GXReportGateway"), Serializable]
    [IndexCollection(true, nameof(ReportId), nameof(GatewayId), Clustered = true)]
    public class GXReportGateway : GXTableBase
    {
        /// <summary>
        /// The database ID of the report.
        /// </summary>
        [DataMember(Name = "ReportID")]
        [ForeignKey(typeof(GXReport), OnDelete = ForeignKeyDelete.None)]
        [StringLength(36)]
        [IsRequired]
        public Guid ReportId
        {
            get;
            set;
        }

        /// <summary>
        /// The database ID of the gateway.
        /// </summary>
        [DataMember(Name = "GatewayId")]
        [ForeignKey(typeof(GXGateway), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid GatewayId
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// The time when the report was joined to the gateway.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when gateway was removed from report.
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
    }
}
