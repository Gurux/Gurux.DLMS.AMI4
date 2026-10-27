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
using System.ComponentModel;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>Persisted device authorization codes used during device authentication.</summary>
    [Description("Persisted device authorization codes used during device authentication.")]
    [DataContract(Name = "GXDeviceCodes"), Serializable]
    public class GXDeviceCodes : IUnique<string>
    {
        /// <summary>Unique identifier of the device codes.</summary>
        [Description("Unique identifier of the device codes.")]
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
        [Description("Device code.")]
        [DataMember]
        [Index]
        [StringLength(200)]
        [IsRequired]
        public string DeviceCode { get; set; } = default!;

        /// <summary>
        /// Gets or sets the unique identifier for the subject.
        /// </summary>
        [Description("Gets or sets the unique identifier for the subject.")]
        [DataMember]
        [StringLength(200)]
        public string? SubjectId { get; set; }
        /// <summary>Session identifier.</summary>
        [Description("Session identifier.")]
        [DataMember]
        [StringLength(100)]
        public string? SessionId { get; set; }

        /// <summary>Client identifier.</summary>
        [Description("Client identifier.")]
        [DataMember]
        [StringLength(200)]
        [IsRequired]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string ClientId { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>Description of the device codes.</summary>
        [Description("Description of the device codes.")]
        [DataMember]
        [StringLength(200)]
        public string? Description { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Expiration time.
        /// </summary>
        [Description("Expiration time.")]
        [DataMember]
        [IsRequired]
        [Index(false)]
        public DateTime Expiration { get; set; }

        /// <summary>
        /// Data.
        /// </summary>
        [Description("Data.")]
        [DataMember]
        [IsRequired]
        public string Data { get; set; } = default!;
    }
}
