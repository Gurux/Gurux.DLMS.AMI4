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
    /// Persisted access tokes are saved for this table.
    /// </summary>
    [DataContract(Name = "GXPersistedGrants"), Serializable]
    public class GXPersistedGrants : IUnique<string>
    {
        //Identifier.
        [StringLength(200)]
        [DataMember(Name = "Key")]
        [Filter(FilterType.Exact)]
        public string Id
        {
            get;
            set;
        }

        [DataMember]
        [StringLength(50)]
        public string Type
        {
            get;
            set;
        }

        [StringLength(200)]
        [DataMember]
        public string SubjectId
        {
            get;
            set;
        }

        [DataMember]
        [StringLength(100)]
        public string SessionId
        {
            get;
            set;
        }

        [DataMember]
        [StringLength(200)]
        public string ClientId
        {
            get;
            set;
        }

        [DataMember]
        [StringLength(200)]
        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
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
        /// Expiration time.
        /// </summary>
        [DataMember]
        [Index(false)]
        public DateTime Expiration
        {
            get;
            set;
        }

        [DataMember]
        public DateTime ConsumedTime
        {
            get;
            set;
        }

        [DataMember]
        public string Data
        {
            get;
            set;
        }
    }
}
