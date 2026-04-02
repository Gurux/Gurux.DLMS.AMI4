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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// List IP addresses where user has establish the connection.
    /// </summary>
    public class GXIpAddress : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Identifier.
        /// </summary>
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// IP address.
        /// </summary>
        [DataMember]
        [Description("IP address.")]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        [StringLength(39)]
        public string? IPAddress
        {
            get;
            set;
        }

        /// <summary>
        /// User.
        /// </summary>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public GXUser? User
        {
            get;
            set;
        }

        /// <summary>
        /// Is IP address in blocked or allowed lists.
        /// </summary>
        [Index(false)]
        [DefaultValue(false)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public IpAddressListStatus? Status
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Last detected.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Detected
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
    }
}
