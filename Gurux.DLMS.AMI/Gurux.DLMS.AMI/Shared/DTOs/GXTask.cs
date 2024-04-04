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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Executed task and it's parameters.
    /// </summary>
    public class GXTask : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Request identifier.
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
        /// The creator of the task.
        /// </summary>
        [DataMember]
        [Index(false)]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        [DefaultValue(null)]
        public GXUser? Creator
        {
            get;
            set;
        }

        /// <summary>
        /// Task order number.
        /// </summary>
        /// <remarks>
        /// Task order number can be used to execute tasks in given order.
        /// </remarks>
        [DataMember]
        [DefaultValue(0)]
        [Filter(FilterType.Exact)]
        public int? Order
        {
            get;
            set;
        }

        /// <summary>
        /// Task batching.
        /// </summary>
        /// <remarks>
        /// Task batching is used to run group of tasks in the same batch.
        /// </remarks>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid? Batch
        {
            get;
            set;
        }

        /// <summary>
        /// Target of the task as a string.
        /// </summary>
        [StringLength(256)]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public string? Target
        {
            get;
            set;
        }

        /// <summary>
        /// Target script method.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXScriptMethod? ScriptMethod
        {
            get;
            set;
        }

        /// <summary>
        /// Target module.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXModule? Module
        {
            get;
            set;
        }

        /// <summary>
        /// Target device group.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXDeviceGroup? DeviceGroup
        {
            get;
            set;
        }

        /// <summary>
        /// Target device.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXDevice? Device
        {
            get;
            set;
        }

        /// <summary>
        /// Target COSEM object.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXObject? Object
        {
            get;
            set;
        }

        /// <summary>
        /// Target COSEM Attribute.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXAttribute? Attribute
        {
            get;
            set;
        }

        /// <summary>
        /// Schedule that triggers this task.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXSchedule? TriggerSchedule
        {
            get;
            set;
        }

        /// <summary>
        /// User that triggers this task.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? TriggerUser
        {
            get;
            set;
        }

        /// <summary>
        /// Script that triggers this task.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXScript? TriggerScript
        {
            get;
            set;
        }

        /// <summary>
        /// Module that triggers this task.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXScript? TriggerModule
        {
            get;
            set;
        }

        /// <summary>
        /// The agent that performs this task.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXAgent? OperatingAgent
        {
            get;
            set;
        }

        /// <summary>
        /// The device this task runs on.
        /// </summary>
        /// <remarks>
        /// This information is added to ensure that multiple agents are not running on the same meter.
        /// </remarks>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid? TargetDevice
        {
            get;
            set;
        }

        /// <summary>
        /// Target agent.
        /// </summary>
        /// <remarks>
        /// This information is used when agents are notified 
        /// from the new task and it's not saved to the DB.
        /// </remarks>
        [Ignore]
        public Guid? TargetAgent
        {
            get;
            set;
        }

        /// <summary>
        /// Target gateway.
        /// </summary>
        /// <remarks>
        /// This information is used when agents are notified 
        /// from the new task and it's not saved to the DB.
        /// </remarks>
        [Ignore]
        public Guid? TargetGateway
        {
            get;
            set;
        }

        /// <summary>
        /// Task type.
        /// </summary>
        [DataMember]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public TaskType? TaskType
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
        [DefaultValue(null)]
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when task is updated for the last time.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// Start execution time.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Start
        {
            get;
            set;
        }

        /// <summary>
        /// Time when task is ready.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Ready
        {
            get;
            set;
        }


        /// <summary>
        /// Task result.
        /// </summary>
        [DataMember]
        public string? Result
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute index.
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public int? Index
        {
            get;
            set;
        }

        /// <summary>
        /// Data to write.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        public string? Data
        {
            get;
            set;
        }

        /// <summary>
        /// Condition script is used to check should the task executed.
        /// </summary>
        /// <remarks>
        /// If the condition script is not set, the task is always executed.
        /// If condition script returns true, the task is executed.
        /// </remarks>
        /// 
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXScriptMethod? Condition
        {
            get;
            set;
        }

        /// <summary>
        /// Action script is executed after the task is executed.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXScriptMethod? Action
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

        /// <inheritdoc />
        public override string ToString()
        {
            string str = "";
            switch ((TaskType)TaskType.GetValueOrDefault())
            {
                case Shared.Enums.TaskType.Read:
                    str = Properties.Resources.Read;
                    break;
                case Shared.Enums.TaskType.Write:
                    str = Properties.Resources.Write;
                    break;
                case Shared.Enums.TaskType.Action:
                    str = Properties.Resources.Action;
                    break;
                default:
                    break;
            }
            str += " " + Target;
            if (Index != 0)
            {
                str += " #" + Index;
            }
            if (Ready != null)
            {
                str += " " + Properties.Resources.Compleated;
            }
            else if (Start != null)
            {
                str += " " + Properties.Resources.Started;
            }
            if (!string.IsNullOrEmpty(Result))
            {
                str += " " + Result;
            }
            return str;
        }

    }

    /// <summary>
    /// This class is used to read task foreign key values.
    /// </summary>
    public class GXTaskColumns
    {
        /// <summary>
        /// The task creator.
        /// </summary>
        public Guid? Creator
        {
            get;
            set;
        }
        /// <summary>
        /// Task order number.
        /// </summary>
        public int Order
        {
            get;
            set;
        }

        /// <summary>
        /// Task batching.
        /// </summary>
        public Guid? Batch
        {
            get;
            set;
        }

        /// <summary>
        /// Target of the task as a string.
        /// </summary>
        public string? Target
        {
            get;
            set;
        }

        /// <summary>
        /// Script method foreign key.
        /// </summary>
        public Guid? ScriptMethod
        {
            get;
            set;
        }

        /// <summary>
        /// Device group foreign key.
        /// </summary>
        public Guid? DeviceGroup
        {
            get;
            set;
        }
        /// <summary>
        /// Device foreign key.
        /// </summary>
        public Guid? Device
        {
            get;
            set;
        }
        /// <summary>
        /// Object foreign key.
        /// </summary>
        public Guid? Object
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute foreign key.
        /// </summary>
        public Guid? Attribute
        {
            get;
            set;
        }

        /// <summary>
        /// Schedule foreign key.
        /// </summary>
        public Guid? TriggerSchedule
        {
            get;
            set;
        }

        /// <summary>
        /// Trigger user foreign key.
        /// </summary>
        public string? TriggerUser
        {
            get;
            set;
        }
        /// <summary>
        /// Schedule foreign key.
        /// </summary>
        public Guid? TriggerScript
        {
            get;
            set;
        }

        /// <summary>
        /// Trigger foreign key.
        /// </summary>
        public Guid? TriggerModule
        {
            get;
            set;
        }

        /// <summary>
        /// Agent foreign key.
        /// </summary>
        public Guid? OperatingAgent
        {
            get;
            set;
        }
        /// <summary>
        /// The device this task runs on.
        /// </summary>
        /// <remarks>
        /// This information is added to ensure that multiple agents are not running on the same meter.
        /// </remarks>
        public Guid? TargetDevice
        {
            get;
            set;
        }
    }
}
