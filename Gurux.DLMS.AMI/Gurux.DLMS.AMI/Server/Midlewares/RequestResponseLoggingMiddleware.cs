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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Microsoft.IO;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    /// <summary>
    /// This class is used to save all user actions for the User log table.
    /// </summary>
    internal class RequestResponseLoggingMiddleware
    {
        private readonly IGXHost _host;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IPerformanceRepository _performanceRepository;
        private Timer? _timer;
        StatisticSettings? statistic;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RequestResponseLoggingMiddleware(RequestDelegate next,
            IGXHost host,
            IConfigurationRepository configurationRepository,
            ILoggerFactory loggerFactory,
            IPerformanceRepository performanceRepository)
        {
            _next = next;
            _host = host;
            _logger = loggerFactory.CreateLogger<RequestResponseLoggingMiddleware>();
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            _configurationRepository = configurationRepository;

            ListConfiguration req = new ListConfiguration() { Filter = new GXConfiguration() { Name = GXConfigurations.Statistic } };
            GXConfiguration[] confs = _configurationRepository.ListAsync(null, req, null, CancellationToken.None).Result;
            if (confs.Length == 1)
            {
                statistic = JsonSerializer.Deserialize<StatisticSettings>(confs[0].Settings);
            }
            _configurationRepository.Updated += async (configurations) =>
            {
                //If maintenance configuration is updated.
                foreach (GXConfiguration it in configurations)
                {
                    if (it.Name == GXConfigurations.Statistic && it.Settings != null)
                    {
                        statistic = JsonSerializer.Deserialize<StatisticSettings>(it.Settings);
                        break;
                    }
                }
            };
            _performanceRepository = performanceRepository;
            if (statistic.PerformanceInterval != 0)
            {
                TimeSpan start = DateTime.Now.AddSeconds(statistic.PerformanceInterval - (DateTime.Now.Second % statistic.PerformanceInterval)) - DateTime.Now;
                _timer = new Timer(DoWork, null, start, TimeSpan.FromSeconds(statistic.PerformanceInterval));
            }
        }

        private async void DoWork(object? state)
        {
            Dictionary<string, GXPerformance> list = new Dictionary<string, GXPerformance>();
            lock (_performanceRepository.Snapshots)
            {
                foreach (var it in _performanceRepository.Snapshots)
                {
                    list.Add(it.Key, it.Value);
                }
            }
            //Save performance values.
            foreach (var it in list)
            {
                await GetPerformance(it.Key, true);
            }
        }

        private async Task<GXPerformance?> GetPerformance(string type,
            bool force = false)
        {
            if (statistic == null || statistic.PerformanceInterval == 0)
            {
                return null;
            }
            DateTime now = DateTime.Now;
            now = now.AddMilliseconds(-now.Millisecond);
            GXPerformance? value = null;
            GXPerformance? savedValue = null;
            lock (_performanceRepository.Snapshots)
            {
                if (_performanceRepository.Snapshots.TryGetValue(type, out GXPerformance? v))
                {
                    if (v != null)
                    {
                        value = v;
                        //Values are saved for the database once a minute if there are any actions.
                        if (force ||
                            (now - value.Start.GetValueOrDefault()).TotalSeconds >= statistic.PerformanceInterval)
                        {
                            value.End = now;
                            savedValue = value;
                            //Remove snapshot value.
                            _performanceRepository.Snapshots.Remove(type);
                        }
                    }
                }
                if (value == null)
                {
                    //Add new snapshot value.
                    value = new GXPerformance()
                    {
                        Target = type,
                        Start = now,
                    };
                    _performanceRepository.Snapshots[type] = value;
                }
            }
            if (savedValue != null)
            {
                if (savedValue.TotalCount != 0)
                {
                    await _performanceRepository.AddAsync(null, new[] { savedValue });
                }
            }
            return value;
        }

        public async Task Invoke(HttpContext context)
        {
            DateTime start = DateTime.Now;
            CrudAction action = CrudAction.None;
            string? type = null;
            string? id = ServerHelpers.GetUserId(context.User, false);
            try
            {
                id = ServerHelpers.GetUserId(context.User, false);
            }
            catch (Exception)
            {
                //It's OK if this fails.
            }
            bool isApi = context.Request.Path.StartsWithSegments("/api");
            //Save user actions.
            if (id != null && statistic != null && statistic.UserActions && isApi)
            {
                string[]? items = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (items != null && items.Length == 3)
                {
                    if (ClientHelpers.GetNotifications(true).Contains(items[1].ToLower()))
                    {
                        type = items[1];
                    }
                    else
                    {
                        _logger.LogInformation("Unknown Target type '{0}'", context.Request.Path.Value);
                    }
                    if (string.Compare(items[2], "add", true) == 0)
                    {
                        action = CrudAction.Create;
                    }
                    else if (!Enum.TryParse(items[2], true, out action))
                    {
                        _logger.LogInformation("Unknown action '{0}'", context.Request.Path.Value);
                    }
                }
                else
                {
                    _logger.LogInformation("Unknown path '{0}'", context.Request.Path.Value);
                }
            }
            GXPerformance? _performance = null;
            if (action != CrudAction.None && type != null)
            {
                _performance = await GetPerformance(type);
                if (_performance != null)
                {
                    //Increase performance counters.
                    switch (action)
                    {
                        case CrudAction.Create:
                            _performance.AddCount = _performance.AddCount.GetValueOrDefault() + 1; ;
                            break;
                        case CrudAction.Read:
                            _performance.ReadCount = _performance.ReadCount.GetValueOrDefault() + 1; ;
                            break;
                        case CrudAction.Update:
                            _performance.UpdateCount = _performance.UpdateCount.GetValueOrDefault() + 1; ;
                            break;
                        case CrudAction.Delete:
                            _performance.DeleteCount = _performance.DeleteCount.GetValueOrDefault() + 1; ;
                            break;
                        case CrudAction.List:
                            _performance.ListCount = _performance.ListCount.GetValueOrDefault() + 1;
                            break;
                        case CrudAction.Clear:
                            _performance.ClearCount = _performance.ClearCount.GetValueOrDefault() + 1;
                            break;
                        case CrudAction.Next:
                            _performance.ListCount = _performance.ListCount.GetValueOrDefault() + 1;
                            break;
                    }
                }
                string request = await LogRequest(context);
                await LogResponse(context, action, type, request);
            }
            else
            {
                await _next(context);
            }
            try
            {
                if (action != CrudAction.None && type != null)
                {
                    if (_performance != null)
                    {
                        //Update performance time.
                        UInt64 time = (UInt64)(DateTime.Now - start).TotalMilliseconds;
                        switch (action)
                        {
                            case CrudAction.Create:
                                _performance.AddTime = time + _performance.AddTime.GetValueOrDefault();
                                break;
                            case CrudAction.Read:
                                _performance.ReadTime = time + _performance.ReadTime.GetValueOrDefault();
                                break;
                            case CrudAction.Update:
                                _performance.UpdateTime = time + _performance.UpdateTime.GetValueOrDefault();
                                break;
                            case CrudAction.Delete:
                                _performance.DeleteTime = time + _performance.DeleteTime.GetValueOrDefault();
                                break;
                            case CrudAction.List:
                                _performance.ListTime = time + _performance.ListTime.GetValueOrDefault();
                                break;
                            case CrudAction.Clear:
                                _performance.ClearTime = time + _performance.ClearTime.GetValueOrDefault();
                                break;
                            case CrudAction.Next:
                                _performance.ListTime = time + _performance.ListTime.GetValueOrDefault();
                                break;
                        }
                    }
                    if (id != null && isApi && statistic != null && (DateTime.Now - start).TotalMilliseconds >= statistic.RestTrigger)
                    {
                        GXRestStatistic diag = new GXRestStatistic();
                        diag.End = DateTime.Now;
                        diag.Start = start;
                        diag.Path = context.Request.Path;
                        diag.User = new GXUser() { Id = id };
                        GXInsertArgs arg = GXInsertArgs.Insert<GXRestStatistic>(diag);
                        await _host.Connection.InsertAsync(arg);
                    }
                }
            }
            catch (Exception)
            {
                //It's OK if this fails.
            }
        }

        private async Task<string> LogRequest(HttpContext context)
        {
            context.Request.EnableBuffering();
            try
            {
                await using var requestStream = _recyclableMemoryStreamManager.GetStream();
                await context.Request.Body.CopyToAsync(requestStream);
                return ReadStreamInChunks(requestStream);
            }
            finally
            {
                context.Request.Body.Position = 0;
            }
        }

        private string ReadStreamInChunks(Stream stream)
        {
            const int readChunkBufferLength = 4096;
            stream.Seek(0, SeekOrigin.Begin);
            using var textWriter = new StringWriter();
            using var reader = new StreamReader(stream);
            var readChunk = new char[readChunkBufferLength];
            int readChunkLength;
            do
            {
                readChunkLength = reader.ReadBlock(readChunk,
                                                   0,
                                                   readChunkBufferLength);
                textWriter.Write(readChunk, 0, readChunkLength);
            } while (readChunkLength > 0);
            return textWriter.ToString();
        }

        /// <summary>
        /// Add user action to the user log.
        /// </summary>
        private async Task AddLog(HttpContext context,
            CrudAction action,
            string target,
            string data,
            string text)
        {
            try
            {
                if (target == TargetType.UserAction ||
                    target == TargetType.AgentLog ||
                    target == TargetType.DeviceLog ||
                    target == TargetType.ScheduleLog ||
                    target == TargetType.ScriptLog)
                {
                    //Log data is not serialized.
                    text = "";
                }
                if (data != null && data.Length > 1000)
                {
                    data = data.Substring(0, 1000);
                }
                if (text != null && text.Length > 1000)
                {
                    text = text.Substring(0, 1000);
                }
                string id = ServerHelpers.GetUserId(context.User);
                GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id, where => where.Id == id);
                GXUser user = await _host.Connection.SingleOrDefaultAsync<GXUser>(args);
                GXUserAction log = new GXUserAction()
                {
                    TargetType = target,
                    User = user,
                    CreationTime = DateTime.Now,
                    Action = action,
                    Status = context.Response.StatusCode,
                    Data = data,
                    //Reply text is reset if user selects user action
                    //or reply increases too big over the time.
                    Reply = target == TargetType.UserAction ? null : text
                };
                GXInsertArgs arg = GXInsertArgs.Insert<GXUserAction>(log);
                await _host.Connection.InsertAsync(arg);
            }
            catch (Exception)
            {
                //It's OK if this fails.
            }
        }

        private async Task LogResponse(HttpContext context,
            CrudAction action,
            string type,
            string data)
        {
            var originalBodyStream = context.Response.Body;
            await using var responseBody = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseBody;
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                return;
            }
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string text = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await AddLog(context, action, type, data, text);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}
