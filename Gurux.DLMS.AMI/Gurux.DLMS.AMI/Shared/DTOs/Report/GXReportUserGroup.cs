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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Report
{
    /// <summary>
    /// A data contract class representing report to user group binding object.
    /// </summary>
    [DataContract(Name = "GXReportUserGroup"), Serializable]
    [IndexCollection(true, nameof(ReportId), nameof(UserGroupId), Clustered = true)]
    public class GXReportUserGroup
    {
        /// <summary>
        /// The database ID of the report.
        /// </summary>
        [DataMember(Name = "ReportID")]
        [ForeignKey(typeof(GXReport), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        public Guid ReportId
        {
            //ForeignKeyDelete is None because user group is causing multiple cascade paths error in MSSQL.
            get;
            set;
        }

        /// <summary>
        /// The database ID of the user group.
        /// </summary>
        [DataMember(Name = "UserGroupId")]
        [ForeignKey(typeof(GXUserGroup), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid UserGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// The time when the report was joined to the device.
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
        /// Time when device was removed from report.
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
    }
}
