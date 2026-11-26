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

using Gurux.DLMS.AMI.Shared.DTOs.Log;

using Gurux.Common;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using DeviceSettings = Gurux.DLMS.AMI.Shared.DTOs.Device.GXDLMSSettings;
using Gurux.DLMS.AMI.Shared.Rest;


using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Secure;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Gurux.DLMS.DeviceAccess;
using SharedReader = Gurux.DLMS.DeviceAccess.GXDLMSReader;
using Task = System.Threading.Tasks.Task;

namespace Gurux.DLMS.AMI.Agent.Worker
{
    /// <summary>Executes device protocol exchanges using the shared device-access reader.</summary>
    internal class GXDLMSReader
    {
        /// <summary>
        /// Sends AMI action and packet-trace records to the server.
        /// </summary>
        private readonly HttpClient _http;
        /// <summary>Shared device-access session used for protocol operations.</summary>
        private readonly SharedReader _session;
        /// <summary>Gets or sets cancellation for device protocol exchanges.</summary>
        internal CancellationToken CancellationToken { get; set; }
        /// <summary>Maximum device response wait in milliseconds.</summary>
        private readonly int _waitTime;
        /// <summary>
        /// Retry count.
        /// </summary>
        private readonly int _retryCount = 3;
        /// <summary>Transport used to exchange bytes with the connected device.</summary>
        private IGXMedia? _media;
        /// <summary>Gets whether console or persisted device packet tracing is enabled.</summary>
        internal bool PacketTracingEnabled => _consoleTrace != TraceLevel.Off || _deviceTrace != TraceLevel.Off;
        /// <summary>Minimum trace level for console diagnostics.</summary>
        readonly TraceLevel _consoleTrace;
        /// <summary>Minimum trace level for persisted device diagnostics.</summary>
        readonly TraceLevel _deviceTrace;
        /// <summary>Secure DLMS client configured for the shared device session.</summary>
        private GXDLMSSecureClient? _client;
        /// <summary>Gets the configured secure protocol client for the current device.</summary>
        internal GXDLMSSecureClient Client
        {
            get => _client ?? throw new ObjectDisposedException(nameof(GXDLMSReader));
            private set => _client = value;
        }
        /// <summary>Records runtime diagnostics without exposing authentication credentials.</summary>
        private readonly ILogger? _logger;
        /// <summary>Device whose protocol operations and diagnostics are being processed.</summary>
        private readonly GXDevice? _device;
        /// <summary>Applies the template's time-zone convention and outgoing date/time field exclusions.</summary>
        /// <param name="client">Configured communication client used by the operation.</param>
        /// <param name="settings">Device protocol and authentication settings.</param>
        internal static void ApplyDateTimeSettings(GXDLMSClient client, DeviceSettings settings)
        {
            client.UseUtc2NormalTime = settings.UtcTimeZone;
            client.DateTimeSkips = (DateTimeSkips)settings.DateTimeSkips;
        }

        // The server authorizes and fills these values when the device is fetched.
        /// <summary>Copies configured security titles and keys into the active secure protocol client.</summary>
        /// <param name="client">Configured communication client used by the operation.</param>
        /// <param name="settings">Device protocol and authentication settings.</param>
        internal static void ApplyDeviceKeys(GXDLMSSecureClient client, DeviceSettings settings)
        {
            client.Password = settings.Password?.ToArray();
            client.Ciphering.BlockCipherKey = settings.BlockCipherKey is { Length: > 0 } ? [.. settings.BlockCipherKey] : null;
            client.Ciphering.AuthenticationKey = settings.AuthenticationKey is { Length: > 0 } ? [.. settings.AuthenticationKey] : null;
            client.Ciphering.DedicatedKey = null;
        }
        /// <summary>
        /// Creates an AMI adapter over the shared device-access session.
        /// </summary>
        /// <param name="client">DLMS Client.</param>
        /// <param name="media">Media.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="consoleTrace">Console Trace level.</param>
        /// <param name="deviceTrace">Device Trace level.</param>
        /// <param name="wt">Reply wait time in seconds.</param>
        /// <param name="retry">Number of receive attempts.</param>
        /// <param name="device">Device whose actions are logged.</param>
        /// <param name="http">HTTP client owned by this worker or its host.</param>
        public GXDLMSReader(
            GXDLMSSecureClient client,
            IGXMedia media,
            ILogger? logger,
            TraceLevel consoleTrace,
            TraceLevel deviceTrace,
            int wt,
            int retry,
            GXDevice? device, HttpClient http)
        {
            this._http = http;
            ArgumentOutOfRangeException.ThrowIfNegative(wt);
            // A missing DTO timeout selects the shared session default.
            _waitTime = wt == 0 ? 5000 : checked((int)TimeSpan.FromSeconds(wt).TotalMilliseconds);
            _retryCount = retry;
            _consoleTrace = consoleTrace;
            _deviceTrace = deviceTrace;
            _media = media;
            Client = client;
            _logger = logger;
            _device = device;
            _session = new SharedReader(client, media as IGXAsyncMedia ?? throw new ArgumentException("The media must support asynchronous communication.", nameof(media)), new GXDLMSReaderOptions
            {
                WaitTime = _waitTime,
                RetryCount = _retryCount,
                CheckAccessRights = false,
                TraceLevel = TraceLevel.Off
            })
            {
                OnPacketTrace = packet => WriteTrace(packet.Sent, GXCommon.ToHex(packet.Data, true)).GetAwaiter().GetResult()
            };
        }

        /// <summary>
        /// Initialize the shared meter session without fetching an association view.
        /// </summary>
        /// <param name="preEstablished">Whether the device association is already established.</param>
        /// <param name="ignoreSNRMWithPreEstablished">Whether link negotiation is skipped for a pre-established association.</param>
        /// <param name="invocationCounter">Invocation counter object used to synchronize secure message numbering.</param>
        /// <returns>A task completing when the device association has been established.</returns>
        public Task InitializeConnection(bool preEstablished, bool ignoreSNRMWithPreEstablished, string? invocationCounter)
        {
            if (!preEstablished)
            {
                Client.ServerSystemTitle = null;
            }
            else if (!Client.PreEstablishedConnection)
            {
                throw new InvalidOperationException("Pre-established associations require a device system title.");
            }
            return _session.ConnectAsync(new GXDLMSConnectOptions
            {
                WaitTime = _waitTime,
                RetryCount = _retryCount,
                InvocationCounter = invocationCounter,
                IgnoreSNRMWithPreEstablished = ignoreSNRMWithPreEstablished,
                ReadAssociationView = false
            }, CancellationToken);
        }

        /// <summary>
        /// This method is used to update meter firmware.
        /// </summary>
        /// <param name="target">Image transfer object accepting the firmware image.</param>
        /// <param name="identification">Firmware image identifier.</param>
        /// <param name="data">Firmware image bytes.</param>
        public void ImageUpdate(GXDLMSImageTransfer target, byte[] identification, byte[] data)
        {
            _session.ImageUpdate(target, identification, data, cancellationToken: CancellationToken);
        }

        /// <summary>
        /// Read association view.
        /// </summary>
        /// <param name="useCache">Whether previously read scaler and unit values may be reused.</param>
        /// <exception cref="GXDLMSException">The meter rejects the requested operation.</exception>
        /// <exception cref="OperationCanceledException">The device task is cancelled.</exception>
        public void GetAssociationView(bool useCache)
        {
            // No file cache was used by the agent; always refresh the shared collection.
            _ = useCache;
            _session.EnsureAssociationView(forceRefresh: true, cancellationToken: CancellationToken);
        }

        /// <summary>
        /// Read attribute value.
        /// </summary>
        /// <param name="it">COSEM object to read.</param>
        /// <param name="attributeIndex">Attribute index.</param>
        /// <returns>Read value.</returns>
        /// <remarks>Scalar reads use the shared attribute operation; composite values retain their typed XML representation.</remarks>
        /// <exception cref="GXDLMSException">The meter rejects the requested operation.</exception>
        /// <exception cref="OperationCanceledException">The device task is cancelled.</exception>
        public async Task<object> Read(GXDLMSObject it, int attributeIndex)
        {
            if (it is GXDLMSProfileGeneric && attributeIndex == 2 && _logger?.IsEnabled(LogLevel.Information) == true)
            {
                _logger.LogInformation("Reading Profile Generic: {LogicalName}, Buffer (Read attribute 2)", it.LogicalName);
            }
            string type = "Read";
            GXLog? a = null;
            if (_device != null)
            {
                a = new GXLog
                {
                    Creator = _device.Id
                };
                string[] names = ((IGXDLMSBase)it).GetNames();
                if (attributeIndex < names.Length)
                {
                    a.Data = it.LogicalName + ": " + names[attributeIndex - 1];
                }
                else
                {
                    a.Data = it.LogicalName + ": " + attributeIndex;
                }
            }
            try
            {
                DataType dataType = it.GetDataType(attributeIndex);
                object? value;
                if (dataType != DataType.None && dataType != DataType.Array && dataType != DataType.Structure)
                {
                    value = await _session.ReadAsync(it, attributeIndex, cancellationToken: CancellationToken);
                }
                else
                {
                    // AMI persists typed wire XML, not the object's display value.
                    // Exchange creates requests under the shared session gate and owns all I/O.
                    GXReplyData reply = await _session.ExchangeAsync(
                        client => client.Read(it, attributeIndex), CancellationToken);
                    if (dataType == DataType.None)
                    {
                        it.SetDataType(attributeIndex, reply.DataType);
                        dataType = reply.DataType;
                    }
                    if (dataType == DataType.Array || dataType == DataType.Structure ||
                        reply.Value is GXStructure || reply.Value is GXArray)
                    {
                        if (it is GXDLMSProfileGeneric && attributeIndex == 2 && _logger?.IsEnabled(LogLevel.Information) == true)
                        {
                            return reply.Value;
                        }
                        if (reply.Value == null)
                        {
                            return "";
                        }
                        string xml = GXDLMSTranslator.ValueToXml(reply.Value);
                        a?.Reply = xml;
                        return xml;
                    }
                    value = Client.UpdateValue(it, attributeIndex, reply.Value);
                }
                if (a != null)
                {
                    if (value is byte[] b)
                    {
                        a.Reply = GXCommon.ToHex(b);
                    }
                    else
                    {
                        a.Reply = Convert.ToString(value);
                    }
                }
                return value!;
            }
            catch (Exception ex)
            {
                if (a != null)
                {
                    type = "Read, Error";
                    a.Reply = ex.Message;
                }
                throw;
            }
            finally
            {
                if (_device != null && _device.TraceLevel > TraceLevel.Warning && a != null)
                {
                    //Send device action information if trace level is Info or Verbose.
                    AddLog log = new()
                    {
                        Logs = [a],
                        Type = type
                    };
                    await _http.PostAsJson<AddLogResponse>("/api/Log/Add?source=device-action", log, CancellationToken);
                }
            }
        }


        /// <summary>
        /// Read list of attributes.
        /// </summary>
        /// <param name="list">Protocol objects or attributes included in the batch operation.</param>
        /// <exception cref="GXDLMSException">The meter rejects the requested operation.</exception>
        /// <exception cref="OperationCanceledException">The device task is cancelled.</exception>
        public void ReadList(List<KeyValuePair<GXDLMSObject, int>> list)
        {
            _session.ReadList(list, cancellationToken: CancellationToken);
        }

        /// <summary>
        /// Write attribute value.
        /// </summary>
        /// <param name="it">Protocol object or profile whose data is requested.</param>
        /// <param name="attributeIndex">Attribute index on the target protocol object.</param>
        /// <exception cref="GXDLMSException">The meter rejects the requested operation.</exception>
        /// <exception cref="OperationCanceledException">The device task is cancelled.</exception>
        public void Write(GXDLMSObject it, int attributeIndex)
        {
            _session.Write(it, attributeIndex, cancellationToken: CancellationToken);
        }

        /// <summary>
        /// Method attribute value.
        /// </summary>
        /// <param name="it">Protocol object or profile whose data is requested.</param>
        /// <param name="attributeIndex">Attribute index on the target protocol object.</param>
        /// <param name="value">New configuration or state value to publish.</param>
        /// <param name="type">Protocol data type or notification category processed by this operation.</param>
        /// <exception cref="GXDLMSException">The meter rejects the requested operation.</exception>
        /// <exception cref="OperationCanceledException">The device task is cancelled.</exception>
        public void Method(GXDLMSObject it, int attributeIndex, object value, DataType type)
        {
            _session.Method(it, attributeIndex, value, type, cancellationToken: CancellationToken);
        }

        /// <summary>
        /// Read Profile Generic Columns by entry.
        /// </summary>
        /// <param name="it">Protocol object or profile whose data is requested.</param>
        /// <param name="index">The one-based first buffer entry.</param>
        /// <param name="count">Maximum number of profile entries to retrieve.</param>
        /// <returns>Decoded profile rows within the requested entry range.</returns>
        /// <remarks>Uses the shared raw exchange to preserve GXArray rows and unconverted octets required by AMI history.</remarks>
        /// <exception cref="GXDLMSException">The meter rejects the requested operation.</exception>
        /// <exception cref="OperationCanceledException">The device task is cancelled.</exception>
        public object ReadRowsByEntry(GXDLMSProfileGeneric it, UInt32 index, UInt32 count)
        {
            if (_logger?.IsEnabled(LogLevel.Information) == true)
            {
                _logger.LogInformation("Reading Profile Generic: {LogicalName}, Entry (index {Index}, count {Count})",
                    it.LogicalName, index, count);
            }
            return _session.Exchange(client => client.ReadRowsByEntry(it, index, count), CancellationToken).Value;
        }

        /// <summary>Reads only the selected Clock time column for a bounded range of profile entries.</summary>
        /// <param name="profile">The profile whose captured timestamps are inspected.</param>
        /// <param name="index">The one-based first buffer entry.</param>
        /// <param name="count">The maximum number of entries to retrieve.</param>
        /// <param name="clockColumn">The zero-based captured Clock time column.</param>
        /// <returns>Wire rows containing the selected timestamp column.</returns>
        /// <exception cref="GXDLMSException">The meter rejects entry or column selection.</exception>
        /// <exception cref="OperationCanceledException">The device task is cancelled.</exception>
        internal object ReadClockRowsByEntry(GXDLMSProfileGeneric profile, uint index, uint count, int clockColumn)
        {
            if (_logger?.IsEnabled(LogLevel.Information) == true)
            {
                _logger.LogInformation("Reading Profile Generic: {LogicalName}, Entry (index {Index}, count {Count}, clock column {Column})",
                    profile.LogicalName, index, count, clockColumn + 1);
            }
            return _session.Exchange(client => client.ReadRowsByEntry(profile, index, count, clockColumn + 1, clockColumn + 1), CancellationToken).Value;
        }

        /// <summary>
        /// Read Profile Generic Columns by range.
        /// </summary>
        /// <param name="it">Protocol object or profile whose data is requested.</param>
        /// <param name="start">Beginning of the requested profile history interval.</param>
        /// <param name="end">End of the requested profile history interval.</param>
        /// <returns>Decoded profile rows within the requested time range using the resolved captured clock.</returns>
        /// <remarks>Logs the Range selection and requested interval in the local time zone with second precision and preserves typed wire rows instead of converting them into the shared reader's profile buffer.</remarks>
        /// <exception cref="GXDLMSException">The meter rejects the requested operation.</exception>
        /// <exception cref="OperationCanceledException">The device task is cancelled.</exception>
        public object ReadRowsByRange(GXDLMSProfileGeneric it, DateTime start, DateTime end)
        {
            if (_logger?.IsEnabled(LogLevel.Information) == true)
            {
                _logger.LogInformation("Reading Profile Generic: {LogicalName}, Range [{Start:yyyy-MM-dd HH:mm:ss zzz} - {End:yyyy-MM-dd HH:mm:ss zzz}]",
                    it.LogicalName, start.ToLocalTime(), end.ToLocalTime());
            }
            return _session.Exchange(client => GXProfileRangeRequest.Create(client, it, start, end), CancellationToken).Value;
        }

        /// <summary>Opens a fresh association to prove the new LLS password before server activation.</summary>
        /// <param name="password">Authentication secret used to establish the replacement association.</param>
        /// <returns>A task completing when the device association using the supplied password is established.</returns>
        internal async Task ReconnectWithPasswordAsync(byte[] password)
        {
            if (_consoleTrace != TraceLevel.Off || _deviceTrace != TraceLevel.Off)
            {
                throw new InvalidOperationException("Password changes require packet tracing to be disabled.");
            }
            CancellationToken.ThrowIfCancellationRequested();
            Disconnect();
            Client.Password = [.. password];
            await InitializeConnection(false, false, null);
        }

        /// <summary>Disconnects the association and optionally closes the transport.</summary>
        /// <param name="closeConnection">Whether the transport must also be closed after releasing the association.</param>
        public void Disconnect(bool closeConnection = true)
        {
            if (_media != null && _client != null)
            {
                try
                {
                    if (closeConnection)
                    {
                        _session.Close(CancellationToken);
                    }
                    else
                    {
                        _session.Disconnect(CancellationToken);
                    }
                }
                catch (Exception error)
                {
                    _logger?.LogDebug(error, "Disconnecting from the meter failed.");
                }
            }
        }

        /// <summary>
        /// Release connection to the meter.
        /// </summary>
        public void Release()
        {
            if (_media != null && _client != null)
            {
                try
                {
                    _session.Release(CancellationToken);
                }
                catch (Exception error)
                {
                    _logger?.LogDebug(error, "Releasing the meter association failed.");
                }
            }
        }

        /// <summary>
        /// Close connection to the meter.
        /// </summary>
        public void Close()
        {
            if (_media == null)
            {
                return;
            }
            try
            {
                _session.Close(CancellationToken);
            }
            catch (Exception error)
            {
                _logger?.LogDebug(error, "Disconnecting from the meter failed.");
            }
            finally
            {
                try
                {
                    _session.Dispose();
                }
                finally
                {
                    _media = null;
                    _client = null;
                }
            }
        }
        /// <summary>
        /// Write trace.
        /// </summary>
        /// <param name="frame">Data frame as a hex string.</param>
        /// <param name="tx">True for transmitted frames, false for received frames.</param>
        /// <returns>A task completing after the trace data has been recorded.</returns>
        async Task WriteTrace(bool tx, string frame)
        {
            if (_consoleTrace > TraceLevel.Info)
            {
                if (tx)
                {
                    if (_logger?.IsEnabled(LogLevel.Information) == true)
                    {
                        _logger.LogInformation("{Message}", "TX:\t" + DateTime.Now.ToString("hh:mm:ss") + "\t" + frame);
                    }
                }
                else
                {
                    if (_logger?.IsEnabled(LogLevel.Information) == true)
                    {
                        _logger.LogInformation("{Message}", "RX:\t" + DateTime.Now.ToString("hh:mm:ss") + "\t" + frame);
                    }
                }
            }
            if (_device != null && _deviceTrace > TraceLevel.Info)
            {
                try
                {
                    GXDeviceTrace trace = new()
                    {
                        Device = new GXDevice() { Id = _device.Id },
                        Frame = frame
                    };
                    AddDeviceTrace it = new()
                    {
                        Traces = [trace]
                    };
                    if (tx)
                    {
                        it.Type = "Send";
                    }
                    else
                    {
                        it.Type = "Receive";
                    }
                    await _http.PostAsJson<AddDeviceTraceResponse>("/api/DeviceTrace/Add", it, CancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    if (_logger?.IsEnabled(LogLevel.Error) == true)
                    {
                        _logger.LogError("{Message}", ex.Message);
                    }
                }
            }
        }
    }
}

