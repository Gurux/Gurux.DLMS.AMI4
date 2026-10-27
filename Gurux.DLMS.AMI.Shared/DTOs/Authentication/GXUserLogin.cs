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
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>An external authentication provider login associated with a user account.</summary>
    [System.ComponentModel.Description("An external authentication provider login associated with a user account.")]
    [DataContract(Name = "GXUserLogin"), Serializable]
    public class GXUserLogin : IUnique<string>
    {
        /// <summary>
        /// Gets or sets the login provider.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the login provider.")]
        [StringLength(128)]
        [DataMember(Name = "LoginProvider")]
        [Filter(FilterType.Exact)]
        public string Id { get; set; } = default!;

        /// <summary>
        /// Gets or sets the unique identifier for the authentication provider.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the unique identifier for the authentication provider.")]
        [DataMember]
        [StringLength(128)]
        public string? ProviderKey { get; set; } = default!;

        /// <summary>
        /// Gets or sets the display name of the authentication provider.
        /// </summary>
        [System.ComponentModel.Description("Gets or sets the display name of the authentication provider.")]
        [DataMember]
        [StringLength(128)]
        public string? ProviderDisplayName { get; set; } = default!;

        /// <summary>User identifier.</summary>
        [System.ComponentModel.Description("User identifier.")]
        [DataMember]
        [Index]
        public Guid UserId { get; set; }
    }
}
