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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Manufacturer
{
    /// <summary>Associates a manufacturer with a manufacturer group.</summary>
    [Description("Associates a manufacturer with a manufacturer group.")]
    [DataContract(Name = "GXManufacturerGroupManufacturer"), Serializable]
    [IndexCollection(true, nameof(ManufacturerGroupId), nameof(ManufacturerId), Clustered = true)]
    public class GXManufacturerGroupManufacturer
    {
        /// <summary>The database identifier of the Schedule group.</summary>
        [Description("The database identifier of the Schedule group.")]
        [DataMember(Name = "ManufacturerGroupId")]
        [ForeignKey(typeof(GXManufacturerGroup), OnDelete = ForeignKeyDelete.None)]
        //ForeignKeyDelete is None because creator of the manufacturer is causing multiple cascade paths error in MSSQL.
        public Guid ManufacturerGroupId { get; set; }

        /// <summary>The database identifier of the manufacturer.</summary>
        [Description("The database identifier of the manufacturer.")]
        [DataMember(Name = "ManufacturerID")]
        [ForeignKey(typeof(GXManufacturer), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid ManufacturerId { get; set; }

        /// <summary>Time when the record was created.</summary>
        [DataMember]
        [Description("Time when the record was created.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when agent group was removed from user group.
        /// </summary>
        [Description("Time when agent group was removed from user group.")]
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
