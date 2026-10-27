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
// This file is a part of Gurux Object Framework.
//
// Gurux Object Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Object Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A module parameter associated with a COSEM object.</summary>
    [Description("A module parameter associated with a COSEM object.")]
    [DataContract(Name = nameof(GXObjectParameter)), Serializable]
    [IndexCollection(true, nameof(Object), nameof(Module), nameof(Name))]
    public class GXObjectParameter : IUnique<Guid>, IGXProperty
    {
        /// <summary>
        /// Parameter identifier.
        /// </summary>
        [Description("Parameter identifier.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>COSEM object that owns this parameter.</summary>
        [Description("COSEM object that owns this parameter.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [JsonIgnore]
        [IsRequired]
        public GXObject? Object { get; set; }

        /// <summary>
        /// The module whose settings these are.
        /// </summary>
        [Description("The module whose settings these are.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        //ForeignKeyDelete is None because creator of the device is causing multiple cascade paths error in MSSQL.
        public GXModule? Module { get; set; }

        /// <summary>
        /// Module settings unique name.
        /// </summary>
        [Description("Module settings unique name.")]
        [DataMember]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Module object patameter.
        /// </summary>
        [Description("Module object patameter.")]
        [DataMember]
        public string? Value { get; set; }

        /// <summary>Version token maintained by Gurux Service.</summary>
        [Description("Version token maintained by Gurux Service.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset CreationTime { get; set; }
        /// <summary>Time when the parameter was last updated.</summary>
        [Description("Time when the parameter was last updated.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
