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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Services
{

    struct GXReportResult
    {
        public DateTimeOffset? offset;
        public string? value;
    }

    /// <inheritdoc/>
    internal sealed class GXReportService : IReport, IHostedService
    {
        private struct GXUserReport
        {
            public ClaimsPrincipal user;
            public GXReport report;
        }
        private List<GXUserReport> _reports = new List<GXUserReport>();
        private static SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly IServiceProvider _serviceProvider;
        private readonly IGXHost _host;
        private readonly ILogger<GXReportService> _logger;
        private readonly Timer _timer;

        public GXReportService(IServiceProvider serviceProvider,
            IGXHost host,
            ILogger<GXReportService> logger)
        {
            _timer = new Timer(async (w) => await UpdateAsync());
            _serviceProvider = serviceProvider;
            _logger = logger;
            _host = host;
            Thread t = new Thread(async () => await SendReport());
            t.Start();
        }

        private async Task HandleDevices(ClaimsPrincipal user, GXReport report, GXDevice[] devices)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                //Get attributes for each device.
                var objectRepository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                var attributeRepository = scope.ServiceProvider.GetRequiredService<IAttributeRepository>();
                foreach (GXDevice dev in devices)
                {
                    ListAttributes req = new ListAttributes();
                    req.Filter = new GXAttribute()
                    {
                        Object = new GXObject()
                        {
                            Device = dev
                        }
                    };
                    var attributes = (await attributeRepository.ListAsync(user, req)).ToList();
                    //Remove attributes that are not in the report.
                    if (report.DeviceAttributeTemplates != null && report.DeviceAttributeTemplates.Any())
                    {
                        List<GXAttribute> tmp = [.. attributes];
                        attributes.Clear();
                        foreach (var it in report.DeviceAttributeTemplates)
                        {
                            attributes.AddRange(tmp.Where(w => w.Template != null && w.Template.Id == it.Id));
                        }
                    }
                    //Remove attributes that are not in the report.
                    if (report.DeviceGroupAttributeTemplates != null &&
                        report.DeviceGroupAttributeTemplates.Any())
                    {
                        List<GXAttribute> tmp = [.. attributes];
                        attributes.Clear();
                        foreach (var it in report.DeviceGroupAttributeTemplates)
                        {
                            attributes.AddRange(tmp.Where(w => w.Template != null && w.Template.Id == it.Id));
                        }
                    }
                    foreach (GXAttribute att in attributes)
                    {
                        ListValues req2 = new ListValues()
                        {
                            Filter = new GXValue()
                            {
                                Read = report.Range == null ? null : DateTime.Now - report.Range,
                                //Use only attribute Id.
                                Attribute = new GXAttribute() { Id = att.Id }
                            },
                            Count = report.Count.GetValueOrDefault(),
                            Devices = devices.Select(s => s.Id).ToArray(),
                        };
                        if (report.From != null)
                        {
                            //Get values using From value.
                            req2.Filter.Read = report.From;
                        }
                        var valueRepository = scope.ServiceProvider.GetRequiredService<IValueRepository>();
                        att.Values = [.. await valueRepository.ListAsync(user, req2)];
                        foreach (var it in att.Values)
                        {
                            it.Id = Guid.Empty;
                            it.User = null;
                        }
                        GXDevice? dev2 = null;
                        if (report.Devices != null && report.Devices.Any())
                        {
                            dev2 = report.Devices.Where(w => w.Id == dev.Id).SingleOrDefault();
                        }
                        else if (report.DeviceGroups != null && report.DeviceGroups.Any())
                        {
                            dev2 = report.DeviceGroups.Where(w => w.Devices != null).SelectMany(s => s.Devices).SingleOrDefault(w => w.Id == dev.Id);
                        }
                        //Add object to the device if not exists yet.
                        if (att.Object != null &&
                            dev2 != null &&
                            dev2.Objects?.Where(w => w.Id == att?.Object?.Id).SingleOrDefault() == null)
                        {
                            dev2.Id = Guid.Empty;
                            dev2.Template = null;
                            att.Object.Device = null;
                            att.Object.Id = Guid.Empty;
                            att.Object.Template.Id = Guid.Empty;
                            att.CreationTime = DateTime.MinValue;
                            att.Updated = null;
                            att.Id = Guid.Empty;
                            att.Template.Id = Guid.Empty;
                            att.Template.CreationTime = DateTime.MinValue;
                            att.Template.Updated = null;
                            att.Template.AccessLevel = 0;
                            att.Template.Weight = 0;
                            if (dev2.Objects == null)
                            {
                                dev2.Objects = new List<GXObject>();
                            }
                            dev2.Objects.Add(att.Object);
                            if (att.Object.Attributes == null)
                            {
                                att.Object.Attributes = new List<GXAttribute>();
                            }
                            att.Object.Attributes.Add(att);
                            att.Object = null;
                        }
                    }
                }
            }
        }

        private async Task HandleDeviceGroups(ClaimsPrincipal user, GXReport report)
        {
            if (report.DeviceGroups != null && report.DeviceGroups.Any())
            {
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    //Get devices from the device groups.
                    ListDeviceGroups req = new ListDeviceGroups();
                    req.Included = report.DeviceGroups.Select(s => s.Id).ToArray();
                    req.Select = [TargetType.Device];
                    var deviceGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                    report.DeviceGroups = (await deviceGroupRepository.ListAsync(user, req)).ToList();
#pragma warning disable CS8603 // Possible null reference return.
                    var devices = report.DeviceGroups.Where(w => w.Devices != null).SelectMany(s => s.Devices).ToArray();
#pragma warning restore CS8603 // Possible null reference return.
                    await HandleDevices(user, report, devices);
                }
            }
        }

        private async Task MakeReport(ClaimsPrincipal user, GXReport report)
        {
            if (report.Devices != null && report.Devices.Any())
            {
                //Get devices.
                GXDevice[] devices;
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    ListDevices req = new ListDevices();
                    req.Included = report.Devices.Select(s => s.Id).ToArray();
                    var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    devices = await deviceRepository.ListAsync(user, req);
                }
                await HandleDevices(user, report, devices);
            }
            await HandleDeviceGroups(user, report);
        }

        /// <inheritdoc/>
        public async Task SendAsync(ClaimsPrincipal user, GXReport report)
        {
            if (report.Delivery == ReportDelivery.Caller)
            {
                await MakeReport(user, report);
                return;
            }
            lock (_reports)
            {
                _reports.Add(new GXUserReport()
                {
                    user = user,
                    report = report
                });
            }
            _signal.Release();
        }

        /// <summary>
        /// Wait for report and send them.
        /// </summary>
        private async Task SendReport()
        {
            while (true)
            {
                _signal.Wait();
                GXUserReport? s = null;
                try
                {
                    lock (_reports)
                    {
                        if (_reports.Any())
                        {
                            s = _reports.FirstOrDefault();
                            if (s != null)
                            {
                                _reports.RemoveAt(0);
                            }
                            if (_reports.Any())
                            {
                                _signal.Release();
                            }
                        }
                    }
                    if (s != null)
                    {
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            IReportLogRepository repository = scope.ServiceProvider.GetRequiredService<IReportLogRepository>();
                            IReportRepository reportRepository = scope.ServiceProvider.GetRequiredService<IReportRepository>();
                            var it = await reportRepository.ReadAsync(s.Value.user, s.Value.report.Id);
                            //Update report status.
                            it.Status = ReportStatus.Process;
                            await reportRepository.UpdateAsync(s.Value.user, [it], c => c.Status);
                            if (it.TraceLevel > TraceLevel.Warning)
                            {
                                GXReportLog log = new GXReportLog(TraceLevel.Info)
                                {
                                    Report = new GXReport()
                                    {
                                        Id = s.Value.report.Id,
                                    },
                                    Message = "Report started.",
                                };
                                await repository.AddAsync(s.Value.user, [log]);
                            }
                            await MakeReport(s.Value.user, it);
                            if (it.TraceLevel > TraceLevel.Warning)
                            {
                                GXReportLog log = new GXReportLog(TraceLevel.Info)
                                {
                                    Report = new GXReport()
                                    {
                                        Id = s.Value.report.Id,
                                    },
                                    Message = "Report ended.",
                                };
                                await repository.AddAsync(s.Value.user, [log]);
                            }
                            //Update report status.
                            it.Status = ReportStatus.Idle;
                            await reportRepository.UpdateAsync(s.Value.user, [it], c => c.Status);
                        }
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (s != null)
                        {
                            //Update status.
                            GXReport it = new GXReport()
                            {
                                Id = s.Value.report.Id
                            };
                            it.Status = ReportStatus.Idle;
                            GXReportLog log = new GXReportLog(TraceLevel.Error)
                            {
                                Report = new GXReport()
                                {
                                    Id = s.Value.report.Id
                                },
                                Message = ex.Message,
                            };
                            using (IServiceScope scope = _serviceProvider.CreateScope())
                            {
                                IReportRepository subtotalRepository = scope.ServiceProvider.GetRequiredService<IReportRepository>();
                                await subtotalRepository.UpdateAsync(s.Value.user, [it], c => new { c.Status });
                                IReportLogRepository repository = scope.ServiceProvider.GetRequiredService<IReportLogRepository>();
                                await repository.AddAsync(s.Value.user, [log]);
                            }
                        }
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogError(ex2.Message);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Cancel(ClaimsPrincipal user, IEnumerable<Guid>? reports)
        {
            lock (_reports)
            {
                if (reports == null)
                {
                    //Cancel all reports.
                    _reports.Clear();
                }
                else
                {
                    foreach (var report in reports)
                    {
                        var item = _reports.Where(s => s.report.Id == report).SingleOrDefault();
                        if (item.report != null)
                        {
                            _reports.Remove(item);
                        }
                    }
                }
            }
            _signal.Release();
        }

        private static DateTimeOffset GetNextExecutionTime(
            DateTimeOffset value,
            int interval, DateTime now)
        {
            while (value.DateTime < now)
            {
                value = value.AddSeconds(interval);
            }
            return value;
        }

        /// <inheritdoc/>
        public async Task UpdateAsync()
        {
            var User = ServerSettings.GetDefaultAdminUser(_host);
            DateTimeOffset next = DateTimeOffset.MaxValue;
            DateTime now = DateTime.Now;
            if (User != null)
            {
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IReportRepository repository = scope.ServiceProvider.GetRequiredService<IReportRepository>();
                    ListReports req = new ListReports()
                    {
                        Filter = new GXReport()
                        {
                            Active = true
                        },
                        Select = [TargetType.User]
                    };
                    var reports = await repository.ListAsync(User, req, null, CancellationToken.None);
                    foreach (var report in reports)
                    {
                        //The report is not send if interval is zero.
                        if (report.Delivery == ReportDelivery.Caller ||
                            report.Interval <= 1)
                        {
                            continue;
                        }
                        DateTimeOffset tmp;
                        if (report.Last != null)
                        {
                            tmp = report.Last.Value.AddSeconds(report.Interval).DateTime;
                        }
                        else
                        {
                            tmp = now.Date;
                        }
                        if (tmp <= now)
                        {
                            tmp = GetNextExecutionTime(tmp, report.Interval, now);
                        }
                        if (tmp < next)
                        {
                            next = tmp;
                        }
                        if (report.Next <= now)
                        {
                            lock (_reports)
                            {
                                _reports.Add(new GXUserReport()
                                {
                                    report = report,
                                    user = ServerHelpers.CreateClaimsPrincipalFromUser(report.Creator)
                                });
                                _signal.Release();
                            }
                        }
                        //Update next develivery time.
                        if (report.Next == null ||
                            report.Next != tmp)
                        {
                            report.Last = report.Next;
                            report.Next = tmp;
                            GXUpdateArgs arg = GXUpdateArgs.Update(report, c => new { c.Next, c.Last });
                            await _host.Connection.UpdateAsync(arg);
                        }
                    }
                    TimeSpan start = next - DateTime.Now;
                    if (next != DateTimeOffset.MaxValue)
                    {
                        int duetime = 0;
                        if (start.TotalMilliseconds > 0)
                        {
                            duetime = (int)start.TotalMilliseconds;
                        }
                        _timer.Change(duetime, 0);
                    }
                }
            }
        }

        /// <summary>
        /// Find next report.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return UpdateAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
