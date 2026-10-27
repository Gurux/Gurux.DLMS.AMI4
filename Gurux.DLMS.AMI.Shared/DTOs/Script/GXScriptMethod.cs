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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Script
{
    /// <summary>An invocable script method and the resources that reference it.</summary>
    [Description("An invocable script method and the resources that reference it.")]
    [DataContract(Name = "GXScriptMethod"), Serializable]
    public class GXScriptMethod : IUnique<Guid>
    {
        /// <summary>Unique identifier of the script method.</summary>
        [Description("Unique identifier of the script method.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the script method.
        /// </summary>
		[Description("Name of the script method.")]
		[DataMember]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Script method description.
        /// </summary>
        [Description("Script method description.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        public string? Description { get; set; }

        /// <summary>Whether the method returns a value.</summary>
		[Description("Whether the method returns a value.")]
		[DataMember]
        public bool IsFunction { get; set; }

        /// <summary>Whether the script method executes asynchronously.</summary>
        [Description("Whether the script method executes asynchronously.")]
        [DataMember]
        public bool IsAsynchronous { get; set; }

        /// <summary>
        /// Parent script.
        /// </summary>
        [Description("Parent script.")]
        [DataMember, ForeignKey(typeof(GXScript), OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public GXScript? Script { get; set; }

        /// <summary>Blocks that reference this script method.</summary>
        [Description("Blocks that reference this script method.")]
        [DataMember]
        [ForeignKey(typeof(GXBlock))]
        [Filter(FilterType.Contains)]
        public List<GXBlock> Blocks { get; set; }

        /// <summary>Agents that reference this script method.</summary>
        [Description("Agents that reference this script method.")]
        [DataMember]
        [ForeignKey(typeof(GXAgent))]
        [Filter(FilterType.Contains)]
        public List<GXAgent> Agents { get; set; }

        /// <summary>Notifications that reference this script method.</summary>
        [Description("Notifications that reference this script method.")]
        [DataMember]
        [ForeignKey(typeof(GXNotification))]
        [Filter(FilterType.Contains)]
        public List<GXNotification> Notifications { get; set; }
        /// <summary>Initializes a new instance of GXScriptMethod.</summary>
        [Description("Initializes a new instance of GXScriptMethod.")]
        public GXScriptMethod()
        {
            Blocks = [];
            Agents = [];
            Notifications = [];
        }
    }
}
