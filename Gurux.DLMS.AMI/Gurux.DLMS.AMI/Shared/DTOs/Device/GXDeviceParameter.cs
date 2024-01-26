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
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Device
{
    /// <summary>
    /// Generic interface for all the parameters.
    /// </summary>
    public interface IGXParameter
    {
        /// <summary>
        /// Module settings unique name.
        /// </summary>
        public string? Name
        {
            get;
            set;
        }


        /// <summary>
        /// The module whose settings these are.
        /// </summary>
        public GXModule? Module
        {
            get;
            set;
        }

        /// <summary>
        /// Module settings.
        /// </summary>
        public string? Settings
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Device parameter.
    /// </summary>
    public class GXDeviceParameter : IUnique<Guid>, IGXParameter
    {
        /// <summary>
        /// Parameter identifier.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Parent device.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [JsonIgnore]
        public GXDevice? Device
        {
            get;
            set;
        }

        /// <summary>
        /// The module whose settings these are.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        public GXModule? Module
        {
            get;
            set;
        }

        /// <summary>
        /// Module settings unique name.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        public string? Name
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
        /// Creation time.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset CreationTime
        {
            get;
            set;
        }
        /// <summary>
        /// When parameter is updated for last time.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the parameter.
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
    }
}
