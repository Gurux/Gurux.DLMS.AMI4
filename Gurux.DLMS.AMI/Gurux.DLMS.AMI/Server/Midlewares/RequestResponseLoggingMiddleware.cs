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

        StatisticSettings? statistic;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RequestResponseLoggingMiddleware(RequestDelegate next,
            IGXHost host,
            IConfigurationRepository configurationRepository,
            ILoggerFactory loggerFactory)
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
        }

        public async Task Invoke(HttpContext context)
        {
            DateTime start = DateTime.Now;
            CrudAction action = CrudAction.None;
            TargetType type = TargetType.None;
            string? id = ServerHelpers.GetUserId(context.User, false);
            try
            {
                id = ServerHelpers.GetUserId(context.User, false);
            }
            catch (Exception)
            {
                //It's OK if this fails.
            }
            //SignalR events are not saved.
            bool isApi = context.Request.Path.StartsWithSegments("/api");

            //Save user actions.
            if (id != null && statistic != null && statistic.UserActions && isApi)
            {
                string[] items = context.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (items.Length == 3)
                {
                    if (!Enum.TryParse<TargetType>(items[1], true, out type))
                    {
                        _logger.LogInformation("Unknown Target type '{0}'", context.Request.Path.Value);
                    }
                    if (!Enum.TryParse<CrudAction>(items[2], true, out action))
                    {
                        _logger.LogInformation("Unknown action '{0}'", context.Request.Path.Value);
                    }
                }
                else
                {
                    _logger.LogInformation("Unknown path '{0}'", context.Request.Path.Value);
                }
            }
            if (action != CrudAction.None && type != TargetType.None)
            {
                string request = await LogRequest(context);
                await LogResponse(context, action, type, request);
            }
            else
            {
                await _next(context);
            }
            try
            {
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
            TargetType target,
            string data,
            string text)
        {
            try
            {
                string id = ServerHelpers.GetUserId(context.User);
                GXSelectArgs args = GXSelectArgs.Select<GXUser>(s => s.Id, where => where.Id == id);
                GXUser user = await _host.Connection.SingleOrDefaultAsync<GXUser>(args);
                GXUserAction log = new GXUserAction()
                {
                    Target = target,
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
            TargetType type,
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
