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

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    [DataContract(Name = nameof(GXUserGroupGatewayGroup)), Serializable]
    [IndexCollection(true, nameof(UserGroupId), nameof(GatewayGroupId), Clustered = true)]
    public class GXUserGroupGatewayGroup
    {
        [DataMember(Name = "UserGroupID"), ForeignKey(typeof(GXUserGroup), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid UserGroupId
        {
            get;
            set;
        }

        [DataMember(Name = "GatewayGroupID"), ForeignKey(typeof(GXGatewayGroup), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid GatewayGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// The time when the gateway group was added to the user group.
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
        /// Time when gateway group was removed from user group.
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
