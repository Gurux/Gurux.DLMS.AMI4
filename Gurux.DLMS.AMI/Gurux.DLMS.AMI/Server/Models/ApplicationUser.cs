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

using Gurux.DLMS.AMI.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gurux.DLMS.AMI.Server.Models
{
    /// <summary>
    /// Application user.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// When user is created.
        /// </summary>
        [PersonalData]
        [DataType(DataType.DateTime)]
        public DateTime? CreationTime
        {
            get;
            set;
        }
        /// <summary>
        /// When was the last time a user logged in.
        /// </summary>
        [PersonalData]
        [DataType(DataType.DateTime)]
        public virtual DateTimeOffset? LastLogin
        {
            get;
            set;
        }
        /// <summary>
        /// IP address where last connection is established.
        /// </summary>
        [PersonalData]
        [Column("IPAddress")]
        public UInt64? LastIPAddress
        {
            get;
            set;
        }
        /// <summary>
        /// When user is updated last time.
        /// </summary>
        [PersonalData]
        [Column("Updated")]
        [DataType(DataType.DateTime)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        } 
        /// <summary>
        /// Given name.
        /// </summary>
        [PersonalData]
        [Column("GivenName")]
        public string? GivenName { get; set; }
        /// <summary>
        /// Surname
        /// </summary>
        [PersonalData]
        [Column("Surname")]
        public string? Surname { get; set; }

        /// <summary>
        /// Default language.
        /// </summary>
        [PersonalData]
        [Column("Language")]
        public string? Language { get; set; }

        /// <summary>
        /// Date of birth.
        /// </summary>
        [PersonalData]
        [Column("DateOfBirth")]
        [DataType(DataType.Date)]
        public DateTimeOffset? DateOfBirth { get; set; }

        /// <summary>
        /// Profile picture.
        /// </summary>
        [PersonalData]
        [Column("ProfilePicture")]
        public string? ProfilePicture { get; set; }      
    }
}
