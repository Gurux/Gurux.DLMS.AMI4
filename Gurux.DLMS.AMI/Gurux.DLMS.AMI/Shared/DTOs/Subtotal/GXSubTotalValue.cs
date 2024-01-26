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
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text;

namespace Gurux.DLMS.AMI.Shared.DTOs.Subtotal
{
    /// <summary>
    /// Sub total attribute value.
    /// </summary>
    [DataContract]
    [IndexCollection(true, nameof(Subtotal), nameof(StartTime), nameof(Target))]
    public class GXSubtotalValue : IUnique<Guid>
    {
        /// <summary>
        /// Value identifier.
        /// </summary>
        [DataMember]
        [Description("Subtotal value identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Subtotal.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXSubtotal), OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXSubtotal? Subtotal
        {
            get;
            set;
        }

        /// <summary>
        /// Subtotal attribute value.
        /// </summary>
        [DataMember]
        [Description("Subtotal attribute value.")]
        [Filter(FilterType.Contains)]
        public string? Value
        {
            get;
            set;
        }

        /// <summary>
        /// Target type.
        /// </summary>
        /// <remarks>
        /// Target type tells is subtotal target attribute,
        /// agent, agent group, gateway or gateway group. 
        /// </remarks>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public string? TargetType
        {
            get;
            set;
        }

        /// <summary>
        /// Target identifier.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid? Target
        {
            get;
            set;
        }


        /// <summary>
        /// Start time.
        /// </summary>
        [DataMember]
        [Description("Start time.")]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        [Index(false)]
        public DateTimeOffset? StartTime
        {
            get;
            set;
        }
        /// <summary>
        /// End time.
        /// </summary>
        [DataMember]
        [Description("End time.")]
        [Filter(FilterType.LessOrEqual)]
        [Index(false)]
        [IsRequired]
        public DateTimeOffset? EndTime
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (StartTime != null)
            {
                sb.Append(StartTime.ToString());
            }
            if (EndTime != null)
            {
                sb.Append(" to ");
                sb.Append(EndTime.ToString());
            }
            sb.Append(" ");
            sb.Append(Value);
            return sb.ToString();
        }
    }
}
