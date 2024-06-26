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
using Gurux.Common;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get report.
    /// </summary>
    public class GetReportResponse
    {
        /// <summary>
        /// Device information.
        /// </summary>        
        [IncludeSwagger(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeSwagger(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeSwagger(typeof(GXGateway), nameof(GXGateway.Id), nameof(GXGateway.Name))]
        [IncludeSwagger(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id), nameof(GXGatewayGroup.Name))]
        [IncludeSwagger(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeSwagger(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeSwagger(typeof(GXScript), nameof(GXScript.Id), nameof(GXScript.Name))]
        [IncludeSwagger(typeof(GXScriptGroup), nameof(GXScriptGroup.Id), nameof(GXScriptGroup.Name))]
        [IncludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeSwagger(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeSwagger(typeof(GXReportGroup), nameof(GXReportGroup.Id), nameof(GXReportGroup.Name))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeSwagger(typeof(GXAttribute), nameof(GXAttribute.Id), nameof(GXAttribute.Template))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id), nameof(GXObject.Template))]
        [IncludeSwagger(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [IncludeSwagger(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeSwagger(typeof(GXReportGroup), nameof(GXReportGroup.Id), nameof(GXReportGroup.Name))]
        [IncludeSwagger(typeof(GXSubtotalGroup), nameof(GXSubtotalGroup.Id), nameof(GXSubtotalGroup.Name))]
        [IncludeSwagger(typeof(GXSubtotal), nameof(GXSubtotal.Id), nameof(GXSubtotal.Name))]
        public GXReport? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from reports.
    /// </summary>
    [DataContract]
    public class ListReports : IGXRequest<ListReportsResponse>
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
        /// Amount of the reports to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter reports.
        /// </summary>
        [ExcludeSwagger(typeof(GXReport),
            nameof(GXReport.ReportGroups),
            nameof(GXReport.Devices),
            nameof(GXReport.DeviceGroups),
            nameof(GXReport.DeviceAttributeTemplates),
            nameof(GXReport.DeviceGroupAttributeTemplates),
            nameof(GXReport.Schedules),
            nameof(GXReport.ScheduleGroups),
            nameof(GXReport.Scripts),
            nameof(GXReport.ScriptGroups),
            nameof(GXReport.Workflows),
            nameof(GXReport.WorkflowGroups),
            nameof(GXReport.Gateways),
            nameof(GXReport.GatewayGroups),
            nameof(GXReport.Logs),
            nameof(GXReport.Agents),
            nameof(GXReport.Users),
            nameof(GXReport.SubtotalGroups),
            nameof(GXReport.Subtotals),
            nameof(GXReport.UserGroups),
            nameof(GXReport.AgentGroups),
            nameof(GXReport.Creator))]
        public GXReport? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access reports from all users.
        /// </summary>
        /// <remarks>
        /// If true, reports from all users are retreaved, not just current user. 
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
    /// Report items reply.
    /// </summary>
    [DataContract]
    public class ListReportsResponse
    {
        /// <summary>
        /// List of report items.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXReport),
            nameof(GXReport.Devices),
            nameof(GXReport.DeviceGroups),
            nameof(GXReport.DeviceAttributeTemplates),
            nameof(GXReport.DeviceGroupAttributeTemplates),
            nameof(GXReport.Schedules),
            nameof(GXReport.ScheduleGroups),
            nameof(GXReport.Scripts),
            nameof(GXReport.ScriptGroups),
            nameof(GXReport.Workflows),
            nameof(GXReport.WorkflowGroups),
            nameof(GXReport.Gateways),
            nameof(GXReport.GatewayGroups),
            nameof(GXReport.Logs),
            nameof(GXReport.SubtotalGroups),
            nameof(GXReport.Subtotals),
            nameof(GXReport.Agents),
            nameof(GXReport.AgentGroups),
            nameof(GXReport.Creator))]
        [IncludeSwagger(typeof(GXReportGroup), nameof(GXReportGroup.Id), nameof(GXReportGroup.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        public GXReport[]? Reports
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the reportrs.
        /// </summary>
        [DataMember]
        /// <remarks>
        /// With large databases reading the amount of the data can take a very long time.
        /// In those cases the count is set to -1.
        /// </remarks>
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update reports.
    /// </summary>
    [DataContract]
    public class UpdateReport : IGXRequest<UpdateReportResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateReport()
        {
            Reports = new List<GXReport>();
        }

        /// <summary>
        /// Reports to update.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXReport),
            nameof(GXReport.Logs),
            nameof(GXReport.Creator))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeSwagger(typeof(GXReportGroup), nameof(GXReportGroup.Id))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeSwagger(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeSwagger(typeof(GXAgentGroup), nameof(GXAgentGroup.Id))]
        [IncludeSwagger(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeSwagger(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id))]
        [IncludeSwagger(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeSwagger(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id))]
        [IncludeSwagger(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeSwagger(typeof(GXScriptGroup), nameof(GXScriptGroup.Id))]
        [IncludeSwagger(typeof(GXWorkflow), nameof(GXWorkflow.Id))]
        [IncludeSwagger(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id))]
        [IncludeSwagger(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id))]
        [IncludeSwagger(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id))]
        [IncludeSwagger(typeof(GXSubtotal), nameof(GXSubtotal.Id))]
        [IncludeSwagger(typeof(GXSubtotalGroup), nameof(GXSubtotalGroup.Id))]
        public List<GXReport> Reports
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update reports reply.
    /// </summary>
    [DataContract]
    public class UpdateReportResponse
    {
        /// <summary>
        /// New report identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete reports.
    /// </summary>
    [DataContract]
    public class RemoveReport : IGXRequest<RemoveReportResponse>
    {
        /// <summary>
        /// Removed report identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from Delete report.
    /// </summary>
    [DataContract]
    public class RemoveReportResponse
    {
    }

    /// <summary>
    /// Send report.
    /// </summary>
    [DataContract]
    public class SendReport : IGXRequest<SendReportResponse>
    {
        /// <summary>
        /// Generated reports.
        /// </summary>
        [DataMember]
        public GXReport[]? Reports
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Send reports response.
    /// </summary>
    [DataContract]
    public class SendReportResponse
    {
        /// <summary>
        /// The result of the calculated reports.
        /// </summary>
        /// <remarks>
        /// The result is returned only when Report delivery is set to Caller.
        /// </remarks>
        [DataMember]
        [IncludeSwagger(typeof(GXReport),
            nameof(GXReport.Id),
            nameof(GXReport.Delivery))]
        public GXReport[]? Reports
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear report values.
    /// </summary>
    [DataContract]
    public class ClearReport : IGXRequest<ClearReportResponse>
    {
        /// <summary>
        /// Calculate reports IDs.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear reports response.
    /// </summary>
    [DataContract]
    public class ClearReportResponse
    {
    }
}
