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
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// User logins table.
    /// </summary>
    [DataContract(Name = "GXUserLogin"), Serializable]
    public class GXUserLogin : IUnique<string>
    {
        //Identifier.
        [StringLength(128)]
        [DataMember(Name = "LoginProvider")]
        [Filter(FilterType.Exact)]
        public string Id
        {
            get;
            set;
        }

        [DataMember]
        [StringLength(128)]
        public string ProviderKey
        {
            get;
            set;
        }

        [DataMember]
        [StringLength(128)]
        public string ProviderDisplayName
        {
            get;
            set;
        }

        [DataMember]
        [Index]
        public Guid UserId
        {
            get;
            set;
        }
    }
}
