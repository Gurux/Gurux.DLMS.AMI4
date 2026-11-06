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
using Gurux.DLMS.Objects.Enums;
using Gurux.DLMS.Secure;
using Gurux.Net;
using Gurux.Serial;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using Task = System.Threading.Tasks.Task;

namespace Gurux.DLMS.AMI.Agent.Worker
{
    internal class GXDLMSReader
    {
        /// <summary>
        /// Wait time.
        /// </summary>
        private readonly HttpClient http;
        internal CancellationToken CancellationToken { get; set; }
        private readonly int WaitTime;
        /// <summary>
        /// Retry count.
        /// </summary>
        private readonly int RetryCount = 3;
        private IGXMedia? _media;
        private IGXMedia Media
        {
            get => _media ?? throw new ObjectDisposedException(nameof(GXDLMSReader));
            set => _media = value;
        }
        readonly TraceLevel _consoleTrace;
        readonly TraceLevel _deviceTrace;
        private GXDLMSSecureClient? _client;
        internal GXDLMSSecureClient Client
        {
            get => _client ?? throw new ObjectDisposedException(nameof(GXDLMSReader));
            private set => _client = value;
        }
        private readonly ILogger? _logger;
        private readonly GXDevice? _device;
        /// <summary>Applies the template's time-zone convention and outgoing date/time field exclusions.</summary>
        internal static void ApplyDateTimeSettings(GXDLMSClient client, DeviceSettings settings)
        {
            client.UseUtc2NormalTime = settings.UtcTimeZone;
            client.DateTimeSkips = (DateTimeSkips)settings.DateTimeSkips;
        }

        // The server authorizes and fills these values when the device is fetched.
        internal static void ApplyDeviceKeys(GXDLMSSecureClient client, DeviceSettings settings)
        {
            client.Password = settings.Password?.ToArray();
            client.Ciphering.BlockCipherKey = settings.BlockCipherKey is { Length: > 0 } ? settings.BlockCipherKey.ToArray() : null;
            client.Ciphering.AuthenticationKey = settings.AuthenticationKey is { Length: > 0 } ? settings.AuthenticationKey.ToArray() : null;
            client.Ciphering.DedicatedKey = null;
        }
        /// <summary>
        /// Constructor.
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
            this.http = http;
            ArgumentOutOfRangeException.ThrowIfNegative(wt);
            WaitTime = checked((int)TimeSpan.FromSeconds(wt).TotalMilliseconds);
            RetryCount = retry;
            _consoleTrace = consoleTrace;
            _deviceTrace = deviceTrace;
            Media = media;
            Client = client;
            _logger = logger;
            _device = device;
        }

        // Protocol methods are synchronous; slice receives without changing the total configured timeout.
        internal bool Receive<T>(ReceiveParameters<T> parameters)
        {
            int timeout = parameters.WaitTime;
            var elapsed = Stopwatch.StartNew();
            try
            {
                do
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    parameters.WaitTime = timeout <= 0 ? 250 : Math.Min(250, Math.Max(1, timeout - (int)elapsed.ElapsedMilliseconds));
                    bool received = Media.Receive(parameters);
                    CancellationToken.ThrowIfCancellationRequested();
                    if (received)
                        {
                        return true;
                    }
                } while (timeout <= 0 || elapsed.ElapsedMilliseconds < timeout);
                return false;
            }
            catch (Exception) when (CancellationToken.IsCancellationRequested)
            {
                CancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            finally { parameters.WaitTime = timeout; }
        }

        internal void Wait(int milliseconds)
        {
            CancellationToken.ThrowIfCancellationRequested();
            if (CancellationToken.WaitHandle.WaitOne(milliseconds))
                {
                CancellationToken.ThrowIfCancellationRequested();
            }
        }
        /// <summary>
        /// Send IEC disconnect message.
        /// </summary>
        void DiscIEC()
        {
            ReceiveParameters<string?> p = new()
            {
                AllData = false,
                Eop = (byte)0x0A,
                WaitTime = WaitTime
            };
            string data = (char)0x01 + "B0" + (char)0x03 + "\r\n";
            Media.Send(data, null);
            p.Eop = "\n";
            p.AllData = true;
            p.Count = 1;

            Receive(p);
        }

        /// <summary>
        /// Initialize optical head.
        /// </summary>
        void InitializeOpticalHead()
        {
            if (Client.InterfaceType != InterfaceType.HdlcWithModeE)
            {
                return;
            }
            GXSerial? serial = Media as GXSerial;
            byte Terminator = (byte)0x0A;
            Media.Open();
            if (serial != null)
            {
                //Some meters need a little break.
                Wait(1000);
            }
            //Query device information.
            string data = "/?!\r\n";
            if (_consoleTrace > TraceLevel.Info)
            {
                Console.WriteLine("IEC Sending:" + data);
            }
            ReceiveParameters<string?> p = new()
            {
                AllData = false,
                Eop = Terminator,
                WaitTime = WaitTime
            };
            lock (Media.Synchronous)
            {
                Media.Send(data, null);
                if (!Receive(p))
                {
                    //Try to move away from mode E.
                    try
                    {
                        Disconnect();
                    }
                    catch (Exception)
                    {
                    }
                    DiscIEC();
                    string str = "Failed to receive reply from the device in given time.";
                    if (_consoleTrace > TraceLevel.Info)
                    {
                        Console.WriteLine(str);
                    }
                    Media.Send(data, null);
                    if (!Receive(p))
                    {
                        throw new Exception(str);
                    }
                }
                //If echo is used.
                if (p.Reply == data)
                {
                    p.Reply = null;
                    if (!Receive(p))
                    {
                        //Try to move away from mode E.
                        GXReplyData reply = new();
                        Disconnect();
                        if (serial != null)
                        {
                            DiscIEC();
                            serial.DtrEnable = serial.RtsEnable = false;
                            serial.BaudRate = 9600;
                            serial.DtrEnable = serial.RtsEnable = true;
                            DiscIEC();
                        }
                        data = "Failed to receive reply from the device in given time.";
                        if (_consoleTrace > TraceLevel.Info)
                        {
                            Console.WriteLine(data);
                        }
                        throw new Exception(data);
                    }
                }
            }
            if (_consoleTrace > TraceLevel.Info)
            {
                Console.WriteLine("IEC received: " + p.Reply);
            }
            if (string.IsNullOrEmpty(p.Reply))
            {
                throw new Exception("Empty IEC response.");
            }
            int pos = 0;
            //With some meters there might be some extra invalid chars. Remove them.
            while (pos < p.Reply.Length && p.Reply[pos] != '/')
            {
                ++pos;
            }
            if (pos == p.Reply.Length || p.Reply[pos] != '/')
            {
                p.WaitTime = 100;
                Receive(p);
                DiscIEC();
                throw new Exception("Invalid responce.");
            }

            _ = p.Reply.Substring(1 + pos, 3);
            char baudrate = p.Reply[4 + pos];
            int BaudRate = baudrate switch
            {
                '0' => 300,
                '1' => 600,
                '2' => 1200,
                '3' => 2400,
                '4' => 4800,
                '5' => 9600,
                '6' => 19200,
                _ => throw new Exception("Unknown baud rate."),
            };
            if (_consoleTrace > TraceLevel.Info)
            {
                Console.WriteLine(DateTime.Now.ToLongTimeString() + "\tBaudRate is : " + BaudRate.ToString());
            }
            //Send ACK
            //Send Protocol control character
            // "2" HDLC protocol procedure (Mode E)
            byte controlCharacter = (byte)'2';
            //Send Baud rate character
            //Mode control character
            byte ModeControlCharacter = (byte)'2';
            //"2" //(HDLC protocol procedure) (Binary mode)
            //Set mode E.
            byte[] arr = [0x06, controlCharacter, (byte)baudrate, ModeControlCharacter, 13, 10];
            if (_consoleTrace > TraceLevel.Info)
            {
                Console.WriteLine(DateTime.Now.ToLongTimeString() + "\tMoving to mode E.", arr);
            }
            lock (Media.Synchronous)
            {
                p.Reply = null;
                Media.Send(arr, null);
                //Some meters need this sleep. Do not remove.
                Wait(200);
                p.WaitTime = 2000;
                //Note! All meters do not echo this.
                Receive(p);
                if (p.Reply != null)
                {
                    if (_consoleTrace > TraceLevel.Info)
                    {
                        Console.WriteLine("Received: " + p.Reply);
                    }
                }
                if (serial != null)
                {
                    Media.Close();
                    serial.BaudRate = BaudRate;
                    serial.DataBits = 8;
                    serial.Parity = Parity.None;
                    serial.StopBits = StopBits.One;
                    Media.Open();
                }
                //Some meters need this sleep. Do not remove.
                Wait(800);
            }
        }

        /// <summary>
        /// Read Invocation counter (frame counter) from the meter and update it.
        /// </summary>
        private async Task UpdateFrameCounter(string? invocationCounter)
        {
            //Read frame counter if GeneralProtection is used.
            if (!string.IsNullOrEmpty(invocationCounter) && Client.Ciphering != null && Client.Ciphering.Security != Security.None)
            {
                InitializeOpticalHead();
                byte[] data;
                GXReplyData reply = new();
                Client.ProposedConformance |= Conformance.GeneralProtection;
                int add = Client.ClientAddress;
                Authentication auth = Client.Authentication;
                Security security = Client.Ciphering.Security;
                Signing signing = Client.Ciphering.Signing;
                byte[] challenge = Client.CtoSChallenge;
                try
                {
                    Client.ClientAddress = 16;
                    Client.Authentication = Authentication.None;
                    Client.Ciphering.Security = Security.None;
                    Client.Ciphering.Signing = Signing.None;
                    data = Client.SNRMRequest();
                    if (data != null)
                    {
                        if (_consoleTrace > TraceLevel.Info)
                        {
                            Console.WriteLine("Send SNRM request." + GXCommon.ToHex(data, true));
                        }
                        ReadDataBlock(data, reply);
                        if (_consoleTrace == TraceLevel.Verbose)
                        {
                            Console.WriteLine("Parsing UA reply." + reply.ToString());
                        }
                        //Has server accepted client.
                        Client.ParseUAResponse(reply.Data);
                        if (_consoleTrace > TraceLevel.Info)
                        {
                            Console.WriteLine("Parsing UA reply succeeded.");
                        }
                    }
                    //Generate AARQ request.
                    //Split requests to multiple packets if needed.
                    //If password is used all data might not fit to one packet.
                    CancellationToken.ThrowIfCancellationRequested();
                    foreach (byte[] it in Client.AARQRequest())
                    {
                        if (_consoleTrace > TraceLevel.Info)
                        {
                            Console.WriteLine("Send AARQ request {0}", GXCommon.ToHex(it, true));
                        }
                        reply.Clear();
                        ReadDataBlock(it, reply);
                    }
                    if (_consoleTrace > TraceLevel.Info)
                    {
                        Console.WriteLine("Parsing AARE reply" + reply.ToString());
                    }
                    try
                    {
                        //Parse reply.
                        CancellationToken.ThrowIfCancellationRequested();
                        Client.ParseAAREResponse(reply.Data);
                        reply.Clear();
                        GXDLMSData d = new(invocationCounter);
                        await Read(d, 2);
                        Client.Ciphering.InvocationCounter = 1 + Convert.ToUInt32(d.Value);
                        Console.WriteLine("Invocation counter: " + Convert.ToString(Client.Ciphering.InvocationCounter));
                        reply.Clear();
                        Disconnect(false);
                        //Some meters need this sleep. Do not remove.
                        Wait(800);
                    }
                    catch (Exception)
                    {
                        Disconnect();
                        throw;
                    }
                }
                finally
                {
                    Client.ClientAddress = add;
                    Client.Authentication = auth;
                    Client.Ciphering.Security = security;
                    Client.CtoSChallenge = challenge;
                    Client.Ciphering.Signing = signing;
                }
            }
        }

        /// <summary>
        /// Initialize connection to the meter.
        /// </summary>
        public async Task InitializeConnection(bool preEstablished, bool ignoreSNRMWithPreEstablished, string? invocationCounter)
        {
            await UpdateFrameCounter(invocationCounter);
            InitializeOpticalHead();
            GXReplyData reply = new();
            byte[] data;
            if (!ignoreSNRMWithPreEstablished)
            {
                data = Client.SNRMRequest();
                if (data != null)
                {
                    if (_consoleTrace > TraceLevel.Info)
                    {
                        Console.WriteLine("Send SNRM request." + GXCommon.ToHex(data, true));
                    }
                    ReadDataBlock(data, reply);
                    if (_consoleTrace == TraceLevel.Verbose)
                    {
                        Console.WriteLine("Parsing UA reply." + reply.ToString());
                    }
                    //Has server accepted client.
                    Client.ParseUAResponse(reply.Data);
                    if (_consoleTrace > TraceLevel.Info)
                    {
                        Console.WriteLine("Parsing UA reply succeeded.");
                    }
                }
            }
            if (!preEstablished)
            {
                //Generate AARQ request.
                //Split requests to multiple packets if needed.
                //If password is used all data might not fit to one packet.
                CancellationToken.ThrowIfCancellationRequested();
                foreach (byte[] it in Client.AARQRequest())
                {
                    if (_consoleTrace > TraceLevel.Info)
                    {
                        Console.WriteLine("Send AARQ request {0}", GXCommon.ToHex(it, true));
                    }
                    reply.Clear();
                    ReadDataBlock(it, reply);
                }
                if (_consoleTrace > TraceLevel.Info)
                {
                    Console.WriteLine("Parsing AARE reply" + reply.ToString());
                }
                //Parse reply.
                CancellationToken.ThrowIfCancellationRequested();
                Client.ParseAAREResponse(reply.Data);
                reply.Clear();
                //Get challenge Is HLS authentication is used.
                if (Client.Authentication > Authentication.Low)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    foreach (byte[] it in Client.GetApplicationAssociationRequest())
                    {
                        reply.Clear();
                        ReadDataBlock(it, reply);
                    }
                    CancellationToken.ThrowIfCancellationRequested();
                    Client.ParseApplicationAssociationResponse(reply.Data);
                }
                if (_consoleTrace > TraceLevel.Info)
                {
                    Console.WriteLine("Parsing AARE reply succeeded.");
                }
            }
        }

        /// <summary>
        /// This method is used to update meter firmware.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="identification">Firmware image identifier.</param>
        /// <param name="data">Firmware image bytes.</param>
        public void ImageUpdate(GXDLMSImageTransfer target, byte[] identification, byte[] data)
        {
            //Check that image transfer ia enabled.
            GXReplyData reply = new();
            CancellationToken.ThrowIfCancellationRequested();
            ReadDataBlock(Client.Read(target, 5), reply);
            Client.UpdateValue(target, 5, reply.Value);
            if (!target.ImageTransferEnabled)
            {
                throw new Exception("Image transfer is not enabled");
            }

            //Step 1: Read image block size.
            reply.Clear();
            CancellationToken.ThrowIfCancellationRequested();
            ReadDataBlock(Client.Read(target, 2), reply);
            Client.UpdateValue(target, 2, reply.Value);

            // Step 2: Initiate the Image transfer process.
            reply.Clear();
            ReadDataBlock(target.ImageTransferInitiate(Client, identification, data.Length), reply);

            // Step 3: Transfers ImageBlocks.
            reply.Clear();
            ReadDataBlock(target.ImageBlockTransfer(Client, data, out _), reply);

            //Step 4: Check the completeness of the Image.
            reply.Clear();
            CancellationToken.ThrowIfCancellationRequested();
            ReadDataBlock(Client.Read(target, 3), reply);
            Client.UpdateValue(target, 3, reply.Value);
            DateTime now = DateTime.Now;
            // Step 5: The Image is verified;
            try
            {
                reply.Clear();
                ReadDataBlock(target.ImageVerify(Client), reply);
            }
            catch (GXDLMSException e)
            {
                if (e.ErrorCode != (short)ErrorCode.TemporaryFailure)
                {
                    throw;
                }
                do
                {
                    Wait(10000);
                    //Read image transfer status.
                    reply.Clear();
                    CancellationToken.ThrowIfCancellationRequested();
                    ReadDataBlock(Client.Read(target, 6), reply);
                    Client.UpdateValue(target, 6, reply.Value);
                    //The image verify can take max 30 mins.
                    if ((DateTime.Now - now).TotalMinutes > 30)
                    {
                        throw new TimeoutException();
                    }
                }
                while (target.ImageTransferStatus != ImageTransferStatus.VerificationSuccessful &&
                target.ImageTransferStatus != ImageTransferStatus.VerificationFailed);
            }
            //Step 7: Activate image.
            try
            {
                reply.Clear();
                ReadDataBlock(target.ImageActivate(Client), reply);
            }
            catch (GXDLMSException e)
            {
                if (e.ErrorCode != (short)ErrorCode.TemporaryFailure)
                {
                    throw;
                }
                do
                {
                    Wait(10000);
                    //Read image transfer status.
                    reply.Clear();
                    CancellationToken.ThrowIfCancellationRequested();
                    ReadDataBlock(Client.Read(target, 6), reply);
                    Client.UpdateValue(target, 6, reply.Value);
                    //The image activication can take max 30 mins.
                    if ((DateTime.Now - now).TotalMinutes > 30)
                    {
                        throw new TimeoutException();
                    }
                } while (target.ImageTransferStatus != ImageTransferStatus.ActivationSuccessful &&
                target.ImageTransferStatus != ImageTransferStatus.ActivationFailed);
            }
            ;
        }

        /// <summary>
        /// Read association view.
        /// </summary>
        public void GetAssociationView(bool useCache)
        {
            // Association views are always read from the device; retain the legacy API argument.
            _ = useCache;
            GXReplyData reply = new();
            CancellationToken.ThrowIfCancellationRequested();
            ReadDataBlock(Client.GetObjectsRequest(), reply);
            Client.ParseObjects(reply.Data, true);
        }

        /// <summary>
        /// Read scalers and units.
        /// </summary>
        public async Task GetScalersAndUnits()
        {
            GXDLMSObjectCollection objs = Client.Objects.GetObjects([ObjectType.Register, ObjectType.ExtendedRegister, ObjectType.DemandRegister]);
            //If trace is info.
            if (_consoleTrace > TraceLevel.Warning)
            {
                Console.WriteLine("Read scalers and units from the device.");
            }
            if ((Client.NegotiatedConformance & Conformance.MultipleReferences) != 0)
            {
                List<KeyValuePair<GXDLMSObject, int>> list = [];
                foreach (GXDLMSObject it in objs)
                {
                    if (it is GXDLMSRegister)
                    {
                        list.Add(new KeyValuePair<GXDLMSObject, int>(it, 3));
                    }
                    if (it is GXDLMSDemandRegister)
                    {
                        list.Add(new KeyValuePair<GXDLMSObject, int>(it, 4));
                    }
                }
                if (list.Count != 0)
                {
                    ReadList(list);
                }
            }
            else
            {
                //Read values one by one.
                foreach (GXDLMSObject it in objs)
                {
                    try
                    {
                        if (it is GXDLMSRegister)
                        {
                            Console.WriteLine(it.Name);
                            await Read(it, 3);
                        }
                        if (it is GXDLMSDemandRegister)
                        {
                            Console.WriteLine(it.Name);
                            await Read(it, 4);
                        }
                    }
                    catch
                    {
                        //Actaric SL7000 can return error here. Continue reading.
                    }
                }
            }
        }

        /// <summary>
        /// Read profile generic columns.
        /// </summary>
        public async Task GetProfileGenericColumns()
        {
            //Read Profile Generic columns first.
            foreach (GXDLMSObject it in Client.Objects.GetObjects(ObjectType.ProfileGeneric))
            {
                try
                {
                    //If info.
                    if (_consoleTrace > TraceLevel.Warning)
                    {
                        Console.WriteLine(it.LogicalName);
                    }
                    await Read(it, 3);
                    //If info.
                    if (_consoleTrace > TraceLevel.Warning)
                    {
                        GXDLMSObject[] cols = ((GXDLMSProfileGeneric)it).GetCaptureObject();
                        StringBuilder sb = new();
                        bool First = true;
                        foreach (GXDLMSObject col in cols)
                        {
                            if (!First)
                            {
                                sb.Append(" | ");
                            }
                            First = false;
                            sb.Append(col.Name);
                            sb.Append(' ');
                            sb.Append(col.Description);
                        }
                        Console.WriteLine(sb.ToString());
                    }
                }
                catch (Exception)
                {
                    //Continue reading.
                }
            }
        }

        /// <summary>
        /// Read DLMS Data from the device.
        /// </summary>
        /// <param name="data">Data to send.</param>
        /// <param name="reply">Reply populated with received data.</param>
        /// <returns>Received data.</returns>
        public void ReadDLMSPacket(byte[]? data, GXReplyData reply)
        {
            CancellationToken.ThrowIfCancellationRequested();
            if (data == null && !reply.IsStreaming())
            {
                return;
            }
            reply.Error = 0;
            object? eop = (byte)0x7E;
            //In network connection terminator is not used.
            if (Client.InterfaceType == InterfaceType.WRAPPER && Media is GXNet)
            {
                eop = null;
            }
            int pos = 0;
            bool succeeded = false;
            ReceiveParameters<byte[]> p = new()
            {
                Eop = eop,
                Count = 5,
                WaitTime = WaitTime,
            };
            GXReplyData notify = new();
            lock (Media.Synchronous)
            {
                while (!succeeded && pos != 3)
                {
                    if (!reply.IsStreaming())
                    {
                        WriteTrace(true, GXCommon.ToHex(data, true)).GetAwaiter().GetResult();
                        Media.Send(data, null);
                    }
                    succeeded = Receive(p);
                    if (!succeeded)
                    {
                        if (++pos >= RetryCount)
                        {
                            throw new Exception("Failed to receive reply from the device in given time.");
                        }
                        //If Eop is not set read one byte at time.
                        if (p.Eop == null)
                        {
                            p.Count = 1;
                        }
                        //Try to read again...
                        System.Diagnostics.Debug.WriteLine("Data send failed. Try to resend " + pos.ToString() + "/3");
                    }
                }
                try
                {
                    pos = 0;
                    //Loop until whole COSEM packet is received.
                    CancellationToken.ThrowIfCancellationRequested();
                    while (!Client.GetData(p.Reply, reply, notify))
                    {
                        // If all data is received.
                        if (notify.IsComplete && !notify.IsMoreData)
                        {
                            /*
                            try
                            {
                                if (GXNotifyListener.Parser != null)
                                {
                                    GXAMIClient cl = new GXAMIClient();
                                    GXNotifyListener.Parser.Parse(Media.ToString(), notify.Value, cl.GetData, cl.SetData);
                                }
                            }
                            catch (Exception ex)
                            {
                                //TODO: Save error to the database.
                            }
                            */
                            notify.Clear();
                        }

                        //If Eop is not set read one byte at time.
                        if (p.Eop == null)
                        {
                            p.Count = 1;
                        }
                        while (!Receive(p))
                        {
                            if (++pos >= RetryCount)
                            {
                                throw new Exception("Failed to receive reply from the device in given time.");
                            }
                            //If echo.
                            if (data != null && (p.Reply == null || p.Reply.Length == data.Length))
                            {
                                Media.Send(data, null);
                            }
                            //Try to read again...
                            System.Diagnostics.Debug.WriteLine("Data send failed. Try to resend " + pos.ToString() + "/3");
                        }
                    }
                }
                catch (Exception)
                {
                    WriteTrace(false, GXCommon.ToHex(p.Reply, true)).GetAwaiter().GetResult();
                    throw;
                }
            }
            WriteTrace(false, GXCommon.ToHex(p.Reply, true)).GetAwaiter().GetResult();
            if (reply.Error != 0)
            {
                if (reply.Error == (short)ErrorCode.Rejected)
                {
                    Wait(1000);
                    ReadDLMSPacket(data, reply);
                }
                else
                {
                    throw new GXDLMSException(reply.Error);
                }
            }
        }

        /// <summary>
        /// Send data block(s) to the meter.
        /// </summary>
        /// <param name="data">Send data block(s).</param>
        /// <param name="reply">Received reply from the meter.</param>
        /// <returns>Return false if frame is rejected.</returns>
        public bool ReadDataBlock(byte[][] data, GXReplyData reply)
        {
            if (data == null)
            {
                return true;
            }
            foreach (byte[] it in data)
            {
                reply.Clear();
                ReadDataBlock(it, reply);
            }
            return reply.Error == 0;
        }

        /// <summary>
        /// Read data block from the device.
        /// </summary>
        /// <param name="data">data to send</param>
        /// <param name="reply">Received data.</param>
        /// <returns>Received data.</returns>
        public void ReadDataBlock(byte[]? data, GXReplyData reply)
        {
            ReadDLMSPacket(data, reply);
            lock (Media.Synchronous)
            {
                while (reply.IsMoreData)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    if (reply.IsStreaming())
                    {
                        data = null;
                    }
                    else
                    {
                        CancellationToken.ThrowIfCancellationRequested();
                        data = Client.ReceiverReady(reply);
                    }
                    ReadDLMSPacket(data, reply);
                    if (_consoleTrace > TraceLevel.Info)
                    {
                        //If data block is read.
                        if ((reply.MoreData & RequestTypes.Frame) == 0)
                        {
                            Console.Write("+");
                        }
                        else
                        {
                            Console.Write("-");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Read attribute value.
        /// </summary>
        /// <param name="it">COSEM object to read.</param>
        /// <param name="attributeIndex">Attribute index.</param>
        /// <returns>Read value.</returns>
        public async Task<object> Read(GXDLMSObject it, int attributeIndex)
        {
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
                GXReplyData reply = new();
                CancellationToken.ThrowIfCancellationRequested();
                if (!ReadDataBlock(Client.Read(it, attributeIndex), reply))
                {
                    if (reply.Error != (short)ErrorCode.Rejected)
                    {
                        throw new GXDLMSException(reply.Error);
                    }
                    reply.Clear();
                    Wait(1000);
                    CancellationToken.ThrowIfCancellationRequested();
                    if (!ReadDataBlock(Client.Read(it, attributeIndex), reply))
                    {
                        throw new GXDLMSException(reply.Error);
                    }
                }
                //Update data type.
                DataType dt = it.GetDataType(attributeIndex);
                if (dt == DataType.None)
                {
                    it.SetDataType(attributeIndex, reply.DataType);
                    dt = reply.DataType;
                }
                if (dt == DataType.Array || dt == DataType.Structure || reply.Value is GXStructure || reply.Value is GXArray)
                {
                    if (it is GXDLMSProfileGeneric && attributeIndex == 2)
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
                object value = Client.UpdateValue(it, attributeIndex, reply.Value);
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
                return value;
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
                    await http.PostAsJson<AddLogResponse>("/api/Log/Add?source=device-action", log, CancellationToken);
                }
            }
        }


        /// <summary>
        /// Read list of attributes.
        /// </summary>
        public void ReadList(List<KeyValuePair<GXDLMSObject, int>> list)
        {
            CancellationToken.ThrowIfCancellationRequested();
            byte[][] data = Client.ReadList(list);
            GXReplyData reply = new();
            List<object> values = [];
            foreach (byte[] it in data)
            {
                ReadDataBlock(it, reply);
                if (list.Count != 1 && reply.Value is object[] replyValues)
                {
                    values.AddRange(replyValues);
                }
                else if (reply.Value != null)
                {
                    //Value is null if data is send in multiple frames.
                    values.Add(reply.Value);
                }
                reply.Clear();
            }
            if (values.Count != list.Count)
            {
                throw new Exception("Invalid reply. Read items count do not match.");
            }
            Client.UpdateValues(list, values);
        }

        /// <summary>
        /// Write attribute value.
        /// </summary>
        public void Write(GXDLMSObject it, int attributeIndex)
        {
            GXReplyData reply = new();
            CancellationToken.ThrowIfCancellationRequested();
            ReadDataBlock(Client.Write(it, attributeIndex), reply);
        }

        /// <summary>
        /// Method attribute value.
        /// </summary>
        public void Method(GXDLMSObject it, int attributeIndex, object value, DataType type)
        {
            GXReplyData reply = new();
            CancellationToken.ThrowIfCancellationRequested();
            ReadDataBlock(Client.Method(it, attributeIndex, value, type), reply);
        }

        /// <summary>
        /// Read Profile Generic Columns by entry.
        /// </summary>
        public object ReadRowsByEntry(GXDLMSProfileGeneric it, UInt32 index, UInt32 count)
        {
            GXReplyData reply = new();
            CancellationToken.ThrowIfCancellationRequested();
            ReadDataBlock(Client.ReadRowsByEntry(it, index, count), reply);
            return reply.Value;
        }

        /// <summary>
        /// Read Profile Generic Columns by range.
        /// </summary>
        public object ReadRowsByRange(GXDLMSProfileGeneric it, DateTime start, DateTime end)
        {
            GXReplyData reply = new();
            CancellationToken.ThrowIfCancellationRequested();
            if (!ReadDataBlock(Client.ReadRowsByRange(it, start, end), reply))
                {
                throw new GXDLMSException(reply.Error);
            }
            return reply.Value;
        }

        /// <summary>
        /// Disconnect.
        /// </summary>
        public void Disconnect(bool closeConnection = true)
        {
            if (_media != null && _client != null)
            {
                try
                {
                    if (_consoleTrace > TraceLevel.Info)
                    {
                        Console.WriteLine("Disconnecting from the meter.");
                    }
                    GXReplyData reply = new();
                    ReadDLMSPacket(Client.DisconnectRequest(), reply);
                    if (closeConnection)
                    {
                        Media.Close();
                    }
                }
                catch
                {

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
                if (_consoleTrace > TraceLevel.Info)
                {
                    Console.WriteLine("Release from the meter.");
                }
                GXReplyData reply = new();
                try
                {

                    if (Client.InterfaceType == InterfaceType.WRAPPER ||
                        Client.Ciphering.Security != (byte)Security.None)
                    {
                        CancellationToken.ThrowIfCancellationRequested();
                        ReadDataBlock(Client.ReleaseRequest(), reply);
                    }
                }
                catch (Exception ex)
                {
                    //All meters don't support Release.
                    Console.WriteLine("Release failed. " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Close connection to the meter.
        /// </summary>
        public void Close()
        {
            var media = _media;
            var client = _client;
            if (media == null)
                {
                return;
            }
            try
            {
                if (client != null && !CancellationToken.IsCancellationRequested)
                {
                    var reply = new GXReplyData();
                    try
                    {
                        if ((client.ConnectionState & ConnectionState.Dlms) != 0 &&
                            (client.InterfaceType == InterfaceType.WRAPPER || client.Ciphering.Security != Security.None))
                        {
                            CancellationToken.ThrowIfCancellationRequested();
                            ReadDataBlock(client.ReleaseRequest(), reply);
                        }
                    }
                    catch (Exception error) { _logger?.LogDebug(error, "Releasing the meter association failed."); }
                    if (client.ConnectionState != 0)
                    {
                        reply.Clear();
                        ReadDLMSPacket(client.DisconnectRequest(), reply);
                    }
                }
            }
            catch (Exception error) { _logger?.LogDebug(error, "Disconnecting from the meter failed."); }
            finally
            {
                try
                { media.Close(); }
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
                    await http.PostAsJson<AddDeviceTraceResponse>("/api/DeviceTrace/Add", it, CancellationToken);
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

