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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Services
{
    /// <inheritdoc/>
    internal sealed class GXSubtotalService : ISubtotal, IHostedService
    {
        private struct GXUserSubtotal
        {
            public ClaimsPrincipal user;
            public Guid subtotal;
        }
        private List<GXUserSubtotal> _subtotals = new List<GXUserSubtotal>();
        private static SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly IServiceProvider _serviceProvider;
        private readonly IGXHost _host;
        private readonly ILogger<GXSubtotalService> _logger;
        private Timer _timer;

        public GXSubtotalService(IServiceProvider serviceProvider,
            IGXHost host,
            ILogger<GXSubtotalService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _host = host;
            Thread t = new Thread(() => Counter());
            t.Start();
        }

        /// <inheritdoc/>
        public void Calculate(ClaimsPrincipal user, IEnumerable<Guid>? subtotals)
        {
            lock (_subtotals)
            {
                if (subtotals == null)
                {
                    _subtotals.Add(new GXUserSubtotal()
                    {
                        user = user
                    });
                }
                else
                {
                    foreach (var subtotal in subtotals)
                    {
                        _subtotals.Add(new GXUserSubtotal()
                        {
                            user = user,
                            subtotal = subtotal
                        });
                    }
                }
            }
            _signal.Release();
        }

        /// <inheritdoc/>
        public void Cancel(ClaimsPrincipal user, IEnumerable<Guid>? subtotals)
        {
            lock (_subtotals)
            {
                if (subtotals == null)
                {
                    //Cancel all subtotals.
                    _subtotals.Clear();
                }
                else
                {
                    foreach (var subtotal in subtotals)
                    {
                        var item = _subtotals.Where(s => s.subtotal == subtotal).SingleOrDefault();
                        if (item.subtotal != Guid.Empty)
                        {
                            _subtotals.Remove(item);
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
            while (value < now)
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
            int interval = 0;
            DateTime now = DateTime.Now;
            if (User != null)
            {
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    ISubtotalRepository repository = scope.ServiceProvider.GetRequiredService<ISubtotalRepository>();
                    ListSubtotals req = new ListSubtotals()
                    {
                        Filter = new GXSubtotal()
                        {
                            Active = true
                        }
                    };
                    var subtotals = await repository.ListAsync(User, req, null, CancellationToken.None);
                    foreach (var subtotal in subtotals)
                    {
                        if (subtotal.Last != null)
                        {
                            DateTimeOffset tmp = subtotal.Last.Value.AddSeconds(subtotal.Interval);
                            if (tmp <= now)
                            {
                                Calculate(User, new Guid[] { subtotal.Id });
                                tmp = GetNextExecutionTime(subtotal.Last.Value, subtotal.Interval, now);
                            }
                            if (tmp < next)
                            {
                                interval = subtotal.Interval;
                                next = tmp;
                            }
                        }
                        else
                        {
                            Calculate(User, new Guid[] { subtotal.Id });
                            if (subtotal.Last != null)
                            {
                                DateTimeOffset tmp = subtotal.Last.Value.AddSeconds(subtotal.Interval);
                                if (tmp < next)
                                {
                                    next = tmp;
                                }
                            }
                        }
                    }
                }
                TimeSpan start = next - DateTime.Now;
                if (start.TotalMilliseconds > 0 && interval != 0)
                {
                    _timer.Change((int)start.TotalMilliseconds,
                        TimeSpan.FromSeconds(interval).Milliseconds);
                }
            }
        }

        private static async Task<DateTimeOffset?> Update(
            ClaimsPrincipal user,
            ISubtotalValueRepository subtotalValueRepository,
            GXSubtotal subtotal,
            List<GXSubtotalValue> list)
        {
            if (list.Any())
            {
                List<GXSubtotalValue> items = new List<GXSubtotalValue>();
                //Add empty group values.
                DateTimeOffset start = list[0].StartTime.Value.AddSeconds(subtotal.Interval);
                DateTimeOffset end = DateTime.Now;
                items.Add(list[0]);
                while (start < end)
                {
                    var item = list.Where(w => w.StartTime == start).SingleOrDefault();
                    if (item != null)
                    {
                        items.Add(item);
                    }
                    else if (subtotal.Fill.GetValueOrDefault(true))
                    {
                        items.Add(new GXSubtotalValue()
                        {
                            Subtotal = subtotal,
                            StartTime = start,
                            EndTime = start.AddSeconds(subtotal.Interval),
                            Value = "0"
                        });
                    }
                    start = start.AddSeconds(subtotal.Interval);
                }
                //Check is the first item already inserted.
                ListSubtotalValues req2 = new ListSubtotalValues()
                {
                    Filter = new GXSubtotalValue()
                    {
                        //Filter only with subtotal ID.
                        Subtotal = new GXSubtotal() { Id = subtotal.Id },
                        StartTime = list[list.Count - 1].StartTime,
                        EndTime = list[list.Count - 1].EndTime
                    },
                    Descending = false,
                    OrderBy = nameof(GXSubtotalValue.StartTime)
                };
                GXSubtotalValue[] ret = await subtotalValueRepository.ListAsync(user, req2, null, CancellationToken.None);
                if (ret.Any())
                {
                    int count = Math.Min(ret.Count(), items.Count);
                    for (int pos = 0; pos != count; ++pos)
                    {
                        //Update value if value has changed.
                        if (items[pos].StartTime == ret[pos].StartTime &&
                            items[pos].EndTime == ret[pos].EndTime &&
                            items[pos].Value != ret[pos].Value)
                        {
                            items[pos].Id = ret[pos].Id;
                        }
                        else
                        {
                            //Remove if it's not changed.
                            items.RemoveAt(pos);
                            --count;
                            --pos;
                        }
                    }
                }
                if (items.Any())
                {
                    //Add subtotal items.
                    await subtotalValueRepository.AddAsync(user, items);
                    //Update calculated time.
                    return items[items.Count - 1].StartTime;
                }
            }
            return null;
        }

        /// <summary>
        /// Group attribute values.
        /// </summary>
        private static async Task<DateTimeOffset?> GroupValuesAsync(
            ClaimsPrincipal user,
            ISubtotalValueRepository subtotalValueRepository,
            GXSubtotal subtotal,
            IEnumerable<GXValue> values)
        {
            if (subtotal.Interval == 0)
            {
                //Interval is one day.
                subtotal.Interval = 86400;
            }
            int interval = subtotal.Interval;
            int hours = interval / 3600;
            if (hours != 0)
            {
                interval -= hours * 3600;
            }
            int minutes = interval / 60;
            if (minutes != 0)
            {
                interval -= minutes * 60;
            }
            else
            {
                minutes = 60;
            }
            int seconds = interval;
            if (seconds == 0)
            {
                seconds = 60;
            }
            else if (minutes == 60)
            {
                minutes = 0;
            }
            var groups = values.GroupBy(x =>
            {
                var stamp = x.Read.Value;
                if (hours != 0)
                {
                    stamp = stamp.AddHours(-(stamp.Hour % hours));
                }
                if (minutes != 0)
                {
                    stamp = stamp.AddMinutes(-(stamp.Minute % minutes));
                }
                stamp = stamp.AddSeconds(-(stamp.Second % seconds));
                stamp = stamp.AddMilliseconds(-stamp.Millisecond);
                return stamp;
            });

            List<GXSubtotalValue> list;
            switch (subtotal.Operation)
            {
                case (int)SubtotalOperation.Sum:
                    list = groups.Select(g => new GXSubtotalValue()
                    {
                        Subtotal = subtotal,
                        StartTime = g.Key,
                        EndTime = g.Key.AddSeconds(subtotal.Interval),
                        Value = g.Sum(s => Convert.ToDouble(s.Value)).ToString()
                    }).OrderBy(o => o.StartTime).ToList();
                    break;
                case (int)SubtotalOperation.Average:
                    list = groups.Select(g => new GXSubtotalValue()
                    {
                        Subtotal = subtotal,
                        StartTime = g.Key,
                        EndTime = g.Key.AddSeconds(subtotal.Interval),
                        Value = g.Average(s => Convert.ToDouble(s.Value)).ToString()
                    }).OrderBy(o => o.StartTime).ToList();
                    break;
                case (int)SubtotalOperation.Minimum:
                    list = groups.Select(g => new GXSubtotalValue()
                    {
                        Subtotal = subtotal,
                        StartTime = g.Key,
                        EndTime = g.Key.AddSeconds(subtotal.Interval),
                        Value = g.Min(s => Convert.ToDouble(s.Value)).ToString()
                    }).OrderBy(o => o.StartTime).ToList();
                    break;
                case (int)SubtotalOperation.Maximum:
                    list = groups.Select(g => new GXSubtotalValue()
                    {
                        Subtotal = subtotal,
                        StartTime = g.Key,
                        EndTime = g.Key.AddSeconds(subtotal.Interval),
                        Value = g.Max(s => Convert.ToDouble(s.Value)).ToString()
                    }).OrderBy(o => o.StartTime).ToList();
                    break;
                case (int)SubtotalOperation.Count:
                    list = groups.Select(g => new GXSubtotalValue()
                    {
                        Subtotal = subtotal,
                        StartTime = g.Key,
                        EndTime = g.Key.AddSeconds(subtotal.Interval),
                        Value = g.Count().ToString()
                    }).OrderBy(o => o.StartTime).ToList();
                    break;
                default:
                    throw new ArgumentException("Invalid subtotal type.");
            }
            if (subtotal.Delta && list.Any())
            {
                if (list.Count == 1)
                {
                    //Previous hour is empty.
                    list[0].Value = "-" + Convert.ToDouble(list[0].Value);
                }
                else
                {
                    //Count delta values.
                    var values2 = list.Zip(list.Skip(1), (current, next) => (Convert.ToDouble(next.Value) - Convert.ToDouble(current.Value)).ToString()).ToArray();
                    //Update delta values.
                    int pos = -1;
                    foreach (var it in list)
                    {
                        if (pos != -1)
                        {
                            //The first value is skipped.
                            it.Value = values2[pos];
                        }
                        ++pos;
                    }
                    if (subtotal.Last != null)
                    {
                        //Remove the first item.
                        list.RemoveAt(0);
                    }
                }
            }
            return await Update(user, subtotalValueRepository,
                subtotal, list);
        }

        /// <summary>
        /// Group agent logs.
        /// </summary>
        private static async Task<DateTimeOffset?> GroupAgentsAsync(
            ClaimsPrincipal user,
            ISubtotalValueRepository subtotalValueRepository,
            GXSubtotal subtotal,
            IEnumerable<GXAgentLog> values)
        {
            if (subtotal.Interval == 0)
            {
                //Interval is one day.
                subtotal.Interval = 86400;
            }
            int interval = subtotal.Interval;
            int hours = interval / 3600;
            if (hours != 0)
            {
                interval -= hours * 3600;
            }
            int minutes = interval / 60;
            if (minutes != 0)
            {
                interval -= minutes * 60;
            }
            else
            {
                minutes = 60;
            }
            int seconds = interval;
            if (seconds == 0)
            {
                seconds = 60;
            }
            else if (minutes == 60)
            {
                minutes = 0;
            }
            var groups = values.GroupBy(x =>
            {
                var stamp = x.CreationTime.Value;
                if (hours != 0)
                {
                    stamp = stamp.AddHours(-(stamp.Hour % hours));
                }
                if (minutes != 0)
                {
                    stamp = stamp.AddMinutes(-(stamp.Minute % minutes));
                }
                stamp = stamp.AddSeconds(-(stamp.Second % seconds));
                stamp = stamp.AddMilliseconds(-stamp.Millisecond);
                return stamp;
            });
            List<GXSubtotalValue> list;
            switch (subtotal.Operation)
            {
                case (int)SubtotalOperation.Count:
                    list = groups.Select(g => new GXSubtotalValue()
                    {
                        Subtotal = subtotal,
                        StartTime = g.Key,
                        EndTime = g.Key.AddSeconds(subtotal.Interval),
                        Value = g.Count().ToString()
                    }).OrderBy(o => o.StartTime).ToList();
                    break;
                default:
                    throw new ArgumentException("Invalid subtotal type.");
            }
            return await Update(user, subtotalValueRepository,
                subtotal, list);
        }

        /// <summary>
        /// Search devices values grouping by attribute.
        /// </summary>
        private async Task<DateTimeOffset?> CalculateDevicesAsync(ClaimsPrincipal user,
            ISubtotalValueRepository subtotalValueRepository,
            GXSubtotal subtotal,
            DateTimeOffset? updated)
        {
            if (subtotal.DeviceAttributeTemplates != null &&
                subtotal.DeviceAttributeTemplates.Any() &&
                subtotal.Devices != null &&
                subtotal.Devices.Any())
            {
                Guid[] deviceIds = subtotal.Devices.Select(s => s.Id).ToArray();
                foreach (var id in deviceIds)
                {
                    foreach (var it in subtotal.DeviceAttributeTemplates)
                    {
                        string ln = it.ObjectTemplate.LogicalName;
                        GXSelectArgs arg = GXSelectArgs.SelectAll<GXValue>();
                        arg.OrderBy.Add<GXValue>(o => o.Read);
                        if (subtotal.Last != null)
                        {
                            arg.Where.And<GXValue>(w => w.Read >= subtotal.Last);
                        }
                        arg.Joins.AddInnerJoin<GXValue, GXAttribute>(j => j.Attribute, j => j.Id);
                        arg.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object, j => j.Id);
                        arg.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device, j => j.Id);
                        arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template, j => j.Id);
                        arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate);
                        if (subtotal.Total.GetValueOrDefault())
                        {
                            //Count for all devices.
                            arg.Where.And<GXDevice>(w => deviceIds.Contains(w.Id));
                        }
                        else
                        {
                            //Count for each device separetly.
                            arg.Where.And<GXDevice>(w => w.Id == id);
                        }
                        arg.Where.And<GXAttributeTemplate>(w => w.Index == it.Index);
                        arg.Where.And<GXObjectTemplate>(w => w.LogicalName == ln);
                        var values = await _host.Connection.SelectAsync<GXValue>(arg);
                        DateTimeOffset? tmp = await GroupValuesAsync(user,
                            subtotalValueRepository, subtotal, values);
                        if (updated == null || tmp > updated)
                        {
                            updated = tmp;
                        }
                    }
                    if (subtotal.Total.GetValueOrDefault())
                    {
                        //Count for all devices.
                        break;
                    }
                }
            }
            return updated;
        }

        /// <summary>
        /// Search attributes for the device groups.
        /// </summary>
        /// <returns></returns>
        private async Task<DateTimeOffset?> CalculateDeviceGroupsAsync(ClaimsPrincipal user,
            ISubtotalValueRepository subtotalValueRepository,
            GXSubtotal subtotal,
            DateTimeOffset? updated)
        {
            if (subtotal.DeviceGroupAttributeTemplates != null &&
                subtotal.DeviceGroupAttributeTemplates.Any() &&
                subtotal.DeviceGroups != null &&
                subtotal.DeviceGroups.Any())
            {
                Guid[] groupIds = subtotal.DeviceGroups.Select(s => s.Id).ToArray();
                foreach (var id in groupIds)
                {
                    foreach (var it in subtotal.DeviceGroupAttributeTemplates)
                    {
                        string ln = it.ObjectTemplate.LogicalName;
                        GXSelectArgs arg = GXSelectArgs.SelectAll<GXValue>();
                        arg.OrderBy.Add<GXValue>(o => o.Read);
                        if (subtotal.Last != null)
                        {
                            arg.Where.And<GXValue>(w => w.Read >= subtotal.Last);
                        }
                        arg.Joins.AddInnerJoin<GXValue, GXAttribute>(j => j.Attribute, j => j.Id);
                        arg.Joins.AddInnerJoin<GXAttribute, GXObject>(j => j.Object, j => j.Id);
                        arg.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device, j => j.Id);
                        arg.Joins.AddInnerJoin<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId);
                        arg.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                        arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template, j => j.Id);
                        arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate);
                        if (subtotal.Total.GetValueOrDefault())
                        {
                            //Count for all device groups.
                            arg.Where.And<GXDeviceGroup>(w => groupIds.Contains(w.Id));
                        }
                        else
                        {
                            //Count for each device group separetly.
                            arg.Where.And<GXDeviceGroup>(w => groupIds.Contains(w.Id));
                        }
                        arg.Where.And<GXAttributeTemplate>(w => w.Index == it.Index);
                        arg.Where.And<GXObjectTemplate>(w => w.LogicalName == ln);
                        var values = await _host.Connection.SelectAsync<GXValue>(arg);
                        DateTimeOffset? tmp = await GroupValuesAsync(user,
                            subtotalValueRepository, subtotal, values);
                        if (updated == null || tmp > updated)
                        {
                            updated = tmp;
                        }
                    }
                    if (subtotal.Total.GetValueOrDefault())
                    {
                        //Count for all device groups.
                        break;
                    }
                }
            }
            return updated;
        }

        /// <summary>
        /// Search agent values grouping by interval.
        /// </summary>
        private async Task<DateTimeOffset?> CalculateAgentsAsync(ClaimsPrincipal user,
            ISubtotalValueRepository subtotalValueRepository,
            GXSubtotal subtotal,
            DateTimeOffset? updated)
        {
            if (subtotal.Agents != null &&
                subtotal.Agents.Any())
            {
                Guid[] agentIds = subtotal.Agents.Select(s => s.Id).ToArray();
                foreach (var id in agentIds)
                {
                    GXSelectArgs arg = GXSelectArgs.SelectAll<GXAgentLog>();
                    arg.OrderBy.Add<GXAgentLog>(o => o.CreationTime);
                    if (subtotal.Last != null)
                    {
                        arg.Where.And<GXAgentLog>(w => w.CreationTime >= subtotal.Last);
                    }
                    arg.Joins.AddInnerJoin<GXAgentLog, GXSubtotalAgent>(j => j.Agent, j => j.AgentId);
                    arg.Where.And<GXSubtotalAgent>(w => w.SubtotalId == subtotal.Id);
                    if (subtotal.Total.GetValueOrDefault())
                    {
                        //Count for all agents.
                        arg.Where.And<GXSubtotalAgent>(w => agentIds.Contains(w.AgentId));
                    }
                    else
                    {
                        //Count for each agent separetly.
                        arg.Where.And<GXSubtotalAgent>(w => w.AgentId == id);
                    }
                    //Get errors.
                    if (subtotal.Type == (int)SubtotalType.Error)
                    {
                        arg.Where.And<GXAgentLog>(w => w.Level == (int)TraceLevel.Error);
                    }
                    //Get warnings.
                    else if (subtotal.Type == (int)SubtotalType.Warning)
                    {
                        arg.Where.And<GXAgentLog>(w => w.Level == (int)TraceLevel.Warning);
                    }
                    //Get connections.
                    else if (subtotal.Type == (int)SubtotalType.Connect)
                    {
                        arg.Where.And<GXAgentLog>(w => w.Type == (int)LogType.Connected);
                    }
                    //Get disconnections.
                    else if (subtotal.Type == (int)SubtotalType.Disconnect)
                    {
                        arg.Where.And<GXAgentLog>(w => w.Type == (int)LogType.Disconnect);
                    }
                    //Get info.
                    else
                    {
                        arg.Where.And<GXAgentLog>(w => w.Type == (int)TraceLevel.Info);
                    }
                    var values = await _host.Connection.SelectAsync<GXAgentLog>(arg);
                    DateTimeOffset? tmp = await GroupAgentsAsync(user,
                        subtotalValueRepository, subtotal, values);
                    if (updated == null || tmp > updated)
                    {
                        updated = tmp;
                    }
                    if (subtotal.Total.GetValueOrDefault())
                    {
                        //Count for all agents.
                        break;
                    }
                }
            }
            return updated;
        }

        /// <summary>
        /// Wait for subtotal and count them.
        /// </summary>
        private async void Counter()
        {
            while (true)
            {
                _signal.Wait();
                GXUserSubtotal? s = null;
                try
                {
                    lock (_subtotals)
                    {
                        if (_subtotals.Any())
                        {
                            s = _subtotals.FirstOrDefault();
                            if (s != null)
                            {
                                _subtotals.RemoveAt(0);
                            }
                            if (_subtotals.Any())
                            {
                                _signal.Release();
                            }
                        }
                    }
                    if (s != null)
                    {
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            ISubtotalLogRepository repository = scope.ServiceProvider.GetRequiredService<ISubtotalLogRepository>();
                            ISubtotalValueRepository subtotalValueRepository = scope.ServiceProvider.GetRequiredService<ISubtotalValueRepository>();
                            ISubtotalRepository subtotalRepository = scope.ServiceProvider.GetRequiredService<ISubtotalRepository>();
                            var it = await subtotalRepository.ReadAsync(s.Value.user, s.Value.subtotal);
                            if (it.TraceLevel > TraceLevel.Warning)
                            {
                                GXSubtotalLog log = new GXSubtotalLog(TraceLevel.Info)
                                {
                                    Subtotal = new GXSubtotal()
                                    {
                                        Id = s.Value.subtotal
                                    },
                                    Message = "Subtotal started.",
                                };
                                await repository.AddAsync(s.Value.user, [log]);
                            }
                            DateTimeOffset? updated = null;
                            updated = await CalculateDevicesAsync(s.Value.user,
                                    subtotalValueRepository, it, updated);

                            updated = await CalculateDeviceGroupsAsync(s.Value.user,
                                    subtotalValueRepository,
                                    it, updated);

                            updated = await CalculateAgentsAsync(s.Value.user,
                                    subtotalValueRepository,
                                    it, updated);
                            if (updated != null)
                            {
                                //Update status and calculated time.
                                it.Last = updated;
                                it.Status = SubtotalStatus.Idle;
                                await subtotalRepository.UpdateAsync(s.Value.user,
                                    [it],
                                    c => new { c.Last, c.Status });
                            }
                            else
                            {
                                //Update status.
                                it.Status = SubtotalStatus.Idle;
                                await subtotalRepository.UpdateAsync(s.Value.user, [it], c => c.Status);
                            }
                            if (it.TraceLevel > TraceLevel.Warning)
                            {
                                GXSubtotalLog log = new GXSubtotalLog(TraceLevel.Info)
                                {
                                    Subtotal = new GXSubtotal()
                                    {
                                        Id = s.Value.subtotal
                                    },
                                    Message = "Subtotal ended.",
                                };
                                await repository.AddAsync(s.Value.user,
                                    new GXSubtotalLog[] { log });
                            }
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
                            GXSubtotal it = new GXSubtotal()
                            {
                                Id = s.Value.subtotal
                            };
                            it.Status = SubtotalStatus.Idle;
                            GXSubtotalLog log = new GXSubtotalLog(TraceLevel.Error)
                            {
                                Subtotal = new GXSubtotal()
                                {
                                    Id = s.Value.subtotal
                                },
                                Message = ex.Message,
                            };
                            using (IServiceScope scope = _serviceProvider.CreateScope())
                            {
                                ISubtotalRepository subtotalRepository = scope.ServiceProvider.GetRequiredService<ISubtotalRepository>();
                                await subtotalRepository.UpdateAsync(s.Value.user,
                                    new GXSubtotal[] { it },
                                    c => new { c.Status });
                                ISubtotalLogRepository repository = scope.ServiceProvider.GetRequiredService<ISubtotalLogRepository>();
                                await repository.AddAsync(s.Value.user,
                                    new GXSubtotalLog[] { log });
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(async (w) => await UpdateAsync(), null, 0, 1000);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
