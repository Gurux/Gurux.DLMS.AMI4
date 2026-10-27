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
// This file is a part of Gurux Attribute Framework.
//
// Gurux Attribute Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Attribute Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>An error associated with a specific COSEM attribute.</summary>
    [Description("An error associated with a specific COSEM attribute.")]
    [DataContract]
    public class GXAttributeError : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXAttributeError.</summary>
        [Description("Initializes a new instance of GXAttributeError.")]
        public GXAttributeError()
        {
        }

        /// <summary>Initializes a new instance of GXAttributeError.</summary>
        /// <remarks>
        /// Error levels from 0 to 4 are reserved for Gurux.DLMS.AMI.
        /// </remarks>
        /// <param name="level">Error severity level</param>
        [Description("Initializes a new instance of GXAttributeError.")]
        public GXAttributeError(int level)
        {
            Level = level;
        }

        /// <summary>Initializes a new instance of GXAttributeError.</summary>
        /// <remarks>
        /// Error levels from 0 to 4 are reserved for Gurux.DLMS.AMI.
        /// </remarks>
        /// <param name="level">Error severity level</param>
        [Description("Initializes a new instance of GXAttributeError.")]
        public GXAttributeError(TraceLevel level) : this((int)level)
        {
        }

        /// <summary>Error identifier.</summary>
        [Description("Error identifier.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The attribute that caused the error.
        /// </summary>
        [Description("The attribute that caused the error.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [DefaultValue(null)]
        public GXAttribute? Attribute { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [TimeStorageUnit(TimeStorageUnit.Milliseconds)]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Error is active if closed time is not set.
        /// </summary>
        [Description("Error is active if closed time is not set.")]
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Closed { get; set; }

        /// <summary>
        /// Error string.
        /// </summary>
        [Description("Error string.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Message { get; set; }

        /// <summary>
        /// Stack trace.
        /// </summary>
        [Description("Stack trace.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? StackTrace { get; set; }

        /// <summary>
        /// Error severity level.
        /// </summary>
        [Description("Error severity level.")]
        [DataMember]
        [DefaultValue(0)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public int? Level { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
        [Description("Initializes the creation timestamp before insertion if it has not been set.")]
        public override void BeforeAdd()
        {
            if (CreationTime == null)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string str = "";
            if (Attribute != null && Attribute.Template != null && !string.IsNullOrEmpty(Attribute.Template.Name))
            {
                str = Attribute.Template.Name;
            }
            if (!string.IsNullOrEmpty(Message))
            {
                str += " " + Message;
            }
            if (str != "")
            {
                return str;
            }
            return nameof(GXAttributeError);
        }
    }
}
