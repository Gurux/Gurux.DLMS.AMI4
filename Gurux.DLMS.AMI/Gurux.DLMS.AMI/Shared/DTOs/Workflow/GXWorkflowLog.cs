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

namespace Gurux.DLMS.AMI.Shared.DTOs.Workflow
{
    /// <summary>
    /// Error that is caused by workflow.
    /// </summary>
    [DataContract]
    public class GXWorkflowLog : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXWorkflowLog()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// Error levels from 0 to 4 are reserved for Gurux.DLMS.AMI.
        /// </remarks>
        /// <param name="level">Error severity level</param>
        public GXWorkflowLog(TraceLevel level) : this((int)level)
        {
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// Error levels from 0 to 4 are reserved for Gurux.DLMS.AMI.
        /// </remarks>
        /// <param name="level">Error severity level</param>
        public GXWorkflowLog(int level)
        {
            Level = level;
            Type = 0;
        }

        /// <summary>
        /// Error Id.
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
        /// The workflow that caused the error.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [DefaultValue(null)]
        [Index(false)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public GXWorkflow? Workflow
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
        /// Workflow log message.
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
            string str = "";
            if (Workflow != null && !string.IsNullOrEmpty(Workflow.Name))
            {
                str = Workflow.Name;
            }
            if (!string.IsNullOrEmpty(Message))
            {
                str += " " + Message;
            }
            if (str != "")
            {
                return str;
            }
            return nameof(GXWorkflowLog);
        }
    }
}
