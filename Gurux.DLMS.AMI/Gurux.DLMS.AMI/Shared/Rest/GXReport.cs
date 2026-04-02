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
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get report.
    /// </summary>
    public class GetReportResponse
    {
        /// <summary>
        /// Report information.
        /// </summary>        
        [ExcludeOpenApi(typeof(GXReport), nameof(GXReport.Logs), nameof(GXReport.Scripts)
            , nameof(GXReport.ScriptMethod))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id), nameof(GXAgent.Name))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id), nameof(GXAgentGroup.Name))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id), nameof(GXGateway.Name))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id), nameof(GXGatewayGroup.Name))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id), nameof(GXSchedule.Name))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id), nameof(GXScheduleGroup.Name))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id), nameof(GXScript.Name))]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id), nameof(GXScriptGroup.Name))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id), nameof(GXWorkflow.Name))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id), nameof(GXWorkflowGroup.Name))]
        [IncludeOpenApi(typeof(GXReportGroup), nameof(GXReportGroup.Id), nameof(GXReportGroup.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id), nameof(GXAttribute.Template))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id), nameof(GXObject.Template))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXReportGroup), nameof(GXReportGroup.Id), nameof(GXReportGroup.Name))]
        [IncludeOpenApi(typeof(GXSubtotalGroup), nameof(GXSubtotalGroup.Id), nameof(GXSubtotalGroup.Name))]
        [IncludeOpenApi(typeof(GXSubtotal), nameof(GXSubtotal.Id), nameof(GXSubtotal.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id),
                nameof(GXComponentView.Name))]
        [IncludeOpenApi(typeof(GXComponentViewGroup), nameof(GXComponentViewGroup.Id),
                nameof(GXComponentViewGroup.Name))]
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
        /// Amount of the reports to retrieve.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter reports.
        /// </summary>
        [ExcludeOpenApi(typeof(GXReport),
            nameof(GXReport.ReportGroups),
            nameof(GXReport.Devices),
            nameof(GXReport.DeviceGroups),
            nameof(GXReport.DeviceAttributeTemplates),
            nameof(GXReport.DeviceGroupAttributeTemplates),
            nameof(GXReport.Schedules),
            nameof(GXReport.ScheduleGroups),
            nameof(GXReport.Scripts),
            nameof(GXReport.ScriptMethod),
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
            nameof(GXReport.AgentGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
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
        [ExcludeOpenApi(typeof(GXReport),
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
        [IncludeOpenApi(typeof(GXReportGroup), nameof(GXReportGroup.Id), nameof(GXReportGroup.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
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
        [ExcludeOpenApi(typeof(GXReport),
            nameof(GXReport.Logs),
            nameof(GXReport.Creator))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXReportGroup), nameof(GXReportGroup.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [IncludeOpenApi(typeof(GXAgent), nameof(GXAgent.Id))]
        [IncludeOpenApi(typeof(GXAgentGroup), nameof(GXAgentGroup.Id))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeOpenApi(typeof(GXGatewayGroup), nameof(GXGatewayGroup.Id))]
        [IncludeOpenApi(typeof(GXSchedule), nameof(GXSchedule.Id))]
        [IncludeOpenApi(typeof(GXScheduleGroup), nameof(GXScheduleGroup.Id))]
        [IncludeOpenApi(typeof(GXScript), nameof(GXScript.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXScriptGroup), nameof(GXScriptGroup.Id))]
        [IncludeOpenApi(typeof(GXWorkflow), nameof(GXWorkflow.Id))]
        [IncludeOpenApi(typeof(GXWorkflowGroup), nameof(GXWorkflowGroup.Id))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id))]
        [IncludeOpenApi(typeof(GXSubtotal), nameof(GXSubtotal.Id))]
        [IncludeOpenApi(typeof(GXSubtotalGroup), nameof(GXSubtotalGroup.Id))]
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
        [IncludeOpenApi(typeof(GXReport), nameof(GXReport.Id))]
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
        [IncludeOpenApi(typeof(GXReport),
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
