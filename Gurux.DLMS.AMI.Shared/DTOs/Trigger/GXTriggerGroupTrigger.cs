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
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Trigger
{
    /// <summary>Associates a trigger with a trigger group.</summary>
    [Description("Associates a trigger with a trigger group.")]
    [DataContract(Name = "GXTriggerGroupTrigger"), Serializable]
    [IndexCollection(true, nameof(TriggerGroupId), nameof(TriggerId), Clustered = true)]
    public class GXTriggerGroupTrigger
    {
        /// <summary>The database identifier of the trigger group.</summary>
        [Description("The database identifier of the trigger group.")]
        [DataMember(Name = "TriggerGroupID")]
        [ForeignKey(typeof(GXTriggerGroup), OnDelete = ForeignKeyDelete.None)]
        public Guid TriggerGroupId { get; set; }

        /// <summary>The database identifier of the trigger.</summary>
        [Description("The database identifier of the trigger.")]
        [DataMember(Name = "TriggerID")]
        [ForeignKey(typeof(GXTrigger), OnDelete = ForeignKeyDelete.Cascade)]
        public Guid TriggerId { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when agent group was removed from user group.
        /// </summary>
        [Description("Time when agent group was removed from user group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }
    }
}
