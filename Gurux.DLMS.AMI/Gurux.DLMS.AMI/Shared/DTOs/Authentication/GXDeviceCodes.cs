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
using System.ComponentModel;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// Device codes table.
    /// </summary>
    [DataContract(Name = "GXDeviceCodes"), Serializable]
    public class GXDeviceCodes : IUnique<string>
    {
        //Identifier.
        [StringLength(200)]
        [DataMember(Name = "UserCode")]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public string Id
        {
            get
            {
                return DeviceCode;
            }
            set
            {
                DeviceCode = value;
            }
        }

        /// <summary>
        /// Device code.
        /// </summary>
        [DataMember]
        [Index]
        [StringLength(200)]
        [IsRequired]
        public string DeviceCode
        {
            get;
            set;
        }
        [DataMember]
        [StringLength(200)]
        public string? SubjectId
        {
            get;
            set;
        }
        /// <summary>
        /// Session Id.
        /// </summary>
        [DataMember]
        [StringLength(100)]
        public string? SessionId
        {
            get;
            set;
        }

        /// <summary>
        /// Client Id.
        /// </summary>
        [DataMember]
        [StringLength(200)]
        [IsRequired]
        public string ClientId
        {
            get;
            set;
        }

        /// <summary>
        /// Description.
        /// </summary>
        [DataMember]
        [StringLength(200)]
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Expiration time.
        /// </summary>
        [DataMember]
        [IsRequired]
        [Index(false)]
        public DateTime Expiration
        {
            get;
            set;
        }

        /// <summary>
        /// Data.
        /// </summary>
        [DataMember]
        [IsRequired]
        public string Data
        {
            get;
            set;
        }
    }
}
