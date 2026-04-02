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
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Shared.DIs;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Shared.DTOs.Content;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using System.Collections;
using Gurux.DLMS.AMI.Shared.DIs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Midlewares;
using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class DataExchangeRepository : IDataExchange
    {
        private readonly ClaimsPrincipal User;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILocalizationRepository _localizationRepository;
        private readonly GXLanguageService _languageService;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DataExchangeRepository(
            IGXAmiContextAccessor contextAccessor,
            IServiceProvider serviceProvider,
            ILocalizationRepository localizationRepository,
            GXLanguageService languageService)
        {
            var user = contextAccessor?.User;
            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = user;
            _serviceProvider = serviceProvider;
            _languageService = languageService;
            _localizationRepository = localizationRepository;
        }

        /// <inheritdoc/>
        public async Task<GXExportDataResponse> ExportDataAsync(GXExportData filter,
            CancellationToken cancellationToken)
        {
            List<GXExportDataItem> targetTypes = new List<GXExportDataItem>();
            if (filter.Targets == null || !filter.Targets.Any())
            {
                foreach (var it in typeof(TargetType).GetFields().Select(s => s.Name).ToArray())
                {
                    targetTypes.Add(new GXExportDataItem() { TargetType = it });
                }
            }
            else
            {
                targetTypes.AddRange(filter.Targets);
            }
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (var it in targetTypes)
            {
                IEnumerable<object>? ret = null;
                switch (it.TargetType)
                {
                    case TargetType.Script:
                        ret = await ExportScript(it, cancellationToken);
                        break;
                    case TargetType.ScriptGroup:
                        ret = await ExportScriptGroup(it, cancellationToken);
                        break;
                    case TargetType.ContentType:
                        ret = await ExportContentType(it, cancellationToken);
                        break;
                    case TargetType.ContentTypeGroup:
                        ret = await ExportContentTypeGroup(it, cancellationToken);
                        break;
                    case TargetType.Content:
                        ret = await ExportContent(it, cancellationToken);
                        break;
                    case TargetType.ContentGroup:
                        ret = await ExportContentGroup(it, cancellationToken);
                        break;
                    case TargetType.Schedule:
                        ret = await ExportSchedule(it, cancellationToken);
                        break;
                    case TargetType.ScheduleGroup:
                        ret = await ExportScheduleGroup(it, cancellationToken);
                        break;
                    case TargetType.Device:
                        ret = await ExportDevice(it, cancellationToken);
                        break;
                    case TargetType.DeviceGroup:
                        ret = await ExportDeviceGroup(it, cancellationToken);
                        break;
                    case TargetType.DeviceTemplate:
                        ret = await ExportDeviceTemplate(it, cancellationToken);
                        break;
                    case TargetType.DeviceTemplateGroup:
                        ret = await ExportDeviceTemplateGroup(it, cancellationToken);
                        break;
                    case TargetType.Gateway:
                        ret = await ExportGateway(it, cancellationToken);
                        break;
                    case TargetType.GatewayGroup:
                        ret = await ExportGatewayGroup(it, cancellationToken);
                        break;
                    default:
                        if (!(filter.Targets == null || filter.Targets.Any()))
                        {
                            throw new ArgumentException("Invalid target type " + it);
                        }
                        break;
                }
                if (ret is ICollection c && c.Count != 0)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }
                    sb.Append("{ \"" + it.TargetType + "\" : ");
                    sb.AppendLine(JsonSerializer.Serialize(ret,
                        new JsonSerializerOptions
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                            WriteIndented = true
                        }));
                    sb.AppendLine("}");
                }
            }
            return new GXExportDataResponse()
            {
                Data = sb.ToString()
            };
        }

        private async Task<IEnumerable<object>?> ExportContentGroup(GXExportDataItem it, CancellationToken cancellationToken)
        {
            var rep = _serviceProvider.GetService<IContentGroupRepository>();
            if (rep != null)
            {
                ListContentGroups req = new ListContentGroups()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken); //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }

        private async Task<IEnumerable<object>?> ExportContent(GXExportDataItem it, CancellationToken cancellationToken)
        {
            var rep = _serviceProvider.GetService<IContentRepository>();
            if (rep != null)
            {
                ListContents req = new ListContents()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken);
                //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.Collaborators = null;
                    item.ConcurrencyStamp = null;
                    if (item.Type != null)
                    {
                        item.Type.Id = Guid.Empty;
                    }
                }
                return list;
            }
            return null;
        }

        private async Task<IEnumerable<object>?> ExportSchedule(GXExportDataItem it, CancellationToken cancellationToken)
        {
            var rep = _serviceProvider.GetService<IScheduleRepository>();
            if (rep != null)
            {
                ListSchedules req = new ListSchedules()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken);
                //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ExecutionTime = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }

        private async Task<IEnumerable<object>?> ExportScheduleGroup(GXExportDataItem it, CancellationToken cancellationToken)
        {
            var rep = _serviceProvider.GetService<IScheduleGroupRepository>();
            if (rep != null)
            {
                ListScheduleGroups req = new ListScheduleGroups()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken); //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }

        private async Task<IEnumerable<object>?> ExportDevice(GXExportDataItem it, CancellationToken cancellationToken)
        {
            var rep = _serviceProvider.GetService<IDeviceRepository>();
            if (rep != null)
            {
                ListDevices req = new ListDevices()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken);
                //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }

        private async Task<IEnumerable<object>?> ExportDeviceGroup(GXExportDataItem it, CancellationToken cancellationToken)
        {
            IDeviceGroupRepository? rep = _serviceProvider.GetService<IDeviceGroupRepository>();
            if (rep != null)
            {
                ListDeviceGroups req = new ListDeviceGroups()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken);
                //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }

        private async Task<IEnumerable<object>?> ExportDeviceTemplate(GXExportDataItem it, CancellationToken cancellationToken)
        {
            var rep = _serviceProvider.GetService<IDeviceTemplateRepository>();
            if (rep != null)
            {
                ListDeviceTemplates req = new ListDeviceTemplates()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken);
                //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }

        private async Task<IEnumerable<object>?> ExportDeviceTemplateGroup(GXExportDataItem it, CancellationToken cancellationToken)
        {
            var rep = _serviceProvider.GetService<IDeviceTemplateGroupRepository>();
            if (rep != null)
            {
                ListDeviceTemplateGroups req = new ListDeviceTemplateGroups()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken);
                //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }


        private async Task<IEnumerable<object>?> ExportGateway(GXExportDataItem it, CancellationToken cancellationToken)
        {
            var rep = _serviceProvider.GetService<IGatewayRepository>();
            if (rep != null)
            {
                ListGateways req = new ListGateways()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken);
                //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }

        private async Task<IEnumerable<object>?> ExportGatewayGroup(GXExportDataItem it, CancellationToken cancellationToken)
        {
            var rep = _serviceProvider.GetService<IGatewayGroupRepository>();
            if (rep != null)
            {
                ListGatewayGroups req = new ListGatewayGroups()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken);
                //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }


        private async Task<IEnumerable<object>?> ExportContentType(GXExportDataItem it, CancellationToken cancellationToken)
        {
            IContentTypeRepository? rep = _serviceProvider.GetService<IContentTypeRepository>();
            if (rep != null)
            {
                ListContentTypes req = new ListContentTypes()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken);
                //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }

        private async Task<IEnumerable<object>?> ExportContentTypeGroup(GXExportDataItem it, CancellationToken cancellationToken)
        {
            IContentGroupRepository? rep = _serviceProvider.GetService<IContentGroupRepository>();
            if (rep != null)
            {
                ListContentGroups req = new ListContentGroups()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken); //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }

        private async Task<IEnumerable<object>?> ExportScript(GXExportDataItem it, CancellationToken cancellationToken)
        {
            IScriptRepository? rep = _serviceProvider.GetService<IScriptRepository>();
            if (rep != null)
            {
                ListScripts req = new ListScripts()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken);
                //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }

        private async Task<IEnumerable<object>?> ExportScriptGroup(GXExportDataItem it, CancellationToken cancellationToken)
        {
            IContentGroupRepository? rep = _serviceProvider.GetService<IContentGroupRepository>();
            if (rep != null)
            {
                ListContentGroups req = new ListContentGroups()
                {
                    Included = it.Ids?.ToArray()
                };
                var list = await rep.ListAsync(req, null, cancellationToken); //Reset private values.
                foreach (var item in list)
                {
                    item.Id = Guid.Empty;
                    item.Creator = null;
                    item.CreationTime = null;
                    item.Updated = null;
                    item.ConcurrencyStamp = null;
                }
                return list;
            }
            return null;
        }

        private async Task<int?> ImportScript(GXImportData data, JsonElement value)
        {
            List<GXScript>? list = new List<GXScript>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXScript[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXScript>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }
            var rep = _serviceProvider.GetService<IScriptRepository>();
            if (list != null && rep != null)
            {
                var removed = new List<GXScript>();
                foreach (var it in list)
                {
                    var req = new ListScripts();
                    req.Filter = new GXScript()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.Script, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(w => removed.Contains(w));
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        private async Task<int?> ImportScriptGroup(GXImportData data, JsonElement value)
        {
            List<GXScriptGroup>? list = new List<GXScriptGroup>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXScriptGroup[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXScriptGroup>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }

            var rep = _serviceProvider.GetService<IScriptGroupRepository>();
            if (list != null && rep != null)
            {
                List<GXScriptGroup> removed = new List<GXScriptGroup>();
                foreach (var it in list)
                {
                    var req = new ListScriptGroups();
                    req.Filter = new GXScriptGroup()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.ScriptGroup, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(w => removed.Contains(w));
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        private async Task<int?> ImportContentType(GXImportData data, JsonElement value)
        {
            List<GXContentType>? list = new List<GXContentType>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXContentType[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXContentType>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }
            var rep = _serviceProvider.GetService<IContentTypeRepository>();
            if (list != null && rep != null)
            {
                var removed = new List<GXContentType>();
                foreach (var it in list)
                {
                    var req = new ListContentTypes();
                    req.Filter = new GXContentType()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.ContentType, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(w => removed.Contains(w));
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        private async Task<int?> ImportContentTypeGroup(GXImportData data, JsonElement value)
        {
            List<GXContentTypeGroup>? list = new List<GXContentTypeGroup>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXContentTypeGroup[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXContentTypeGroup>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }

            var rep = _serviceProvider.GetService<IContentTypeGroupRepository>();
            if (list != null && rep != null)
            {
                List<GXContentTypeGroup> removed = new List<GXContentTypeGroup>();
                foreach (var it in list)
                {
                    var req = new ListContentTypeGroups();
                    req.Filter = new GXContentTypeGroup()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.ContentTypeGroup, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(w => removed.Contains(w));
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        private async Task<int?> ImportContent(GXImportData data, JsonElement value)
        {
            List<GXContent>? list = new List<GXContent>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXContent[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXContent>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }
            var rep2 = _serviceProvider.GetService<IContentTypeRepository>();
            foreach (var it in list)
            {
                if (string.IsNullOrEmpty(it.Type?.Name))
                {
                    throw new ArgumentNullException(Properties.Resources.InvalidName);
                }
                var req = new ListContentTypes();
                req.Filter = new GXContentType()
                {
                    Name = it.Type.Name,
                };
                var types = await rep2.ListAsync(req);
                if (types == null || types.Length != 1)
                {
                    throw new ArgumentException("Invalid content type.");
                }
                it.Type = types[0];
            }
            var rep = _serviceProvider.GetService<IContentRepository>();
            if (list != null && rep != null)
            {
                List<GXContent> removed = new List<GXContent>();
                foreach (var it in list)
                {
                    var req = new ListContents();
                    req.Filter = new GXContent()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.Content, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(w => removed.Contains(w));
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        private async Task<int?> ImportContentGroup(GXImportData data, JsonElement value)
        {
            List<GXContentGroup>? list = new List<GXContentGroup>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXContentGroup[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXContentGroup>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }

            var rep = _serviceProvider.GetService<IContentGroupRepository>();
            if (list != null && rep != null)
            {
                List<GXContentGroup> removed = new List<GXContentGroup>();
                foreach (var it in list)
                {
                    var req = new ListContentGroups();
                    req.Filter = new GXContentGroup()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.ContentGroup, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(w => removed.Contains(w));
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        private async Task<int?> ImportSchedule(GXImportData data, JsonElement value)
        {
            List<GXSchedule>? list = new List<GXSchedule>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXSchedule[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXSchedule>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }

            var rep = _serviceProvider.GetService<IScheduleRepository>();
            if (list != null && rep != null)
            {
                List<GXSchedule> removed = new List<GXSchedule>();
                foreach (var it in list)
                {
                    var req = new ListSchedules();
                    req.Filter = new GXSchedule()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.Schedule, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(w => removed.Contains(w));
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        private async Task<int?> ImportScheduleGroup(GXImportData data, JsonElement value)
        {
            List<GXScheduleGroup>? list = new List<GXScheduleGroup>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXScheduleGroup[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXScheduleGroup>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }

            var rep = _serviceProvider.GetService<IScheduleGroupRepository>();
            if (list != null && rep != null)
            {
                List<GXScheduleGroup> removed = new List<GXScheduleGroup>();
                foreach (var it in list)
                {
                    var req = new ListScheduleGroups();
                    req.Filter = new GXScheduleGroup()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.ScheduleGroup, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(removed.Contains);
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        private async Task<int?> ImportDevice(GXImportData data, JsonElement value, CancellationToken cancellationToken)
        {
            List<GXDevice>? list = new List<GXDevice>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXDevice[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXDevice>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }

            var rep = _serviceProvider.GetService<IDeviceRepository>();
            if (list != null && rep != null)
            {
                await rep.UpdateAsync(list, cancellationToken);
            }
            return list?.Count;
        }

        private async Task<int?> ImportDeviceGroup(GXImportData data, JsonElement value, CancellationToken cancellationToken)
        {
            List<GXDeviceGroup>? list = new List<GXDeviceGroup>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXDeviceGroup[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXDeviceGroup>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }

            var rep = _serviceProvider.GetService<IDeviceGroupRepository>();
            if (list != null && rep != null)
            {
                List<GXDeviceGroup> removed = new List<GXDeviceGroup>();
                foreach (var it in list)
                {
                    var req = new ListDeviceGroups();
                    req.Filter = new GXDeviceGroup()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.DeviceGroup, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(w => removed.Contains(w));
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        private async Task<int?> ImportDeviceTemplate(GXImportData data, JsonElement value, CancellationToken cancellationToken)
        {
            List<GXDeviceTemplate>? list = new List<GXDeviceTemplate>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXDeviceTemplate[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXDeviceTemplate>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }

            var rep = _serviceProvider.GetService<IDeviceTemplateRepository>();
            if (list != null && rep != null)
            {
                List<GXDeviceTemplate> removed = new List<GXDeviceTemplate>();
                foreach (var it in list)
                {
                    var req = new ListDeviceTemplates();
                    req.Filter = new GXDeviceTemplate()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.DeviceTemplate, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(removed.Contains);
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        private async Task<int?> ImportDeviceTemplateGroup(GXImportData data, JsonElement value, CancellationToken cancellationToken)
        {
            List<GXDeviceTemplateGroup>? list = new List<GXDeviceTemplateGroup>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXDeviceTemplateGroup[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXDeviceTemplateGroup>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }

            var rep = _serviceProvider.GetService<IDeviceTemplateGroupRepository>();
            if (list != null && rep != null)
            {
                List<GXDeviceTemplateGroup> removed = new List<GXDeviceTemplateGroup>();
                foreach (var it in list)
                {
                    var req = new ListDeviceTemplateGroups();
                    req.Filter = new GXDeviceTemplateGroup()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.DeviceTemplateGroup, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(w => removed.Contains(w));
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        private async Task<int?> ImportGateway(GXImportData data, JsonElement value)
        {
            List<GXGateway>? list = new List<GXGateway>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXGateway[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXGateway>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }

            var rep = _serviceProvider.GetService<IGatewayRepository>();
            if (list != null && rep != null)
            {
                List<GXGateway> removed = new List<GXGateway>();
                foreach (var it in list)
                {
                    var req = new ListGateways();
                    req.Filter = new GXGateway()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.Gateway, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(removed.Contains);
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        private async Task<int?> ImportGatewayGroup(GXImportData data, JsonElement value)
        {
            List<GXGatewayGroup>? list = new List<GXGatewayGroup>();
            if (value.ValueKind == JsonValueKind.Array)
            {
                list = value.Deserialize<GXGatewayGroup[]>()?.ToList();
            }
            else
            {
                var item = value.Deserialize<GXGatewayGroup>();
                if (item == null)
                {
                    throw new ArgumentException();
                }
                list = [item];
            }

            var rep = _serviceProvider.GetService<IGatewayGroupRepository>();
            if (list != null && rep != null)
            {
                List<GXGatewayGroup> removed = new List<GXGatewayGroup>();
                foreach (var it in list)
                {
                    var req = new ListGatewayGroups();
                    req.Filter = new GXGatewayGroup()
                    {
                        Name = it.Name,
                    };
                    var types = await rep.ListAsync(req);
                    switch (data.Rule)
                    {
                        case DataExchangeRule.Keep:
                            if (types.Any())
                            {
                                removed.Add(it);
                            }
                            break;
                        case DataExchangeRule.Replace:
                            if (types.Length == 1)
                            {
                                it.Id = types[0].Id;
                                removed.Add(it);
                            }
                            else if (types.Length > 1)
                            {
                                throw new ArgumentException(Properties.Resources.Name);
                            }
                            break;
                        case DataExchangeRule.Copy:
                            if (types.Any())
                            {
                                int index = 1;
                                while (types.Where(w => w.Name == it.Name + index).Any())
                                {
                                    ++index;
                                }
                                it.Name += index;
                            }
                            break;
                        case DataExchangeRule.Error:
                            throw new ArgumentException(string.Format(Properties.Resources.AlreadyExists, Properties.Resources.GatewayGroup, it.Name));
                    }
                }
                if (removed.Any())
                {
                    list.RemoveAll(w => removed.Contains(w));
                }
                if (list.Any())
                {
                    await rep.UpdateAsync(list);
                }
            }
            return list?.Count;
        }

        /// <inheritdoc/>
        public async Task<GXImportDataResponse> ImportDataAsync(
            GXImportData data,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(data.Data))
            {
                throw new ArgumentException();
            }
            string str = "{ \"Targets\" : [" + data.Data + "]}";
            List<KeyValuePair<string, Guid>> list = new List<KeyValuePair<string, Guid>>();
            List<GXTargetType> types = new List<GXTargetType>();
            using (JsonDocument doc = JsonDocument.Parse(str))
            {
                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Object ||
                        property.Value.ValueKind == JsonValueKind.Array)
                    {
                        var sortedProperties = property.Value.EnumerateArray().SelectMany(s => s.EnumerateObject()).ToList();
                        sortedProperties.Sort(new PropertyComparer());
                        foreach (var it in sortedProperties)
                        {
                            int? ret = (await Import(data, it, cancellationToken));
                            types.Add(new GXTargetType()
                            {
                                Id = it.Name,
                                Count = ret
                            });
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Invalid target type " + property.Value);
                    }
                }
            }
            return new GXImportDataResponse()
            {
                Types = types.ToArray()
            };
        }

        private async Task<int?> Import(
            GXImportData data,
            JsonProperty property,
            CancellationToken cancellationToken)
        {
            int? ret;
            switch (property.Name)
            {
                case TargetType.Script:
                    ret = await ImportScript(data, property.Value);
                    break;
                case TargetType.ScriptGroup:
                    ret = await ImportScriptGroup(data, property.Value);
                    break;
                case TargetType.ContentType:
                    ret = await ImportContentType(data, property.Value);
                    break;
                case TargetType.ContentTypeGroup:
                    ret = await ImportContentTypeGroup(data, property.Value);
                    break;
                case TargetType.Content:
                    ret = await ImportContent(data, property.Value);
                    break;
                case TargetType.ContentGroup:
                    ret = await ImportContentGroup(data, property.Value);
                    break;
                case TargetType.Schedule:
                    ret = await ImportSchedule(data, property.Value);
                    break;
                case TargetType.ScheduleGroup:
                    ret = await ImportScheduleGroup(data, property.Value);
                    break;
                case TargetType.Device:
                    ret = await ImportDevice(data, property.Value, cancellationToken);
                    break;
                case TargetType.DeviceGroup:
                    ret = await ImportDeviceGroup(data, property.Value, cancellationToken);
                    break;
                case TargetType.DeviceTemplate:
                    ret = await ImportDeviceTemplate(data, property.Value, cancellationToken);
                    break;
                case TargetType.DeviceTemplateGroup:
                    ret = await ImportDeviceTemplateGroup(data, property.Value, cancellationToken);
                    break;
                case TargetType.Gateway:
                    ret = await ImportGateway(data, property.Value);
                    break;
                case TargetType.GatewayGroup:
                    ret = await ImportGatewayGroup(data, property.Value);
                    break;
                default:
                    throw new ArgumentException("Invalid target type " + property.Name);
            }
            return ret;
        }

        /// <inheritdoc/>
        public async Task<GXDataExchangeTypeResponse> AvailableTypes(
            GXDataExchangeType? req,
            CancellationToken cancellationToken = default)
        {
            List<GXTargetType> list = new List<GXTargetType>();
            string[] arr = [TargetType.ContentType, TargetType.ContentTypeGroup, TargetType.Content,
                    TargetType.ContentGroup, TargetType.Schedule, TargetType.ScheduleGroup,
                    TargetType.Device, TargetType.DeviceGroup, TargetType.DeviceTemplate,
                    TargetType.DeviceTemplateGroup, TargetType.Gateway, TargetType.GatewayGroup];
            foreach (var it in arr)
            {
                list.Add(new GXTargetType()
                {
                    Id = it
                });
            }           
            return new GXDataExchangeTypeResponse()
            {
                Types = list.ToArray()
            };
        }
    }
}
