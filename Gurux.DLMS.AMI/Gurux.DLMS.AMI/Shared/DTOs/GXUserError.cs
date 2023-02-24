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
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Error that is caused by user.
    /// </summary>
    [DataContract]
    public class GXUserError : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// Error levels from 0 to 4 are reserved for Gurux.DLMS.AMI.
        /// </remarks>
        public GXUserError() : this((int)TraceLevel.Error)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// Error levels from 0 to 4 are reserved for Gurux.DLMS.AMI.
        /// </remarks>
        /// <param name="level">Error severity level</param>
        public GXUserError(int level)
        {
            Level = level;
            Message = "";
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// Error levels from 0 to 4 are reserved for Gurux.DLMS.AMI.
        /// </remarks>
        /// <param name="level">Error severity level</param>
        public GXUserError(TraceLevel level)
        {
            Level = (int)level;
            Message = "";
        }

        /// <summary>
        /// Error Id.
        /// </summary>
        [DataMember]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// The user who caused this error.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        public GXUser? User
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
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Error is active if closed time is not set.
        /// </summary>
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Closed
        {
            get;
            set;
        }

        /// <summary>
        /// Error string.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Message
        {
            get;
            set;
        }

        /// <summary>
        /// Stack trace.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? StackTrace
        {
            get;
            set;
        }
        

        /// <summary>
        /// Error severity level.
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public int? Level
        {
            get;
            set;
        }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }
    }
}
