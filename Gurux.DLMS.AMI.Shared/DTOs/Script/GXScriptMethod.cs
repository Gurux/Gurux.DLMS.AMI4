// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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
