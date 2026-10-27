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
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.View
{
    /// <summary>Associates a table view with a view group.</summary>
    [Description("Associates a table view with a view group.")]
    [DataContract(Name = "GXViewGroupView"), Serializable]
    [IndexCollection(true, nameof(ViewGroupId), nameof(ViewId), Clustered = true)]
    public class GXViewGroupView
    {
        /// <summary>The database identifier of the content group.</summary>
        [Description("The database identifier of the content group.")]
        [DataMember(Name = "ViewGroupId")]
        [ForeignKey(typeof(GXViewGroup), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        //ForeignKeyDelete is None because creator of the view is causing multiple cascade paths error in MSSQL.
        public Guid ViewGroupId { get; set; }

        /// <summary>Identifier of the view assigned to the group.</summary>
        [Description("Identifier of the view assigned to the group.")]
        [DataMember(Name = "ViewId")]
        [ForeignKey(typeof(GXView), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid ViewId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the view was added to the view group.
        /// </summary>
        [Description("Creation time. The time when the view was added to the view group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when block was removed from view group.
        /// </summary>
        [Description("Time when block was removed from view group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
