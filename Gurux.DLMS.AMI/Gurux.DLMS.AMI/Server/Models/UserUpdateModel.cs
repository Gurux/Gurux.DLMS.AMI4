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

using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Server.Models
{
    public class UserUpdateModel
    {
        /// <summary>
        /// Given name.
        /// </summary>
        public string? GivenName { get; set; }
        /// <summary>
        /// Surname.
        /// </summary>
        public string? Surname { get; set; }

        /// <summary>
        /// Phone number.
        /// </summary>
        [Phone]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Language.
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Date of birth.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTimeOffset? DateOfBirth { get; set; }

        /// <summary>
        /// Profile picture.
        /// </summary>
        public string ProfilePicture
        {
            get;
            set;
        }
    }

}
