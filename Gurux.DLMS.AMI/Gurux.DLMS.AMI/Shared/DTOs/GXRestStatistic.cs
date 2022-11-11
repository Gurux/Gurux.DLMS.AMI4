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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Diagnostic is used to save Rest execution time to the database.
    /// </summary>
    public class GXRestStatistic : IUnique<Guid>
    {
        /// <summary>
        /// Rest timing identifier.
        /// </summary>
        //Filter uses default value.
        [DefaultValue(null)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Executing User.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXUser))]
        [Index(false)]
        public GXUser? User
        {
            get;
            set;
        }

        /// <summary>
        /// Rest path.
        /// </summary>
        [StringLength(64)]
        public string? Path
        {
            get;
            set;
        }

        /// <summary>
        /// The start time of rest execution.
        /// </summary>
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        public DateTimeOffset? Start
        {
            get;
            set;
        }

        /// <summary>
        /// The end time of rest execution.
        /// </summary>
        [DefaultValue(null)]
        public DateTimeOffset? End
        {
            get;
            set;
        }
    }
}
