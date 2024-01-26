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
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.User
{
    /// <summary>
    /// Stamp is used to tell what objects user has seen.
    /// </summary>
    public class GXUserStamp : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXUserStamp()
        {
        }

        /// <summary>
        /// Stamp identifier.
        /// </summary>
        [Description("Stamp identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// The stamp Creator.
        /// </summary>
        [DataMember]
        [Index(false)]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When was the stamp last updated.
        /// </summary>
        [Description("When was the stamp last updated.")]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the stamp.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        [JsonIgnore]
        public bool Modified
        {
            get;
            set;
        }

        /// <summary>
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        public string? ConcurrencyStamp
        {
            get;
            set;
        }

        /// <summary>
        /// Stamped target.
        /// </summary>
        [Description("Stamped target.")]
        [DefaultValue(null)]
        [Filter(FilterType.Equals)]
        public Guid? Target
        {
            get;
            set;
        }

        /// <summary>
        /// Target type.
        /// </summary>
        /// <remarks>
        /// Target type can be e.g. user, schedule, device, etc.
        /// </remarks>
        [Description("Target type.")]
        [Filter(FilterType.Equals)]
        [StringLength(32)]
        [Required]
        public string? TargetType
        {
            get;
            set;
        }

        /// <summary>
        /// Number of the errors.
        /// </summary>
        [Ignore]
        public int Errors
        {
            get;
            set;
        }

        /// <summary>
        /// Number of the warnings.
        /// </summary>
        [Ignore]
        public int Warnings
        {
            get;
            set;
        }

        /// <summary>
        /// Number of the informational messages.
        /// </summary>
        [Ignore]
        public int Informational
        {
            get;
            set;
        }

        /// <summary>
        /// Number of the verbose messages.
        /// </summary>
        [Ignore]
        public int Verboses
        {
            get;
            set;
        }

        /// <summary>
        /// Get user stamp count.
        /// </summary>
        /// <returns></returns>
        public int GetCount()
        {
            if (Errors != 0)
            {
                return Errors;
            }
            if (Warnings != 0)
            {
                return Warnings;
            }
            if (Informational != 0)
            {
                return Informational;
            }
            if (Verboses != 0)
            {
                return Verboses;
            }
            return 0;
        }

        /// <summary>
        /// Get user stamp count.
        /// </summary>
        /// <returns></returns>
        public int GetTotalCount()
        {
            return Errors + Warnings + Informational + Verboses;
        }

        /// <summary>
        /// Get used trace level.
        /// </summary>
        /// <returns></returns>
        public TraceLevel GetTraceLevel()
        {
            if (Errors != 0)
            {
                return TraceLevel.Error;
            }
            if (Warnings != 0)
            {
                return TraceLevel.Warning;
            }
            if (Informational != 0)
            {
                return TraceLevel.Info;
            }
            if (Verboses != 0)
            {
                return TraceLevel.Verbose;
            }
            return TraceLevel.Off;
        }

        /// <summary>
        /// Add stamp counts to the target.
        /// </summary>
        /// <param name="target">Target.</param>
        public void AddTo(GXUserStamp target)
        {
            if (Errors == 0 && Warnings == 0 &&
                Informational == 0 &&
                Verboses == 0)
            {
                //Clear values.
                target.Errors = 0;
                target.Warnings = 0;
                target.Informational = 0;
                target.Verboses = 0;
            }
            else
            {
                target.Errors += Errors;
                target.Warnings += Warnings;
                target.Informational += Informational;
                target.Verboses += Verboses;
            }
        }

        /// <summary>
        /// Copy stamp counts to the target.
        /// </summary>
        /// <param name="target">Target.</param>
        public void CopyTo(GXUserStamp target)
        {
            target.Errors = Errors;
            target.Warnings = Warnings;
            target.Informational = Informational;
            target.Verboses = Verboses;
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

        /// <summary>
        /// Update concurrency stamp.
        /// </summary>
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string str;
            if (!string.IsNullOrEmpty(Creator?.UserName))
            {
                str = Creator.UserName;
            }
            else
            {
                str = nameof(GXUserStamp);
            }
            return str;
        }
    }
}
