﻿//
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
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Gateway
{
    /// <summary>
    /// Gateway log table.
    /// </summary>
    [DataContract]
    public class GXGatewayLog : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXGatewayLog()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// Log levels from 0 to 4 are reserved for Gurux.DLMS.AMI.
        /// </remarks>
        /// <param name="level">Log severity level</param>
        public GXGatewayLog(int level)
        {
            Level = level;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// Log levels from 0 to 4 are reserved for Gurux.DLMS.AMI.
        /// </remarks>
        /// <param name="level">Log severity level</param>
        public GXGatewayLog(TraceLevel level)
        {
            Level = (int)level;
            Type = 0;
            Message = "";
        }
        /// <summary>
        /// Log Id.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Parent gateway.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public GXGateway? Gateway
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
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Log is active if closed time is not set.
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
        /// Log string.
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
        /// Log severity level.
        /// </summary>
        [DataMember]
        [DefaultValue(1)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public int? Level
        {
            get;
            set;
        }

        /// <summary>
        /// Log type.
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public int? Type
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

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Gateway != null && !string.IsNullOrEmpty(Gateway.Name))
            {
                return Gateway.Name + " " + Message;
            }
            if (!string.IsNullOrEmpty(Message))
            {
                return Message;
            }
            return nameof(GXGatewayLog);
        }

    }
}
