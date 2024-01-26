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
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Add new task (Read, write or action) to the device.
    /// </summary>
    [DataContract]
    public class AddTask
    {
        /// <summary>
        /// Tasks to execute.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeSwagger(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeSwagger(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeSwagger(typeof(GXTask), nameof(GXTask.TriggerUser), nameof(GXTask.Creator))]
        public GXTask[] Tasks
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add task response.
    /// </summary>
    [DataContract]
    public class AddTaskResponse
    {
        /// <summary>
        /// New task identifiers.
        /// </summary>
        [DataMember]
        public Guid[] TaskIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from the tasks.
    /// </summary>
    [DataContract]
    public class ListTasks
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the tasks to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter tasks.
        /// </summary>

        [IncludeSwagger(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeSwagger(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeSwagger(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id))]
        public GXTask? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access tasks from all users.
        /// </summary>
        /// <remarks>
        /// If true, tasks from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }


        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public string[]? Select
        {
            get;
            set;
        }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending
        {
            get;
            set;
        }

        /// <summary>
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public Guid[]? Included
        {
            get;
            set;
        }

        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
        /// </remarks>
        public Guid[]? Exclude
        {
            get;
            set;
        }
    }

    /// <summary>
    /// List tasks response.
    /// </summary>
    [DataContract]
    public class ListTasksResponse
    {
        /// <summary>
        /// List of tasks.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeSwagger(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeSwagger(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id))]
        public GXTask[] Tasks
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the tasks.
        /// </summary>
        /// <remarks>
        /// With large databases reading the amount of the data can take a very long time.
        /// In those cases the count is set to -1.
        /// </remarks>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reserved for internal use. Get next executed task. Readers use this.
    /// </summary>
    [DataContract]
    public class GetNextTask
    {
        /// <summary>
        /// Agent identifier.
        /// </summary>
        [DataMember]
        public Guid AgentId
        {
            get;
            set;
        }

        /// <summary>
        /// Gateway identifier.
        /// </summary>
        /// <remarks>
        /// If gateway identifier is given, all tasks for that gateway are retreaved.
        /// </remarks>
        [DataMember]
        public Guid? GatewayId
        {
            get;
            set;
        }

        /// <summary>
        /// Device identifier.
        /// </summary>
        /// <remarks>
        /// If device identifier is given, all tasks for that device are retreaved.
        /// </remarks>
        [DataMember]
        public Guid? DeviceId
        {
            get;
            set;
        }

        /// <summary>
        /// Is listener or normal read asking next task.
        /// </summary>
        [DataMember]
        public bool Listener
        {
            get;
            set;
        }

        /// <summary>
        /// How long is the new task expected.
        /// </summary>
        [DataMember]
        public int WaitTime
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get next task response.
    /// </summary>
    [DataContract]
    public class GetNextTaskResponse
    {
        /// <summary>
        /// Executed tasks. Null if there are no operations to execute.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeSwagger(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeSwagger(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeSwagger(typeof(GXTask), nameof(GXTask.TriggerUser), nameof(GXTask.Creator))]
        public GXTask[]? Tasks
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Reserved for internal use. Mark task to executed. Readers use this.
    /// </summary>
    [DataContract]
    public class TaskDone
    {
        /// <summary>
        /// Executed tasks.
        /// </summary>
        [DataMember]

        [IncludeSwagger(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeSwagger(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeSwagger(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeSwagger(typeof(GXTask), nameof(GXTask.TriggerUser), nameof(GXTask.Creator))]
        public GXTask[] Tasks
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reserved for internal use. Task ready reply. Readers use this.
    /// </summary>
    [DataContract]
    public class TaskDoneResponse
    {

    }

    /// <summary>
    /// Reserved for internal use. Restart tasks.
    /// </summary>
    [DataContract]
    public class TaskRestart
    {
        /// <summary>
        /// Executed tasks.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXTask), nameof(GXTask.Id))]
        public GXTask[] Tasks
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reserved for internal use. Restart tasks reply.
    /// </summary>
    [DataContract]
    public class TaskRestartResponse
    {

    }

    /// <summary>
    /// Delete tasks.
    /// </summary>
    [DataContract]
    public class DeleteTask
    {
        /// <summary>
        /// Removed Tasks identifier(s).
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete tasks response.
    /// </summary>
    [DataContract]
    public class DeleteTaskResponse
    {
    }

    /// <summary>
    /// Clear tasks.
    /// </summary>
    [DataContract]
    public class ClearTask
    {
    }

    /// <summary>
    /// Clear errors response.
    /// </summary>
    [DataContract]
    public class ClearTaskResponse
    {
    }
}
