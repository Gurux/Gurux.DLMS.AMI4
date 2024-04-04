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
using System.Security.Claims;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Client.Shared;
using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using System.Data;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class ReportRepository : IReportRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IReportGroupRepository _reportGroupRepository;
        private readonly IReport _report;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ReportRepository(IGXHost host,
            IUserRepository userRepository,
            IServiceProvider serviceProvider,
            IReportGroupRepository reportGroupRepository,
            IGXEventsNotifier eventsNotifier,
            IReport subTotal)
        {
            _host = host;
            _eventsNotifier = eventsNotifier;
            _userRepository = userRepository;
            _serviceProvider = serviceProvider;
            _reportGroupRepository = reportGroupRepository;
            _report = subTotal;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? reportId)
        {
            GXSelectArgs args = GXQuery.GetUsersByReport(ServerHelpers.GetUserId(user), reportId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? reportIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByReports(ServerHelpers.GetUserId(user), reportIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User,
            IEnumerable<Guid> reports,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.ReportManager)))
            {
                throw new UnauthorizedAccessException();
            }
            GXSelectArgs arg = GXSelectArgs.Select<GXReport>(a => a.Id, q => reports.Contains(q.Id));
            List<GXReport> list = _host.Connection.Select<GXReport>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXReport, List<string>> updates = new();
            foreach (GXReport it in list)
            {
                it.Removed = now;
                List<string> users = await GetUsersAsync(User, it.Id);
                if (delete)
                {
                    await _host.Connection.DeleteAsync(GXDeleteArgs.DeleteById<GXReport>(it.Id));
                }
                else
                {
                    _host.Connection.Update(GXUpdateArgs.Update(it, q => q.Removed));
                }
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                GXReport tmp = new GXReport() { Id = it.Key.Id };
                await _eventsNotifier.ReportDelete(it.Value, new GXReport[] { tmp });
            }
        }

        /// <inheritdoc />
        public async Task<GXReport[]> ListAsync(
            ClaimsPrincipal user,
            ListReports? request,
            ListReportsResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            if (request != null && request.AllUsers && user.IsInRole(GXRoles.Admin))
            {
                //Admin can see all the reports.
                arg = GXSelectArgs.SelectAll<GXReport>();
            }
            else
            {
                string? userId = ServerHelpers.GetUserId(user);
                arg = GXQuery.GetReportsByUser(userId, null);
            }
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXReport>(w => !request.Exclude.Contains(w.Id));
                }
                if (request.Included != null && request.Included.Any())
                {
                    arg.Where.And<GXReport>(w => request.Included.Contains(w.Id));
                }
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXReport>(request.OrderBy);
            }
            else
            {
                arg.OrderBy.Add<GXReport>(q => q.CreationTime);
                arg.Descending = true;
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXReport>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request?.Select != null && request.Select.Contains(TargetType.User))
            {
                arg.Joins.AddInnerJoin<GXReport, GXUser>(j => j.Creator, j => j.Id);
                arg.Columns.Add<GXUser>(c => c.Id);
            }
            GXReport[] reports = (await _host.Connection.SelectAsync<GXReport>(arg)).ToArray();
            if (response != null)
            {
                response.Reports = reports;
                if (response.Count == 0)
                {
                    response.Count = reports.Length;
                }
            }
            return reports;
        }

        /// <inheritdoc />
        public async Task<GXReport> ReadAsync(
            ClaimsPrincipal user,
            Guid id)
        {
            bool isAdmin = user.IsInRole(GXRoles.Admin);
            string userId = ServerHelpers.GetUserId(user);
            GXSelectArgs arg;
            if (user == null || isAdmin)
            {
                //Admin can see all the reports.
                arg = GXSelectArgs.SelectAll<GXReport>(w => w.Id == id);
                arg.Joins.AddLeftJoin<GXReport, GXReportGroupReport>(x => x.Id, y => y.ReportId);
                arg.Joins.AddLeftJoin<GXReportGroupReport, GXReportGroup>(j => j.ReportGroupId, j => j.Id);
            }
            else
            {
                arg = GXQuery.GetReportsByUser(userId, id);
                arg.Joins.AddInnerJoin<GXReportGroupReport, GXReportGroup>(j => j.ReportGroupId, j => j.Id);
            }
            arg.Columns.Add<GXReportGroup>();
            arg.Columns.Exclude<GXReportGroup>(e => e.Reports);
            arg.Distinct = true;
            GXReport report = await _host.Connection.SingleOrDefaultAsync<GXReport>(arg);
            if (report == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get agents.
            arg = GXSelectArgs.Select<GXAgent>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXReport, GXReportAgent>(y => y.Id, x => x.ReportId);
            arg.Joins.AddInnerJoin<GXReportAgent, GXAgent>(y => y.AgentId, x => x.Id);
            arg.Where.And<GXReport>(w => w.Id == report.Id);
            report.Agents = (await _host.Connection.SelectAsync<GXAgent>(arg)).ToList();
            //Get gateways.
            arg = GXSelectArgs.Select<GXGateway>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXReport, GXReportGateway>(y => y.Id, x => x.ReportId);
            arg.Joins.AddInnerJoin<GXReportGateway, GXGateway>(y => y.GatewayId, x => x.Id);
            arg.Where.And<GXReport>(w => w.Id == report.Id);
            report.Gateways = (await _host.Connection.SelectAsync<GXGateway>(arg)).ToList();
            //Get device attributes with own query.
            arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Name, s.ObjectTemplate, s.Index });
            arg.Joins.AddInnerJoin<GXReport, GXReportDeviceAttributeTemplate>(y => y.Id, x => x.ReportId);
            arg.Joins.AddInnerJoin<GXReportDeviceAttributeTemplate, GXAttributeTemplate>(y => y.AttributeTemplateId, x => x.Id);
            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXObjectTemplate>(y => y.ObjectTemplate, x => x.Id);
            arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name, s.LogicalName, s.Attributes });
            arg.Columns.Exclude<GXObjectTemplate>(e => e.Attributes);
            arg.Where.And<GXReport>(w => w.Id == report.Id);
            report.DeviceAttributeTemplates = (await _host.Connection.SelectAsync<GXAttributeTemplate>(arg)).ToList();
            //Get devices with own query.
            arg = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name, s.Template });
            arg.Columns.Add<GXDeviceTemplate>(s => new { s.Id });
            arg.Joins.AddInnerJoin<GXReport, GXReportDevice>(y => y.Id, x => x.ReportId);
            arg.Joins.AddInnerJoin<GXReportDevice, GXDevice>(y => y.DeviceId, x => x.Id);
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(y => y.Template, x => x.Id);
            arg.Where.And<GXReport>(w => w.Id == report.Id);
            report.Devices = (await _host.Connection.SelectAsync<GXDevice>(arg)).ToList();
            //Get device groups with own query.
            arg = GXSelectArgs.SelectAll<GXDeviceGroup>();
            arg.Joins.AddInnerJoin<GXReport, GXReportDeviceGroup>(y => y.Id, x => x.ReportId);
            arg.Joins.AddInnerJoin<GXReportDeviceGroup, GXDeviceGroup>(y => y.DeviceGroupId, x => x.Id);
            arg.Where.And<GXReport>(w => w.Id == report.Id);
            report.DeviceGroups = (await _host.Connection.SelectAsync<GXDeviceGroup>(arg)).ToList();
            //Get device group attributes with own query.
            arg = GXSelectArgs.Select<GXAttributeTemplate>(s => new { s.Id, s.Name, s.ObjectTemplate, s.Index });
            arg.Joins.AddInnerJoin<GXReport, GXReportDeviceGroupAttributeTemplate>(y => y.Id, x => x.ReportId);
            arg.Joins.AddInnerJoin<GXReportDeviceGroupAttributeTemplate, GXAttributeTemplate>(y => y.AttributeTemplateId, x => x.Id);
            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXObjectTemplate>(y => y.ObjectTemplate, x => x.Id);
            arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name, s.LogicalName, s.Attributes });
            arg.Columns.Exclude<GXObjectTemplate>(e => e.Attributes);
            arg.Where.And<GXReport>(w => w.Id == report.Id);
            report.DeviceGroupAttributeTemplates = (await _host.Connection.SelectAsync<GXAttributeTemplate>(arg)).ToList();

            //Get agent groups.
            arg = GXSelectArgs.Select<GXAgentGroup>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXReport, GXReportAgentGroup>(y => y.Id, x => x.ReportId);
            arg.Joins.AddInnerJoin<GXReportAgentGroup, GXAgentGroup>(y => y.AgentGroupId, x => x.Id);
            arg.Where.And<GXReport>(w => w.Id == report.Id);
            report.AgentGroups = (await _host.Connection.SelectAsync<GXAgentGroup>(arg)).ToList();
            //Get gateway groups.
            arg = GXSelectArgs.Select<GXGatewayGroup>(s => new { s.Id, s.Name });
            arg.Joins.AddInnerJoin<GXReport, GXReportGatewayGroup>(y => y.Id, x => x.ReportId);
            arg.Joins.AddInnerJoin<GXReportGatewayGroup, GXGatewayGroup>(y => y.GatewayGroupId, x => x.Id);
            arg.Where.And<GXReport>(w => w.Id == report.Id);
            report.GatewayGroups = (await _host.Connection.SelectAsync<GXGatewayGroup>(arg)).ToList();
            return report;
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user,
            IEnumerable<GXReport> reports,
            Expression<Func<GXReport, object?>>? columns)
        {
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(user) };
            DateTime now = DateTime.Now;
            List<Guid> list = new();
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (GXReport report in reports)
                {
                    if (string.IsNullOrEmpty(report.Name) &&
                        (columns == null || ServerHelpers.Contains(columns, nameof(GXReport.Name))))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    if (report.ReportGroups == null || !report.ReportGroups.Any())
                    {
                        ListReportGroups request = new ListReportGroups()
                        {
                            Filter = new GXReportGroup() { Default = true }
                        };
                        report.ReportGroups = new List<GXReportGroup>();
                        report.ReportGroups.AddRange(await _reportGroupRepository.ListAsync(user, request, null, CancellationToken.None));
                        if (!report.ReportGroups.Any())
                        {
                            throw new ArgumentException(Properties.Resources.TargetMustBelongToOneGroup);
                        }
                    }
                    if (report.Id == Guid.Empty)
                    {
                        report.CreationTime = now;
                        report.Creator = creator;
                        GXInsertArgs args = GXInsertArgs.Insert(report);
                        args.Exclude<GXReport>(q => new
                        {
                            q.ReportGroups,
                            q.DeviceGroups,
                            q.Devices,
                            q.DeviceAttributeTemplates,
                            q.DeviceGroupAttributeTemplates,
                            q.Agents,
                            q.Gateways,
                            q.Last,
                            q.Next,
                            q.Updated,
                            q.Removed
                        });
                        _host.Connection.Insert(args);
                        list.Add(report.Id);
                        AddReportToReportGroups(transaction, report.Id, report.ReportGroups);
                        AddDeviceAttributesToReport(transaction, report.Id, report.DeviceAttributeTemplates);
                        AddDevicesToReport(transaction, report.Id, report.Devices);
                        AddDeviceGroupsToReport(transaction, report.Id, report.DeviceGroups);
                        AddDeviceGroupAttributesToReport(transaction, report.Id, report.DeviceGroupAttributeTemplates);
                        AddAgentsToReport(transaction, report.Id, report.Agents);
                        AddGatewaysToReport(transaction, report.Id, report.Gateways);
                    }
                    else
                    {
                        if (columns == null || !ServerHelpers.Contains(columns, nameof(GXReport.Status)))
                        {
                            GXSelectArgs m = GXSelectArgs.Select<GXReport>(q => q.ConcurrencyStamp, where => where.Id == report.Id);
                            string updated = _host.Connection.SingleOrDefault<string>(m);
                            if (!string.IsNullOrEmpty(updated) && updated != report.ConcurrencyStamp)
                            {
                                throw new ArgumentException(Properties.Resources.ContentEdited);
                            }
                            report.ConcurrencyStamp = Guid.NewGuid().ToString();
                            report.Updated = now;
                        }
                        GXUpdateArgs args = GXUpdateArgs.Update(report, columns);
                        args.Exclude<GXReport>(q => new
                        {
                            q.CreationTime,
                            q.ReportGroups,
                            q.DeviceGroups,
                            q.Devices,
                            q.DeviceAttributeTemplates,
                            q.DeviceGroupAttributeTemplates,
                            q.Agents,
                            q.Gateways,
                            q.Last,
                            q.Next
                        });
                        if (!user.IsInRole(GXRoles.Admin) ||
                            report.Creator == null ||
                            string.IsNullOrEmpty(report.Creator.Id))
                        {
                            //Only admin can update the creator.
                            args.Exclude<GXReport>(q => q.Creator);
                        }
                        _host.Connection.Update(args);
                        //Map report groups to report.
                        List<GXReportGroup> reportGroups;
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            {
                                IReportGroupRepository reportGroupRepository = scope.ServiceProvider.GetRequiredService<IReportGroupRepository>();
                                reportGroups = await reportGroupRepository.GetJoinedReportGroups(user, report.Id);
                                var comparer = new UniqueComparer<GXReportGroup, Guid>();
                                List<GXReportGroup> removedReportGroups = reportGroups.Except(report.ReportGroups, comparer).ToList();
                                List<GXReportGroup> addedReportGroups = report.ReportGroups.Except(reportGroups, comparer).ToList();
                                if (removedReportGroups.Any())
                                {
                                    RemoveReportsFromReportGroup(transaction,
                                        report.Id, removedReportGroups);
                                }
                                if (addedReportGroups.Any())
                                {
                                    AddReportToReportGroups(transaction,
                                        report.Id, addedReportGroups);
                                }
                            }
                        }
                        //Add agents.
                        if (columns == null || ServerHelpers.Contains(columns, nameof(GXReport.Agents)))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgent>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXAgent, GXReportAgent>(a => a.Id, b => b.AgentId);
                            arg.Where.And<GXReportAgent>(where => where.Removed == null && where.ReportId == report.Id);
                            List<GXAgent> agents = (await _host.Connection.SelectAsync<GXAgent>(arg)).ToList();
                            var comparer = new UniqueComparer<GXAgent, Guid>();
                            List<GXAgent> removed = agents.Except(report.Agents, comparer).ToList();
                            List<GXAgent> added = report.Agents.Except(agents, comparer).ToList();
                            if (removed.Any())
                            {
                                RemoveAgentsFromReports(transaction,
                                    report.Id, removed);
                            }
                            if (added.Any())
                            {
                                AddAgentsToReport(transaction,
                                    report.Id, added);
                            }
                        }
                        //Add gateways.
                        if (columns == null || ServerHelpers.Contains(columns, nameof(GXReport.Gateways)))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXGateway>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXGateway, GXReportGateway>(a => a.Id, b => b.GatewayId);
                            arg.Where.And<GXReportGateway>(where => where.Removed == null && where.ReportId == report.Id);
                            List<GXGateway> gateways = (await _host.Connection.SelectAsync<GXGateway>(arg)).ToList();
                            var comparer = new UniqueComparer<GXGateway, Guid>();
                            List<GXGateway> removed = gateways.Except(report.Gateways, comparer).ToList();
                            List<GXGateway> added = report.Gateways.Except(gateways, comparer).ToList();
                            if (removed.Any())
                            {
                                RemoveGatewaysFromReports(transaction,
                                    report.Id, removed);
                            }
                            if (added.Any())
                            {
                                AddGatewaysToReport(transaction,
                                    report.Id, added);
                            }
                        }
                        //Add device template attributes.
                        if (columns == null || ServerHelpers.Contains(columns, nameof(GXReport.DeviceAttributeTemplates)))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAttributeTemplate>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXReportDeviceAttributeTemplate>(a => a.Id, b => b.AttributeTemplateId);
                            arg.Where.And<GXReportDeviceAttributeTemplate>(where => where.Removed == null && where.ReportId == report.Id);
                            List<GXAttributeTemplate> attributes = (await _host.Connection.SelectAsync<GXAttributeTemplate>(arg)).ToList();
                            var comparer = new UniqueComparer<GXAttributeTemplate, Guid>();
                            List<GXAttributeTemplate> removedReportAttributes = attributes.Except(report.DeviceAttributeTemplates, comparer).ToList();
                            List<GXAttributeTemplate> addedReportAttributes = report.DeviceAttributeTemplates.Except(attributes, comparer).ToList();
                            if (removedReportAttributes.Any())
                            {
                                RemoveDeviceAttributesFromReports(transaction,
                                    report.Id, removedReportAttributes);
                            }
                            if (addedReportAttributes.Any())
                            {
                                AddDeviceAttributesToReport(transaction,
                                    report.Id, addedReportAttributes);
                            }
                        }
                        if (columns == null || ServerHelpers.Contains(columns, nameof(GXReport.DeviceGroupAttributeTemplates)))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXAttributeTemplate>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXAttributeTemplate, GXReportDeviceGroupAttributeTemplate>(a => a.Id, b => b.AttributeTemplateId);
                            arg.Where.And<GXReportDeviceGroupAttributeTemplate>(where => where.Removed == null && where.ReportId == report.Id);
                            List<GXAttributeTemplate> attributes = (await _host.Connection.SelectAsync<GXAttributeTemplate>(arg)).ToList();
                            var comparer = new UniqueComparer<GXAttributeTemplate, Guid>();
                            List<GXAttributeTemplate> removedReportAttributes = attributes.Except(report.DeviceGroupAttributeTemplates, comparer).ToList();
                            List<GXAttributeTemplate> addedReportAttributes = report.DeviceGroupAttributeTemplates.Except(attributes, comparer).ToList();
                            if (removedReportAttributes.Any())
                            {
                                RemoveDeviceGroupAttributesFromReports(transaction,
                                    report.Id, removedReportAttributes);
                            }
                            if (addedReportAttributes.Any())
                            {
                                AddDeviceGroupAttributesToReport(transaction,
                                    report.Id, addedReportAttributes);
                            }
                        }
                        if (columns == null || ServerHelpers.Contains(columns, nameof(GXReport.Devices)))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDevice>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXDevice, GXReportDevice>(a => a.Id, b => b.DeviceId);
                            arg.Where.And<GXReportDevice>(where => where.Removed == null && where.ReportId == report.Id);
                            List<GXDevice> devices = (await _host.Connection.SelectAsync<GXDevice>(arg)).ToList();
                            var comparer = new UniqueComparer<GXDevice, Guid>();
                            List<GXDevice> removedReportDevices = devices.Except(report.Devices, comparer).ToList();
                            List<GXDevice> addedReportDevices = report.Devices.Except(devices, comparer).ToList();
                            if (removedReportDevices.Any())
                            {
                                RemoveDevicesFromReports(transaction,
                                    report.Id, removedReportDevices);
                            }
                            if (addedReportDevices.Any())
                            {
                                AddDevicesToReport(transaction,
                                    report.Id, addedReportDevices);
                            }
                        }
                        if (columns == null || ServerHelpers.Contains(columns, nameof(GXReport.DeviceGroups)))
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceGroup>(where => where.Removed == null);
                            arg.Joins.AddInnerJoin<GXDeviceGroup, GXReportDeviceGroup>(a => a.Id, b => b.DeviceGroupId);
                            arg.Where.And<GXReportDeviceGroup>(where => where.Removed == null && where.ReportId == report.Id);
                            List<GXDeviceGroup> deviceGroups = (await _host.Connection.SelectAsync<GXDeviceGroup>(arg)).ToList();
                            var comparer = new UniqueComparer<GXDeviceGroup, Guid>();
                            List<GXDeviceGroup> removedReportDeviceGroups = deviceGroups.Except(report.DeviceGroups, comparer).ToList();
                            List<GXDeviceGroup> addedReportDeviceGroups = report.DeviceGroups.Except(deviceGroups, comparer).ToList();
                            if (removedReportDeviceGroups.Any())
                            {
                                RemoveDeviceGroupsFromReports(transaction,
                                    report.Id, removedReportDeviceGroups);
                            }
                            if (addedReportDeviceGroups.Any())
                            {
                                AddDeviceGroupsToReport(transaction,
                                    report.Id, addedReportDeviceGroups);
                            }
                        }
                    }
                }
                _host.Connection.CommitTransaction(transaction);
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            Dictionary<GXReport, List<string>> updates = new();
            foreach (GXReport report in reports)
            {
                updates[report] = await GetUsersAsync(user, report.Id);
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.ReportUpdate(it.Value, new GXReport[] { it.Key });
            }
            //Count new report execution time.
            await _report.UpdateAsync();
            return list.ToArray();
        }

        /// <summary>
        /// Map report to report groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="groups">Group IDs of the report groups where the report is added.</param>
        public void AddReportToReportGroups(
            IDbTransaction transaction,
            Guid reportId, IEnumerable<GXReportGroup>? groups)
        {
            if (groups != null)
            {
                DateTime now = DateTime.Now;
                List<GXReportGroupReport> list = new();
                foreach (GXReportGroup it in groups)
                {
                    list.Add(new GXReportGroupReport()
                    {
                        ReportId = reportId,
                        ReportGroupId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between report group and report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="groups">Group IDs of the report groups where the report is removed.</param>
        public void RemoveReportsFromReportGroup(
            IDbTransaction transaction,
            Guid reportId, IEnumerable<GXReportGroup> groups)
        {
            foreach (GXReportGroup it in groups)
            {
                _host.Connection.Delete(transaction,
                GXDeleteArgs.Delete<GXReportGroupReport>(w => w.ReportId == reportId && w.ReportGroupId == it.Id));
            }
        }

        /// <summary>
        /// Map device with report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="devices">Joined devices.</param>
        public void AddDevicesToReport(
            IDbTransaction transaction,
            Guid reportId, IEnumerable<GXDevice>? devices)
        {
            if (devices != null)
            {
                DateTime now = DateTime.Now;
                List<GXReportDevice> list = new();
                foreach (GXDevice it in devices)
                {
                    list.Add(new GXReportDevice()
                    {
                        ReportId = reportId,
                        DeviceId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device and report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="devices">Removed devices.</param>
        public void RemoveDevicesFromReports(
            IDbTransaction transaction,
            Guid reportId, IEnumerable<GXDevice> devices)
        {
            foreach (GXDevice it in devices)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXReportDevice>(w => w.ReportId == reportId && w.DeviceId == it.Id));
            }
        }

        /// <summary>
        /// Map device group with report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="groups">Joined device groups.</param>
        public void AddDeviceGroupsToReport(IDbTransaction transaction,
            Guid reportId, IEnumerable<GXDeviceGroup>? groups)
        {
            if (groups != null)
            {
                DateTime now = DateTime.Now;
                List<GXReportDeviceGroup> list = new();
                foreach (GXDeviceGroup it in groups)
                {
                    list.Add(new GXReportDeviceGroup()
                    {
                        ReportId = reportId,
                        DeviceGroupId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device group and report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="groups">Removed device groups.</param>
        public void RemoveDeviceGroupsFromReports(IDbTransaction transaction,
            Guid reportId, IEnumerable<GXDeviceGroup> groups)
        {
            foreach (GXDeviceGroup it in groups)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXReportDeviceGroup>(w => w.ReportId == reportId && w.DeviceGroupId == it.Id));
            }
        }

        /// <summary>
        /// Map agents with report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="agents">Joined agents.</param>
        public void AddAgentsToReport(IDbTransaction transaction,
            Guid reportId, IEnumerable<GXAgent>? agents)
        {
            if (agents != null)
            {
                DateTime now = DateTime.Now;
                List<GXReportAgent> list = new();
                foreach (GXAgent it in agents)
                {
                    list.Add(new GXReportAgent()
                    {
                        ReportId = reportId,
                        AgentId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between agents and report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="agents">Removed agents.</param>
        public void RemoveAgentsFromReports(IDbTransaction transaction,
            Guid reportId, IEnumerable<GXAgent> agents)
        {
            foreach (GXAgent it in agents)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXReportAgent>(w =>
                    w.ReportId == reportId &&
                    w.AgentId == it.Id));
            }
        }

        /// <summary>
        /// Map gateways with report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="gateways">Joined gateways.</param>
        public void AddGatewaysToReport(IDbTransaction transaction,
            Guid reportId, IEnumerable<GXGateway>? gateways)
        {
            if (gateways != null)
            {
                DateTime now = DateTime.Now;
                List<GXReportGateway> list = new();
                foreach (GXGateway it in gateways)
                {
                    list.Add(new GXReportGateway()
                    {
                        ReportId = reportId,
                        GatewayId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between gateways and report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="gateways">Removed gateways.</param>
        public void RemoveGatewaysFromReports(IDbTransaction transaction,
            Guid reportId, IEnumerable<GXGateway> gateways)
        {
            foreach (GXGateway it in gateways)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXReportGateway>(w =>
                    w.ReportId == reportId &&
                    w.GatewayId == it.Id));
            }
        }

        /// <summary>
        /// Map device attributes with report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="attributes">Joined attributes.</param>
        public void AddDeviceAttributesToReport(IDbTransaction transaction,
            Guid reportId, IEnumerable<GXAttributeTemplate>? attributes)
        {
            if (attributes != null)
            {
                DateTime now = DateTime.Now;
                List<GXReportDeviceAttributeTemplate> list = new();
                foreach (GXAttributeTemplate it in attributes)
                {
                    list.Add(new GXReportDeviceAttributeTemplate()
                    {
                        ReportId = reportId,
                        AttributeTemplateId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device attribute and report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="attributes">Removed attributes.</param>
        public void RemoveDeviceAttributesFromReports(IDbTransaction transaction,
            Guid reportId, IEnumerable<GXAttributeTemplate> attributes)
        {
            foreach (GXAttributeTemplate it in attributes)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXReportDeviceAttributeTemplate>(w =>
                    w.ReportId == reportId &&
                    w.AttributeTemplateId == it.Id));
            }
        }

        /// <summary>
        /// Map device group attributes with report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="attributes">Joined attributes.</param>
        public void AddDeviceGroupAttributesToReport(IDbTransaction transaction,
            Guid reportId, IEnumerable<GXAttributeTemplate>? attributes)
        {
            if (attributes != null)
            {
                DateTime now = DateTime.Now;
                List<GXReportDeviceGroupAttributeTemplate> list = new();
                foreach (GXAttributeTemplate it in attributes)
                {
                    list.Add(new GXReportDeviceGroupAttributeTemplate()
                    {
                        ReportId = reportId,
                        AttributeTemplateId = it.Id,
                        CreationTime = now
                    });
                }
                _host.Connection.Insert(transaction,
                    GXInsertArgs.InsertRange(list));
            }
        }

        /// <summary>
        /// Remove map between device group attribute and report.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="attributes">Removed attributes.</param>
        public void RemoveDeviceGroupAttributesFromReports(IDbTransaction transaction,
            Guid reportId, IEnumerable<GXAttributeTemplate> attributes)
        {
            foreach (GXAttributeTemplate it in attributes)
            {
                _host.Connection.Delete(transaction,
                    GXDeleteArgs.Delete<GXReportDeviceGroupAttributeTemplate>(w =>
                    w.ReportId == reportId &&
                    w.AttributeTemplateId == it.Id));
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXReport>> SendAsync(
            ClaimsPrincipal user,
            IEnumerable<GXReport> reports)
        {
            List<GXReport> list = new List<GXReport>();
            foreach (var report in reports)
            {
                var tmp = await ReadAsync(user, report.Id);
                if (report.Delivery == ReportDelivery.Caller)
                {
                    tmp.Delivery = ReportDelivery.Caller;
                    tmp.Interval = report.Interval;
                    tmp.From = report.From;
                    tmp.To = report.To;
                    tmp.Range = report.Range;
                    tmp.Count = report.Count;
                }
                //Ignore unnecessary properties.
                tmp.CreationTime = null;
                tmp.Updated = null;
                tmp.ConcurrencyStamp = null;
                tmp.ReportGroups = null;
                tmp.Next = null;
                tmp.Interval = 0;
                tmp.Destination = null;
                tmp.Active = null;
                await _report.SendAsync(user, tmp);
                list.Add(tmp);
            }
            return list;
        }

        /// <inheritdoc />
        public async Task CancelAsync(ClaimsPrincipal user, IEnumerable<Guid>? reports)
        {
            if (reports != null)
            {
                Dictionary<GXReport, List<string>> updates = new();
                ListReports req = new ListReports()
                {
                    Included = reports?.ToArray()
                };
                var list = await ListAsync(user, req, null, CancellationToken.None);
                //Notify users
                foreach (var report in list)
                {
                    if (report.Status == ReportStatus.Process)
                    {
                        _report.Cancel(user, reports);
                        report.Status = ReportStatus.Idle;
                        await UpdateAsync(user,
                            new GXReport[] { report },
                            c => new { c.Status });
                        List<string> users = await GetUsersAsync(user, report.Id);
                        updates[report] = users;
                    }
                }
                foreach (var it in updates)
                {
                    //Don't notify if already cancelled.
                    if (it.Key.Status == ReportStatus.Cancel)
                    {
                        GXReport tmp = new GXReport() { Id = it.Key.Id, Status = it.Key.Status };
                        await _eventsNotifier.ReportClear(it.Value,
                            new GXReport[] { tmp });
                    }
                }
            }
        }
    }
}
