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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DIs;
using System.Data;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.Linq.Expressions;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.Text;
using System.Diagnostics;
using Gurux.DLMS.AMI.Shared;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class DeviceRepository : IDeviceRepository
    {
        private readonly IGXHost _host;
        private readonly IGXEventsNotifier _eventsNotifier;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDeviceGroupRepository _deviceGroupRepository;
        private readonly IUserRepository _userRepository;
        private readonly IKeyManagementRepository _keyManagementRepository;
        private readonly IAttributeRepository _attributeRepository;
        private readonly GXPerformanceSettings _performanceSettings;
        private readonly IObjectRepository _objectRepository;
        /// <summary>
        /// Constructor.
        /// </summary>
        public DeviceRepository(
            IGXHost host,
            IServiceProvider serviceProvider,
            IDeviceGroupRepository deviceGroupRepository,
            IGXEventsNotifier eventsNotifier,
            IUserRepository userRepository,
            IKeyManagementRepository keyManagementRepository,
            IAttributeRepository attributeRepository,
            GXPerformanceSettings performanceSettings,
            IObjectRepository objectRepository)
        {
            _host = host;
            _serviceProvider = serviceProvider;
            _eventsNotifier = eventsNotifier;
            _deviceGroupRepository = deviceGroupRepository;
            _userRepository = userRepository;
            _keyManagementRepository = keyManagementRepository;
            _attributeRepository = attributeRepository;
            _performanceSettings = performanceSettings;
            _objectRepository = objectRepository;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(
            ClaimsPrincipal user,
            Guid? deviceId)
        {
            GXSelectArgs args = GXQuery.GetUsersByDevice(ServerHelpers.GetUserId(user), deviceId);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task<List<string>> GetUsersAsync(
            ClaimsPrincipal user,
            IEnumerable<Guid>? deviceIds)
        {
            GXSelectArgs args = GXQuery.GetUsersByDevices(ServerHelpers.GetUserId(user), deviceIds);
            List<GXUser> users = await _host.Connection.SelectAsync<GXUser>(args);
            List<string> ret = users.Select(s => s.Id).ToList();
            if (user.IsInRole(GXRoles.Admin))
            {
                ret.AddDistinct(await _userRepository.GetUserIdsInRoleAsync(user, new string[] { GXRoles.Admin }));
            }
            return ret;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(ClaimsPrincipal User, IEnumerable<Guid> devices,
            bool delete)
        {
            if (User == null || (!User.IsInRole(GXRoles.Admin) && !User.IsInRole(GXRoles.DeviceManager)))
            {
                throw new UnauthorizedAccessException();
            }

            GXSelectArgs arg = GXSelectArgs.Select<GXDevice>(a => a.Id, q => devices.Contains(q.Id));
            List<GXDevice> list = _host.Connection.Select<GXDevice>(arg);
            DateTime now = DateTime.Now;
            Dictionary<GXDevice, List<string>> updates = new Dictionary<GXDevice, List<string>>();
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                foreach (var it in list)
                {
                    it.Removed = now;
                    List<string> users = await GetUsersAsync(User, it.Id);
                    if (!delete)
                    {
                        _host.Connection.Update(transaction, GXUpdateArgs.Update(it, q => q.Removed));
                    }
                    updates[it] = users;
                }
                if (delete)
                {
                    GXDeleteArgs args1;
                    //////////////////////////////////////////////////////
                    //It's faster to remove atribute templates, object templates and device templates with own query than removing just device templates.
                    //////////////////////////////////////////////////////
                    //Delete attributes.
                    arg = GXSelectArgs.Select<GXObject>(a => a.Id);
                    arg.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device, j => j.Id);
                    arg.Where.And<GXDevice>(q => devices.Contains(q.Id));
                    args1 = GXDeleteArgs.Delete<GXAttribute>(w => GXSql.Exists<GXAttribute, GXObject>(j => j.Object, j => j.Id, arg));
                    await _host.Connection.DeleteAsync(transaction, args1);
                    //Delete objects.
                    arg = GXSelectArgs.Select<GXDevice>(a => a.Id, q => devices.Contains(q.Id));
                    args1 = GXDeleteArgs.Delete<GXObject>(w => GXSql.Exists<GXObject, GXDevice>(j => j.Device, j => j.Id, arg));
                    await _host.Connection.DeleteAsync(transaction, args1);
                    //Delete devices.
                    var args = GXDeleteArgs.Delete<GXDevice>(q => devices.Contains(q.Id));
                    await _host.Connection.DeleteAsync(transaction, args);
                    // var args = GXDeleteArgs.DeleteRange(list);
                    // await _host.Connection.DeleteAsync(transaction, args);
                }
                _host.Connection.CommitTransaction(transaction);
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.DeviceDelete(it.Value, new GXDevice[] { it.Key });
            }
        }

        /// <inheritdoc />
        public async Task<GXDevice[]> ListAsync(
            ClaimsPrincipal User,
            ListDevices? request,
            ListDevicesResponse? response,
            CancellationToken cancellationToken)
        {
            GXSelectArgs arg;
            bool all = request != null && request.AllUsers && User.IsInRole(GXRoles.Admin);
            if (all)
            {
                //Admin can see all the devices.
                arg = GXSelectArgs.SelectAll<GXDevice>();
            }
            else
            {
                string userId = ServerHelpers.GetUserId(User);
                arg = GXQuery.GetDevicesByUser(userId, false, Guid.Empty);
            }
            arg.Columns.Add<GXDeviceTemplate>(s => new { s.Id, s.Type, s.Name });
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(j => j.Template, j => j.Id);
            //If devices are filtered by user.
            if (request?.Filter?.DeviceGroups != null &&
                request.Filter.DeviceGroups.SingleOrDefault() is GXDeviceGroup dg)
            {
                if (dg.UserGroups != null &&
                    dg.UserGroups.SingleOrDefault() is GXUserGroup ug)
                {
                    if (ug?.Users != null && ug.Users.Any())
                    {
                        var user = ug.Users.FirstOrDefault();
                        if (user != null)
                        {
                            GXSelectArgs userGroups = GXSelectArgs.Select<GXDeviceGroupDevice>(s => s.DeviceId);
                            userGroups.Joins.AddLeftJoin<GXDeviceGroupDevice, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
                            userGroups.Joins.AddLeftJoin<GXDeviceGroup, GXUserGroupDeviceGroup>(j => j.Id, j => j.DeviceGroupId);
                            userGroups.Joins.AddLeftJoin<GXUserGroupDeviceGroup, GXUserGroup>(j => j.UserGroupId, j => j.Id);
                            userGroups.Joins.AddLeftJoin<GXUserGroup, GXUserGroupUser>(j => j.Id, j => j.UserGroupId);
                            userGroups.Joins.AddLeftJoin<GXUserGroupUser, GXUser>(j => j.UserId, j => j.Id);
                            userGroups.Where.FilterBy(user);
                            arg.Where.And<GXDevice>(q => GXSql.Exists<GXDevice, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceId, userGroups));
                        }
                    }
                }
                request.Filter.DeviceGroups = null;
            }
            else if ((request?.Filter?.Objects?.SingleOrDefault()?.Template?.LogicalName is string ldn) &&
                (request?.Filter?.Objects?.SingleOrDefault()?.Attributes?.SingleOrDefault()?.Value is string att))
            {
                //If devices are filtered by attribute. E.g. logical device name.
                request.Filter.Objects = null;
                arg.Joins.AddInnerJoin<GXDevice, GXObject>(j => j.Id, j => j.Device);
                arg.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object);
                arg.Joins.AddInnerJoin<GXDeviceTemplate, GXObjectTemplate>(j => j.Id, j => j.DeviceTemplate);
                arg.Joins.AddInnerJoin<GXObjectTemplate, GXAttributeTemplate>(j => j.Id, j => j.ObjectTemplate);
                arg.Where.And<GXObjectTemplate>(w => w.LogicalName == ldn);
                arg.Where.And<GXAttribute>(w => w.Value == att);
            }
            else if (request?.Filter?.Template != null)
            {
                arg.Where.FilterBy(request.Filter.Template);
                request.Filter.Template = null;
            }
            if (request != null && (request.Select & TargetType.DeviceTemplate) != 0)
            {
                arg.Columns.Add<GXDeviceTemplate>();
                arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(x => x.Template, y => y.Id);
                arg.Where.And<GXDeviceTemplate>(w => w.Removed == null);
            }
            arg.Columns.Exclude<GXDevice>(e => new { e.Settings, e.MediaSettings });
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Exclude != null && request.Exclude.Any())
                {
                    arg.Where.And<GXDevice>(w => !request.Exclude.Contains(w.Id));
                }
            }
            arg.Distinct = true;
            if (request != null && request.Count != 0)
            {
                //Return total row count. This can be used for paging.
                GXSelectArgs total = GXSelectArgs.Select<GXDevice>(q => GXSql.DistinctCount(q.Id));
                total.Joins.Append(arg.Joins);
                total.Where.Append(arg.Where);
                if (response != null)
                {
                    response.Count = _host.Connection.SingleOrDefault<int>(total);
                }
                arg.Index = (UInt32)request.Index;
                arg.Count = (UInt32)request.Count;
            }
            if (request != null && !string.IsNullOrEmpty(request.OrderBy))
            {
                arg.Descending = request.Descending;
                arg.OrderBy.Add<GXDevice>(request.OrderBy);
            }
            else
            {
                arg.Descending = true;
                arg.OrderBy.Add<GXDevice>(q => q.CreationTime);
            }
            GXDevice[] devices = (await _host.Connection.SelectAsync<GXDevice>(arg)).ToArray();
            //Empty non needed values.
            foreach (var it in devices)
            {
                it.Actions = null;
                it.DeviceGroups = null;
                it.Errors = null;
                it.Parameters = null;
                it.Tasks = null;
                it.Traces = null;
                it.Keys = null;
                if (request != null && (request.Select & TargetType.Object) != 0)
                {
                    //Get objects.
                    arg = GXSelectArgs.Select<GXObject>(s => new { s.Id, s.Template });
                    if (request != null && (request.Select & TargetType.ObjectTemplate) != 0)
                    {
                        arg.Columns.Add<GXObjectTemplate>(s => new { s.Id, s.Name, s.Attributes });
                        arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template, j => j.Id);
                    }
                    if (request != null && (request.Select & TargetType.Attribute) != 0)
                    {
                        arg.Columns.Add<GXAttribute>(s => new { s.Id, s.Template });
                        arg.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object);
                        if (request != null && (request.Select & TargetType.AttributeTemplate) != 0)
                        {
                            arg.Columns.Add<GXAttributeTemplate>(s => new { s.Id, s.Name });
                            arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(j => j.Template, j => j.Id);
                        }
                    }
                    it.Objects = (await _host.Connection.SelectAsync<GXObject>(arg)).ToList();
                }
                else
                {
                    it.Objects = null;
                }
                if (request != null && (request.Select & TargetType.DeviceGroup) != 0)
                {
                    //Get device groups.
                    arg = GXSelectArgs.Select<GXDeviceGroup>(s => new { s.Id, s.Name }, w => w.Removed == null);
                    arg.Joins.AddInnerJoin<GXDeviceGroup, GXDeviceGroupDevice>(j => j.Id, j => j.DeviceGroupId);
                    arg.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDevice>(j => j.DeviceId, j => j.Id);
                    arg.Where.And<GXDevice>(w => w.Removed == null && w.Id == it.Id);
                    it.DeviceGroups = (await _host.Connection.SelectAsync<GXDeviceGroup>(arg)).ToList();
                }
            }

            if (response != null)
            {
                response.Devices = devices;
                if (response.Count == 0)
                {
                    response.Count = devices.Length;
                }
            }
            return devices;
        }

        /// <inheritdoc />
        public async Task<GXDevice> ReadAsync(
           ClaimsPrincipal User,
           Guid id)
        {
            string userId = ServerHelpers.GetUserId(User);
            GXSelectArgs arg = GXQuery.GetDevicesByUser(userId, false, id);
            arg.Where.And<GXDeviceTemplate>(q => q.Removed == null);
            arg.Columns.Add<GXDeviceTemplate>();
            arg.Columns.Add<GXDeviceGroup>(s => new { s.Id, s.Name });
            arg.Columns.Exclude<GXDeviceGroup>(e => e.Devices);
            arg.Joins.AddInnerJoin<GXDevice, GXDeviceTemplate>(x => x.Template, y => y.Id);
            arg.Joins.AddInnerJoin<GXDeviceGroupDevice, GXDeviceGroup>(j => j.DeviceGroupId, j => j.Id);
            bool isAdmin = User.IsInRole(GXRoles.Admin);
            GXDevice device = await _host.Connection.SingleOrDefaultAsync<GXDevice>(arg);
            if (device == null)
            {
                throw new ArgumentException(Properties.Resources.UnknownTarget);
            }
            //Get device parameters.
            arg = GXSelectArgs.SelectAll<GXDeviceParameter>(w => w.Device == device && w.Removed == null);
            arg.Columns.Add<GXModule>(s => s.Id);
            arg.Columns.Exclude<GXModule>(e => e.DeviceParameters);
            arg.Joins.AddInnerJoin<GXDeviceParameter, GXModule>(j => j.Module, j => j.Id);
            device.Parameters = await _host.Connection.SelectAsync<GXDeviceParameter>(arg);
            //Get device password. This is obsolete.
            //The Agent ask keys from the key management.
            if (!string.IsNullOrEmpty(device.Settings))
            {
                var s = JsonSerializer.Deserialize<Shared.DTOs.GXDLMSSettings>(device.Settings);
                if (s != null && s.HexPassword == null)
                {
                    if (s.Authentication != (byte)Enums.Authentication.None ||
                        s.Security != (byte)Enums.Security.None)
                    {
                        ListKeyManagements req = new ListKeyManagements()
                        {
                            Filter = new GXKeyManagement()
                            {
                                Device = device
                            },
                            Select = TargetType.KeyManagementKey
                        };
                        var list = await _keyManagementRepository.ListAsync(User, req, null, default);
                        //Update password.
                        foreach (var it in list)
                        {
                            if (it.Keys != null)
                            {
                                foreach (var key in it.Keys)
                                {
                                    if ((byte)key.KeyType.GetValueOrDefault(0) == s.Authentication &&
                                        !string.IsNullOrEmpty(key.Data))
                                    {
                                        if (key.IsHex.GetValueOrDefault(false))
                                        {
                                            s.HexPassword = GXDLMSTranslator.HexToBytes(key.Data);
                                        }
                                        else
                                        {
                                            s.HexPassword = ASCIIEncoding.ASCII.GetBytes(key.Data);
                                        }
                                        break;
                                    }
                                }
                            }
                        }

                        //Update block cipher key.
                        if (s.Authentication == (byte)Enums.Authentication.HighGMAC ||
                            s.Security != 0)
                        {
                            foreach (var it in list)
                            {
                                if (it.Keys != null)
                                {
                                    foreach (var key in it.Keys)
                                    {
                                        if (key.KeyType.GetValueOrDefault(0) == KeyManagementType.BlockCipher &&
                                            !string.IsNullOrEmpty(key.Data))
                                        {
                                            if (key.IsHex.GetValueOrDefault(false))
                                            {
                                                s.BlockCipherKey = key.Data;
                                            }
                                            else
                                            {
                                                var bytes = ASCIIEncoding.ASCII.GetBytes(key.Data);
                                                s.BlockCipherKey = GXDLMSTranslator.ToHex(bytes, false);
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        //Update authentication key.
                        if (s.Authentication == (byte)Enums.Authentication.HighGMAC ||
                            s.Security != 0)
                        {
                            foreach (var it in list)
                            {
                                if (it.Keys != null)
                                {
                                    foreach (var key in it.Keys)
                                    {
                                        if (key.KeyType.GetValueOrDefault(0) == KeyManagementType.Authentication &&
                                            !string.IsNullOrEmpty(key.Data))
                                        {
                                            if (key.IsHex.GetValueOrDefault(false))
                                            {
                                                s.AuthenticationKey = key.Data;
                                            }
                                            else
                                            {
                                                var bytes = ASCIIEncoding.ASCII.GetBytes(key.Data);
                                                s.AuthenticationKey = GXDLMSTranslator.ToHex(bytes, false);
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        device.Settings = JsonSerializer.Serialize(s);
                    }
                }
            }
            return device;
        }

        private void UpdateKeys(GXKeyManagement management, KeyManagementType type, string? data, byte[]? hexData)
        {
            bool found = false;
            bool isHex = hexData != null && hexData.Any();
            if (management.Keys == null)
            {
                management.Keys = new List<GXKeyManagementKey>();
            }
            foreach (var key in management.Keys)
            {
                if (key.KeyType == type)
                {
                    found = true;
                    key.IsHex = isHex;
                    key.Data = isHex ? GXDLMSTranslator.ToHex(hexData) : data;
                    break;
                }
            }
            if (!found)
            {
                management.Keys.Add(new GXKeyManagementKey()
                {
                    KeyType = type,
                    IsHex = isHex,
                    Data = isHex ? GXDLMSTranslator.ToHex(hexData) : data
                });
            }
        }

        /// <inheritdoc />
        public async Task<Guid[]> UpdateAsync(
            ClaimsPrincipal User,
            IEnumerable<GXDevice> devices,
            CancellationToken cancellationToken,
            Expression<Func<GXDevice, object?>>? columns,
            bool lateBinding)
        {
            DateTime now = DateTime.Now;
            GXUser creator = new GXUser() { Id = ServerHelpers.GetUserId(User) };
            List<Guid> updated = new List<Guid>();
            var newDevices = devices.Where(w => w.Id == Guid.Empty).ToList();
            var updatedDevices = devices.Where(w => w.Id != Guid.Empty).ToList();
            Dictionary<GXDevice, List<GXDeviceGroup>> deviceGroups = new Dictionary<GXDevice, List<GXDeviceGroup>>();
            List<GXDeviceGroup>? defaultGroups = null;
            //Get notified users.
            if (newDevices.Any())
            {
                if (defaultGroups == null)
                {
                    //Get default device groups.
                    ListDeviceGroups request = new ListDeviceGroups()
                    {
                        Filter = new GXDeviceGroup() { Default = true }
                    };
                    defaultGroups = new List<GXDeviceGroup>(await _deviceGroupRepository.ListAsync(User, request, null, CancellationToken.None));
                    if (!defaultGroups.Any())
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                }
                foreach (var it in newDevices)
                {
                    if (it.DeviceGroups == null || !it.DeviceGroups.Any())
                    {
                        it.DeviceGroups = defaultGroups;
                    }
                }
            }
            foreach (var it in updatedDevices)
            {
                //Get mapped device groups.
                ListDeviceGroups request = new ListDeviceGroups()
                {
                    Filter = new GXDeviceGroup() { Default = true }
                };
                deviceGroups.Add(it, await _deviceGroupRepository.GetDeviceGroupsByDeviceId(User, it.Id));
                if (!deviceGroups[it].Any())
                {
                    throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                }
            }
            using IDbTransaction transaction = _host.Connection.BeginTransaction();
            try
            {
                List<GXKeyManagement> keys = new List<GXKeyManagement>();
                foreach (GXDevice device in devices)
                {
                    if (string.IsNullOrEmpty(device.Name) && (columns == null || ServerHelpers.Contains(columns, nameof(GXDevice.Name))))
                    {
                        throw new ArgumentException(Properties.Resources.InvalidName);
                    }
                    bool newDevice = device.Id == Guid.Empty;
                    if (newDevice && (device.Template == null || device.Template.Id == Guid.Empty))
                    {
                        throw new ArgumentException("Invalid device template identifier.");
                    }
                    GXKeyManagement[]? tmp = null;
                    //Get device keys and save them to key manager table.
                    if (!string.IsNullOrEmpty(device.Settings))
                    {
                        var s = JsonSerializer.Deserialize<Shared.DTOs.GXDLMSSettings>(device.Settings);
                        if (s?.HexPassword != null ||
                            !string.IsNullOrEmpty(s?.Password) ||
                            !string.IsNullOrEmpty(s?.BlockCipherKey) ||
                            !string.IsNullOrEmpty(s?.AuthenticationKey))
                        {
                            if (device.Id != Guid.Empty)
                            {
                                //There is no need to get the managemenet keys when a new device is added.
                                ListKeyManagements req = new ListKeyManagements()
                                {
                                    Filter = new GXKeyManagement()
                                    {
                                        Device = device
                                    }
                                };
                                tmp = await _keyManagementRepository.ListAsync(User, req, null, cancellationToken);
                            }
                            if (tmp?.FirstOrDefault() == null)
                            {
                                //Check that device system title is not assigned for other meter.
                                if (!string.IsNullOrEmpty(s.DeviceSystemTitle))
                                {
                                    ListKeyManagements req = new ListKeyManagements()
                                    {
                                        Filter = new GXKeyManagement()
                                        {
                                            SystemTitle = s.DeviceSystemTitle,
                                        }
                                    };
                                    tmp = await _keyManagementRepository.ListAsync(User, req, null, cancellationToken);
                                    //Check that system title is not used yet.
                                    foreach (var it in tmp)
                                    {
                                        if (!string.IsNullOrEmpty(it.SystemTitle))
                                        {
                                            throw new ArgumentException(string.Format(Properties.Resources.SystemTitleIsAlreadyInUse,
                                                s.DeviceSystemTitle));
                                        }
                                    }
                                }
                                tmp = new GXKeyManagement[] { new GXKeyManagement(
                                    s.DeviceSystemTitle)
                                {
                                    Name = device.Name,
                                    Device = device
                                }
                                };
                            }
                            else
                            {
                                //Read all key management information.
                                tmp = new GXKeyManagement[] { await _keyManagementRepository.ReadAsync(User, tmp[0].Id) };
                            }
                            if (s?.HexPassword != null || !string.IsNullOrEmpty(s?.Password))
                            {
                                if (s.Authentication == (byte)Enums.Authentication.Low)
                                {
                                    UpdateKeys(tmp[0], KeyManagementType.LLSPassword, s.Password, s.HexPassword);
                                }
                                else if (s.Authentication == (byte)Enums.Authentication.High)
                                {
                                    UpdateKeys(tmp[0], KeyManagementType.HLSPassword, s.Password, s.HexPassword);
                                }
                                s.HexPassword = null;
                                s.Password = null;
                            }
                            if (!string.IsNullOrEmpty(s?.BlockCipherKey))
                            {
                                byte[] tmp2 = GXDLMSTranslator.HexToBytes(s.BlockCipherKey);
                                if (s.SecuritySuite == 2)
                                {
                                    if (tmp2.Length != 32)
                                    {
                                        throw new Exception("Invalid block cipher key.");
                                    }
                                }
                                else if (tmp2.Length != 16)
                                {
                                    throw new Exception("Invalid block cipher key.");
                                }
                                UpdateKeys(tmp[0], KeyManagementType.BlockCipher, null, tmp2);
                                s.BlockCipherKey = null;
                            }
                            if (!string.IsNullOrEmpty(s?.AuthenticationKey))
                            {
                                byte[] tmp2 = GXDLMSTranslator.HexToBytes(s.AuthenticationKey);
                                if (s.SecuritySuite == 2)
                                {
                                    if (tmp2.Length != 32)
                                    {
                                        throw new Exception("Invalid authentication key.");
                                    }
                                }
                                else if (tmp2.Length != 16)
                                {
                                    throw new Exception("Invalid authentication key.");
                                }
                                UpdateKeys(tmp[0], KeyManagementType.Authentication, null, tmp2);
                                s.AuthenticationKey = null;
                            }
                            keys.Add(tmp[0]);
                            device.Settings = JsonSerializer.Serialize(s);
                        }
                    }
                }

                if (newDevices.Any())
                {
                    if (lateBinding)
                    {
                        foreach (var device in newDevices)
                        {
                            if (device.TraceLevel == null)
                            {
                                device.TraceLevel = TraceLevel.Off;
                            }
                            device.Creator = creator;
                            device.CreationTime = now;
                        }
                        GXInsertArgs args = GXInsertArgs.InsertRange(newDevices);
                        args.Exclude<GXDevice>(e => new
                        {
                            e.Objects,
                            e.Updated,
                            e.Removed,
                        });
                        await _host.Connection.InsertAsync(transaction, args);
                        updated = newDevices.Select(s => s.Id).ToList();
                    }
                    else
                    {
                        foreach (GXDevice device in newDevices)
                        {
                            //Add new device.
                            if (device.TraceLevel == null)
                            {
                                device.TraceLevel = TraceLevel.Off;
                            }
                            device.Type = device.Template.Type;
                            GXInsertArgs args = GXInsertArgs.Insert(device);
                            args.Exclude<GXDevice>(q => new
                            {
                                q.Objects,
                                q.DeviceGroups,
                                q.Updated,
                                q.Parameters
                            });
                            device.Creator = creator;
                            device.CreationTime = now;
                            await _host.Connection.InsertAsync(transaction, args, cancellationToken);
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXObjectTemplate>(q => q.Removed == null);
                            arg.Distinct = true;
                            arg.Columns.Add<GXAttributeTemplate>();
                            arg.Joins.AddLeftJoin<GXObjectTemplate, GXAttributeTemplate>(o => o.Id, a => a.ObjectTemplate);
                            arg.Joins.AddLeftJoin<GXObjectTemplate, GXDeviceTemplate>(o => o.DeviceTemplate, a => a.Id);
                            arg.Where.And<GXAttributeTemplate>(q => q.Removed == null);
                            arg.Where.And<GXDeviceTemplate>(q => q.Removed == null && q.Id == device.Template.Id);
                            arg.Where.And<GXAttributeTemplate>(q => q.Removed == null);
                            List<GXObjectTemplate> l = await _host.Connection.SelectAsync<GXObjectTemplate>(arg, cancellationToken);
                            foreach (GXObjectTemplate it in l)
                            {
                                GXObject obj = new GXObject(it)
                                {
                                    CreationTime = now,
                                    Device = device,
                                };
                                await _host.Connection.InsertAsync(transaction, GXInsertArgs.Insert(obj), cancellationToken);
                                foreach (GXAttributeTemplate ait in it.Attributes)
                                {
                                    GXAttribute a = new GXAttribute();
                                    a.Object = obj;
                                    a.Template = ait;
                                    a.CreationTime = now;
                                    a.ExpirationTime = ait.ExpirationTime;
                                    obj.Attributes.Add(a);
                                };
                                //Update object values e.g. logical device name.
                                if (device.Objects != null)
                                {
                                    foreach (var obj2 in device.Objects)
                                    {
                                        if (obj?.Template?.LogicalName == obj2?.Template?.LogicalName &&
                                            obj2?.Attributes != null)
                                        {
                                            foreach (var att in obj2.Attributes)
                                            {
                                                if (!string.IsNullOrEmpty(att.Value))
                                                {
                                                    if (att.Template == null)
                                                    {
                                                        throw new ArgumentException("Template is missing.");
                                                    }
                                                    if (att.Template.Id == Guid.Empty)
                                                    {
                                                        if (lateBinding)
                                                        {
                                                            obj2.CreationTime = now;
                                                            obj2.Device = device;
                                                            await _host.Connection.InsertAsync(transaction, GXInsertArgs.Insert(obj), cancellationToken);
                                                        }
                                                        else
                                                        {
                                                            //Get object by logical name.
                                                            int ot = obj2.Template.ObjectType.Value;
                                                            string ln = obj2.Template.LogicalName;
                                                            arg = GXSelectArgs.SelectAll<GXObject>();
                                                            arg.Columns.Add<GXAttribute>();
                                                            arg.Columns.Add<GXAttributeTemplate>();
                                                            arg.Joins.AddInnerJoin<GXObject, GXDevice>(j => j.Device, j => j.Id);
                                                            arg.Joins.AddInnerJoin<GXObject, GXObjectTemplate>(j => j.Template, j => j.Id);
                                                            arg.Joins.AddInnerJoin<GXObject, GXAttribute>(j => j.Id, j => j.Object);
                                                            arg.Joins.AddInnerJoin<GXAttribute, GXAttributeTemplate>(j => j.Template, j => j.Id);
                                                            arg.Where.And<GXObjectTemplate>(w => w.ObjectType == ot && w.LogicalName == ln);
                                                            arg.Where.And<GXDevice>(w => w.Id == device.Id);
                                                            GXObject tmp = await _host.Connection.SingleOrDefaultAsync<GXObject>(transaction, arg);
                                                            var tmp2 = tmp?.Attributes?.Where(w => w.Template.Index == att.Template.Index).SingleOrDefault();
                                                            if (tmp2?.Template != null)
                                                            {
                                                                //Template is null if object is added.
                                                                att.Template = tmp2.Template;
                                                            }
                                                        }
                                                    }
                                                    var tmp3 = obj.Attributes.Where(w => w.Template.Index == att.Template.Index).SingleOrDefault();
                                                    if (tmp3 != null)
                                                    {
                                                        tmp3.Value = att.Value;
                                                    }
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }

                                await _host.Connection.InsertAsync(transaction,
                                    GXInsertArgs.InsertRange(obj.Attributes),
                                    cancellationToken);
                            }
                            //Add device parameters.
                            await AddDeviceParameters(transaction, device, device.Parameters, cancellationToken);
                            //Add device to the default device group.
                            await AddDeviceToDeviceGroups(transaction, device.Id, device.DeviceGroups, cancellationToken);
                            updated.Add(device.Id);
                        }
                    }
                }
                foreach (GXDevice device in updatedDevices)
                {
                    if ((device.DeviceGroups == null || !device.DeviceGroups.Any()) &&
                        (columns == null || ServerHelpers.Contains(columns, nameof(GXDevice.DeviceGroups))))
                    {
                        throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                    }
                    device.Updated = DateTime.Now;
                    GXUpdateArgs args = GXUpdateArgs.Update(device, columns);
                    args.Exclude<GXDevice>(q => new
                    {
                        q.Objects,
                        q.CreationTime,
                        q.DeviceGroups,
                        device.Type,
                        q.Parameters,
                        q.Template,
                        q.Creator
                    });
                    _host.Connection.Update(transaction, args);
                    //Update objects if exists.
                    if (device.Objects != null)
                    {
                        GXAttribute a = new GXAttribute();
                        foreach (var obj in device.Objects)
                        {
                            if (obj.Attributes != null)
                            {
                                await _attributeRepository.UpdateAsync(User,
                                    obj.Attributes,
                                    c => a.Value);
                            }
                        }
                    }
                    {
                        //Update device parameters.
                        if (device.Parameters == null)
                        {
                            if (ServerHelpers.Contains(columns, nameof(GXDevice.Parameters)))
                            {
                                throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                            }
                        }
                        else
                        {
                            GXSelectArgs arg = GXSelectArgs.SelectAll<GXDeviceParameter>(w => w.Device == device && w.Removed == null);
                            var deviceParameters = await _host.Connection.SelectAsync<GXDeviceParameter>(arg);
                            var comparer = new UniqueComparer<GXDeviceParameter, Guid>();
                            List<GXDeviceParameter> removedDeviceParameters = deviceParameters.Except(device.Parameters, comparer).ToList();
                            List<GXDeviceParameter> addedDeviceGroupParameters = device.Parameters.Except(deviceParameters, comparer).ToList();
                            List<GXDeviceParameter> updatedDeviceGroupParameters = device.Parameters.Union(deviceParameters, comparer).ToList();
                            if (removedDeviceParameters.Any())
                            {
                                RemoveDeviceParameters(transaction, device, removedDeviceParameters);
                            }
                            if (addedDeviceGroupParameters.Any())
                            {
                                await AddDeviceParameters(transaction, device, addedDeviceGroupParameters, cancellationToken);
                            }
                            if (updatedDeviceGroupParameters.Any())
                            {
                                foreach (var it in updatedDeviceGroupParameters)
                                {
                                    GXUpdateArgs u = GXUpdateArgs.Update(it, c => new { c.Settings, c.Updated });
                                    await _host.Connection.UpdateAsync(u);
                                }
                            }
                        }
                    }
                    //Map device groups to device.
                    {
                        var comparer = new UniqueComparer<GXDeviceGroup, Guid>();
                        if (device.DeviceGroups == null)
                        {
                            if (ServerHelpers.Contains(columns, nameof(GXDevice.DeviceGroups)))
                            {
                                throw new ArgumentNullException(Properties.Resources.ArrayIsEmpty);
                            }
                        }
                        else
                        {
                            List<GXDeviceGroup> removedDeviceGroups = deviceGroups[device].Except(device.DeviceGroups, comparer).ToList();
                            List<GXDeviceGroup> addedDeviceGroups = device.DeviceGroups.Except(deviceGroups[device], comparer).ToList();
                            if (removedDeviceGroups.Any())
                            {
                                RemoveDevicesFromDeviceGroup(transaction, device.Id, removedDeviceGroups);
                            }
                            if (addedDeviceGroups.Any())
                            {
                                await AddDeviceToDeviceGroups(transaction, device.Id, addedDeviceGroups, cancellationToken);
                            }
                        }
                    }
                }
                _host.Connection.CommitTransaction(transaction);
                if (keys != null)
                {
                    //Management keys must update after the devices are added for the DB.
                    await _keyManagementRepository.UpdateAsync(User, keys);
                }
            }
            catch (Exception)
            {
                _host.Connection.RollbackTransaction(transaction);
                throw;
            }
            Dictionary<GXDevice, List<string>> updates = new Dictionary<GXDevice, List<string>>();
            if (newDevices.Any())
            {
                var first = newDevices.First();
                var users = await GetUsersAsync(User, first.Id);
                foreach (var it in newDevices)
                {
                    updates[it] = users;
                }
            }
            foreach (var it in updatedDevices)
            {
                var users = await GetUsersAsync(User, it.Id);
                updates[it] = users;
            }
            foreach (var it in updates)
            {
                await _eventsNotifier.DeviceUpdate(it.Value, new GXDevice[] { new GXDevice() { Id = it.Key.Id, Name = it.Key.Name } });
            }
            return updated.ToArray();
        }

        /// <summary>
        /// Map device template to device groups.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateId">Device ID.</param>
        /// <param name="groups">Device groups where the device is added.</param>
        public Task AddDeviceToDeviceGroups(IDbTransaction transaction,
            Guid deviceTemplateId,
            IEnumerable<GXDeviceGroup> groups,
            CancellationToken cancellationToken)
        {
            DateTime now = DateTime.Now;
            List<GXDeviceGroupDevice> list = new List<GXDeviceGroupDevice>();
            foreach (GXDeviceGroup it in groups)
            {
                list.Add(new GXDeviceGroupDevice()
                {
                    DeviceId = deviceTemplateId,
                    DeviceGroupId = it.Id,
                    CreationTime = now
                });
            }
            return _host.Connection.InsertAsync(transaction, GXInsertArgs.InsertRange(list), cancellationToken);
        }

        /// <summary>
        /// Remove map between device group and device.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="deviceTemplateId">Device template ID.</param>
        /// <param name="groups">Device template groups where the device template is removed.</param>
        public void RemoveDevicesFromDeviceGroup(IDbTransaction transaction, Guid deviceTemplateId, IEnumerable<GXDeviceGroup> groups)
        {
            foreach (var it in groups)
            {
                _host.Connection.Delete(transaction, GXDeleteArgs.Delete<GXDeviceGroupDevice>(w => w.DeviceId == deviceTemplateId && w.DeviceGroupId == it.Id));
            }
        }

        /// <summary>
        /// Add device parameters.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="device">Device where parameters are added.</param>
        /// <param name="parameters">Added device parameters.</param>
        public Task AddDeviceParameters(IDbTransaction transaction,
            GXDevice device,
            IEnumerable<GXDeviceParameter>? parameters,
            CancellationToken cancellationToken)
        {
            if (parameters != null)
            {
                DateTime now = DateTime.Now;
                foreach (GXDeviceParameter it in parameters)
                {
                    it.CreationTime = now;
                    it.Device = device;
                }
                return _host.Connection.InsertAsync(transaction, GXInsertArgs.InsertRange(parameters), cancellationToken);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Remove device parameters from the device.
        /// </summary>
        /// <param name="transaction">Transaction.</param>
        /// <param name="device">Device where parameters are removed.</param>
        /// <param name="parameters">Removed device parameters.</param>
        public void RemoveDeviceParameters(IDbTransaction transaction, GXDevice device, IEnumerable<GXDeviceParameter> parameters)
        {
            foreach (GXDeviceParameter it in parameters)
            {
                _host.Connection.Delete(transaction, GXDeleteArgs.DeleteById<GXDeviceParameter>(it.Id));
            }
            //_host.Connection.Delete(GXDeleteArgs.DeleteRange(parameters));
        }

        /// <inheritdoc />
        public async Task UpdateStatusAsync(ClaimsPrincipal User, Guid deviceId, DeviceStatus status)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXDevice>(s => new { s.Id, s.Name },
                where => where.Id == deviceId && where.Removed == null);
            GXDevice device = await _host.Connection.SingleOrDefaultAsync<GXDevice>(args);
            if (device == null)
            {
                throw new GXAmiNotFoundException(Properties.Resources.Device + " " +
                    Properties.Resources.Id + " " + deviceId.ToString());
            }
            device.Status = status;
            device.Detected = DateTime.Now;
            GXUpdateArgs update;
            update = GXUpdateArgs.Update(device, c => new { c.Status, c.Detected });
            await _host.Connection.UpdateAsync(update);
            //Only part of the device properties are send.
            GXDevice tmp = new GXDevice()
            {
                Id = device.Id,
                Name = device.Name,
                Detected = device.Detected,
                Status = device.Status
            };
            if (_performanceSettings.Notify(TargetType.Device))
            {
                await _eventsNotifier.DeviceStatusChange(await GetUsersAsync(User, device.Id), new GXDevice[] { tmp });
            }
            //Update device state for the device log.
            GXDeviceError? log = new GXDeviceError(TraceLevel.Info);
            log.Device = device;
            switch (status)
            {
                case DeviceStatus.Connected:
                    log.Message = Properties.Resources.Connected;
                    break;
                case DeviceStatus.Disconnected:
                    log.Message = Properties.Resources.Disconnected;
                    break;
                case DeviceStatus.Error:
                    log.Message = Properties.Resources.Error;
                    break;
                default:
                    log = null;
                    break;
            }
            if (log != null)
            {
                //Add device log. Idle status is not logged.
                IDeviceErrorRepository repository = _serviceProvider.GetRequiredService<IDeviceErrorRepository>();
                await repository.AddAsync(User, new GXDeviceError[] { log });
            }
        }

        /// <inheritdoc />
        public Task ResetAsync(ClaimsPrincipal user, IEnumerable<Guid> devices)
        {
            throw new NotImplementedException();
        }
    }
}