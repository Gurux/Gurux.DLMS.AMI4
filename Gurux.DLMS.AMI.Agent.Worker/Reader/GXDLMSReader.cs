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
// More information of Gurux products: http://www.gurux.org
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.Common;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
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
        private readonly int WaitTime;
        /// <summary>
        /// Retry count.
        /// </summary>
        private readonly int RetryCount = 3;
        IGXMedia Media;
        TraceLevel _consoleTrace;
        TraceLevel _deviceTrace;
        internal GXDLMSSecureClient Client;
        private readonly ILogger? _logger;
        private GXDevice? _device;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="client">DLMS Client.</param>
        /// <param name="media">Media.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="consoleTrace">Console Trace level.</param>
        /// <param name="deviceTrace">Device Trace level.</param>
        public GXDLMSReader(
            GXDLMSSecureClient client,
            IGXMedia media,
            ILogger? logger,
            TraceLevel consoleTrace,
            TraceLevel deviceTrace,
            int wt,
            int retry,
            GXDevice? device)
        {
            WaitTime = wt * 1000;
            RetryCount = retry;
            _consoleTrace = consoleTrace;
            _deviceTrace = deviceTrace;
            Media = media;
            Client = client;
            _logger = logger;
            _device = device;
        }

        /// <summary>
        /// Send IEC disconnect message.
        /// </summary>
        void DiscIEC()
        {
            ReceiveParameters<string> p = new ReceiveParameters<string>()
            {
                AllData = false,
                Eop = (byte)0x0A,
                WaitTime = WaitTime * 1000
            };
            string data = (char)0x01 + "B0" + (char)0x03 + "\r\n";
            Media.Send(data, null);
            p.Eop = "\n";
            p.AllData = true;
            p.Count = 1;

            Media.Receive(p);
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
            GXSerial serial = Media as GXSerial;
            byte Terminator = (byte)0x0A;
            Media.Open();
            if (serial != null)
            {
                //Some meters need a little break.
                Thread.Sleep(1000);
            }
            //Query device information.
            string data = "/?!\r\n";
            if (_consoleTrace > TraceLevel.Info)
            {
                Console.WriteLine("IEC Sending:" + data);
            }
            ReceiveParameters<string> p = new ReceiveParameters<string>()
            {
                AllData = false,
                Eop = Terminator,
                WaitTime = WaitTime * 1000
            };
            lock (Media.Synchronous)
            {
                Media.Send(data, null);
                if (!Media.Receive(p))
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
                    if (!Media.Receive(p))
                    {
                        throw new Exception(str);
                    }
                }
                //If echo is used.
                if (p.Reply == data)
                {
                    p.Reply = null;
                    if (!Media.Receive(p))
                    {
                        //Try to move away from mode E.
                        GXReplyData reply = new GXReplyData();
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
            int pos = 0;
            //With some meters there might be some extra invalid chars. Remove them.
            while (pos < p.Reply.Length && p.Reply[pos] != '/')
            {
                ++pos;
            }
            if (p.Reply[pos] != '/')
            {
                p.WaitTime = 100;
                Media.Receive(p);
                DiscIEC();
                throw new Exception("Invalid responce.");
            }
            string manufactureID = p.Reply.Substring(1 + pos, 3);
            char baudrate = p.Reply[4 + pos];
            int BaudRate = 0;
            switch (baudrate)
            {
                case '0':
                    BaudRate = 300;
                    break;
                case '1':
                    BaudRate = 600;
                    break;
                case '2':
                    BaudRate = 1200;
                    break;
                case '3':
                    BaudRate = 2400;
                    break;
                case '4':
                    BaudRate = 4800;
                    break;
                case '5':
                    BaudRate = 9600;
                    break;
                case '6':
                    BaudRate = 19200;
                    break;
                default:
                    throw new Exception("Unknown baud rate.");
            }
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
            byte[] arr = new byte[] { 0x06, controlCharacter, (byte)baudrate, ModeControlCharacter, 13, 10 };
            if (_consoleTrace > TraceLevel.Info)
            {
                Console.WriteLine(DateTime.Now.ToLongTimeString() + "\tMoving to mode E.", arr);
            }
            lock (Media.Synchronous)
            {
                p.Reply = null;
                Media.Send(arr, null);
                //Some meters need this sleep. Do not remove.
                Thread.Sleep(200);
                p.WaitTime = 2000;
                //Note! All meters do not echo this.
                Media.Receive(p);
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
                Thread.Sleep(800);
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
                GXReplyData reply = new GXReplyData();
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
                    foreach (byte[] it in Client.AARQRequest())
                    {
                        if (_consoleTrace > TraceLevel.Info)
                        {
                            Console.WriteLine("Send AARQ request", GXCommon.ToHex(it, true));
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
                        Client.ParseAAREResponse(reply.Data);
                        reply.Clear();
                        GXDLMSData d = new GXDLMSData(invocationCounter);
                        await Read(d, 2);
                        Client.Ciphering.InvocationCounter = 1 + Convert.ToUInt32(d.Value);
                        Console.WriteLine("Invocation counter: " + Convert.ToString(Client.Ciphering.InvocationCounter));
                        reply.Clear();
                        Disconnect(false);
                        //Some meters need this sleep. Do not remove.
                        Thread.Sleep(800);
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
            GXReplyData reply = new GXReplyData();
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
                foreach (byte[] it in Client.AARQRequest())
                {
                    if (_consoleTrace > TraceLevel.Info)
                    {
                        Console.WriteLine("Send AARQ request", GXCommon.ToHex(it, true));
                    }
                    reply.Clear();
                    ReadDataBlock(it, reply);
                }
                if (_consoleTrace > TraceLevel.Info)
                {
                    Console.WriteLine("Parsing AARE reply" + reply.ToString());
                }
                //Parse reply.
                Client.ParseAAREResponse(reply.Data);
                reply.Clear();
                //Get challenge Is HLS authentication is used.
                if (Client.Authentication > Authentication.Low)
                {
                    foreach (byte[] it in Client.GetApplicationAssociationRequest())
                    {
                        reply.Clear();
                        ReadDataBlock(it, reply);
                    }
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
        public void ImageUpdate(GXDLMSImageTransfer target, byte[] identification, byte[] data)
        {
            //Check that image transfer ia enabled.
            GXReplyData reply = new GXReplyData();
            ReadDataBlock(Client.Read(target, 5), reply);
            Client.UpdateValue(target, 5, reply.Value);
            if (!target.ImageTransferEnabled)
            {
                throw new Exception("Image transfer is not enabled");
            }

            //Step 1: Read image block size.
            ReadDataBlock(Client.Read(target, 2), reply);
            Client.UpdateValue(target, 2, reply.Value);

            // Step 2: Initiate the Image transfer process.
            ReadDataBlock(target.ImageTransferInitiate(Client, identification, data.Length), reply);

            // Step 3: Transfers ImageBlocks.
            int imageBlockCount;
            ReadDataBlock(target.ImageBlockTransfer(Client, data, out imageBlockCount), reply);

            //Step 4: Check the completeness of the Image.
            ReadDataBlock(Client.Read(target, 3), reply);
            Client.UpdateValue(target, 3, reply.Value);

            // Step 5: The Image is verified;
            ReadDataBlock(target.ImageVerify(Client), reply);
            // Step 6: Before activation, the Image is checked;

            //Get list to images to activate.
            ReadDataBlock(Client.Read(target, 7), reply);
            Client.UpdateValue(target, 7, reply.Value);
            bool bFound = false;
            foreach (GXDLMSImageActivateInfo it in target.ImageActivateInfo)
            {
                if (it.Identification == identification)
                {
                    bFound = true;
                    break;
                }
            }

            //Read image transfer status.
            ReadDataBlock(Client.Read(target, 6), reply);
            Client.UpdateValue(target, 6, reply.Value);
            if (target.ImageTransferStatus != Gurux.DLMS.Objects.Enums.ImageTransferStatus.VerificationSuccessful)
            {
                throw new Exception("Image transfer status is " + target.ImageTransferStatus.ToString());
            }

            if (!bFound)
            {
                throw new Exception("Image not found.");
            }

            //Step 7: Activate image.
            ReadDataBlock(target.ImageActivate(Client), reply);
        }
        /// <summary>
        /// Read association view.
        /// </summary>
        public void GetAssociationView(bool useCache)
        {
            GXReplyData reply = new GXReplyData();
            ReadDataBlock(Client.GetObjectsRequest(), reply);
            Client.ParseObjects(reply.Data, true);
        }

        /// <summary>
        /// Read scalers and units.
        /// </summary>
        public async Task GetScalersAndUnits()
        {
            GXDLMSObjectCollection objs = Client.Objects.GetObjects(new ObjectType[] { ObjectType.Register, ObjectType.ExtendedRegister, ObjectType.DemandRegister });
            //If trace is info.
            if (_consoleTrace > TraceLevel.Warning)
            {
                Console.WriteLine("Read scalers and units from the device.");
            }
            if ((Client.NegotiatedConformance & Conformance.MultipleReferences) != 0)
            {
                List<KeyValuePair<GXDLMSObject, int>> list = new List<KeyValuePair<GXDLMSObject, int>>();
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
                        GXDLMSObject[] cols = (it as GXDLMSProfileGeneric).GetCaptureObject();
                        StringBuilder sb = new StringBuilder();
                        bool First = true;
                        foreach (GXDLMSObject col in cols)
                        {
                            if (!First)
                            {
                                sb.Append(" | ");
                            }
                            First = false;
                            sb.Append(col.Name);
                            sb.Append(" ");
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
        /// <returns>Received data.</returns>
        public void ReadDLMSPacket(byte[] data, GXReplyData reply)
        {
            if (data == null && !reply.IsStreaming())
            {
                return;
            }
            reply.Error = 0;
            object eop = (byte)0x7E;
            //In network connection terminator is not used.
            if (Client.InterfaceType == InterfaceType.WRAPPER && Media is GXNet)
            {
                eop = null;
            }
            int pos = 0;
            bool succeeded = false;
            ReceiveParameters<byte[]> p = new ReceiveParameters<byte[]>()
            {
                Eop = eop,
                Count = 5,
                WaitTime = WaitTime,
            };
            GXReplyData notify = new GXReplyData();
            lock (Media.Synchronous)
            {
                while (!succeeded && pos != 3)
                {
                    if (!reply.IsStreaming())
                    {
                        WriteTrace(true, GXCommon.ToHex(data, true));
                        Media.Send(data, null);
                    }
                    succeeded = Media.Receive(p);
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
                        while (!Media.Receive(p))
                        {
                            if (++pos >= RetryCount)
                            {
                                throw new Exception("Failed to receive reply from the device in given time.");
                            }
                            //If echo.
                            if (p.Reply == null || p.Reply.Length == data.Length)
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
                    WriteTrace(false, GXCommon.ToHex(p.Reply, true));
                    throw;
                }
            }
            WriteTrace(false, GXCommon.ToHex(p.Reply, true));
            if (reply.Error != 0)
            {
                if (reply.Error == (short)ErrorCode.Rejected)
                {
                    Thread.Sleep(1000);
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
        public void ReadDataBlock(byte[] data, GXReplyData reply)
        {
            ReadDLMSPacket(data, reply);
            lock (Media.Synchronous)
            {
                while (reply.IsMoreData)
                {
                    if (reply.IsStreaming())
                    {
                        data = null;
                    }
                    else
                    {
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
            GXDeviceAction? a = null;
            if (_device != null)
            {
                a = new GXDeviceAction();
                a.Device = new GXDevice()
                {
                    Id = _device.Id,
                    Name = _device.Name
                };
                a.Type = DeviceActionType.Read;
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
                GXReplyData reply = new GXReplyData();
                if (!ReadDataBlock(Client.Read(it, attributeIndex), reply))
                {
                    if (reply.Error != (short)ErrorCode.Rejected)
                    {
                        throw new GXDLMSException(reply.Error);
                    }
                    reply.Clear();
                    Thread.Sleep(1000);
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
                    a.Reply = GXDLMSTranslator.ValueToXml(reply.Value);
                    return a.Reply;
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
                    a.Type = a.Type | DeviceActionType.Error;
                    a.Reply = ex.Message;
                }
                throw;
            }
            finally
            {
                if (_device != null && _device.TraceLevel > TraceLevel.Warning)
                {
                    //Send device action information if trace level is Info or Verbose.
                    AddDeviceAction log = new AddDeviceAction();
                    log.Actions = new GXDeviceAction[] { a };
                    await GXAgentWorker.client.PostAsJson<AddDeviceActionResponse>("/api/DeviceAction/Add", log);
                }
            }
        }


        /// <summary>
        /// Read list of attributes.
        /// </summary>
        public void ReadList(List<KeyValuePair<GXDLMSObject, int>> list)
        {
            byte[][] data = Client.ReadList(list);
            GXReplyData reply = new GXReplyData();
            List<object> values = new List<object>();
            foreach (byte[] it in data)
            {
                ReadDataBlock(it, reply);
                if (list.Count != 1 && reply.Value is object[])
                {
                    values.AddRange((object[])reply.Value);
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
            GXReplyData reply = new GXReplyData();
            ReadDataBlock(Client.Write(it, attributeIndex), reply);
        }

        /// <summary>
        /// Method attribute value.
        /// </summary>
        public void Method(GXDLMSObject it, int attributeIndex, object value, DataType type)
        {
            GXReplyData reply = new GXReplyData();
            ReadDataBlock(Client.Method(it, attributeIndex, value, type), reply);
        }

        /// <summary>
        /// Read Profile Generic Columns by entry.
        /// </summary>
        public object ReadRowsByEntry(GXDLMSProfileGeneric it, UInt32 index, UInt32 count)
        {
            GXReplyData reply = new GXReplyData();
            ReadDataBlock(Client.ReadRowsByEntry(it, index, count), reply);
            return reply.Value;
        }

        /// <summary>
        /// Read Profile Generic Columns by range.
        /// </summary>
        public object ReadRowsByRange(GXDLMSProfileGeneric it, DateTime start, DateTime end)
        {
            GXReplyData reply = new GXReplyData();
            ReadDataBlock(Client.ReadRowsByRange(it, start, end), reply);
            return reply.Value;
        }

        /// <summary>
        /// Disconnect.
        /// </summary>
        public void Disconnect(bool closeConnection = true)
        {
            if (Media != null && Client != null)
            {
                try
                {
                    if (_consoleTrace > TraceLevel.Info)
                    {
                        Console.WriteLine("Disconnecting from the meter.");
                    }
                    GXReplyData reply = new GXReplyData();
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
            if (Media != null && Client != null)
            {
                if (_consoleTrace > TraceLevel.Info)
                {
                    Console.WriteLine("Release from the meter.");
                }
                GXReplyData reply = new GXReplyData();
                try
                {

                    if (Client.InterfaceType == InterfaceType.WRAPPER ||
                        Client.Ciphering.Security != (byte)Security.None)
                    {
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
            if (Media != null && Client != null)
            {
                try
                {
                    if (_consoleTrace > TraceLevel.Info)
                    {
                        Console.WriteLine("Disconnecting from the meter.");
                    }
                    GXReplyData reply = new GXReplyData();
                    try
                    {
                        if ((Client.ConnectionState & ConnectionState.Dlms) != 0 &&
                            (Client.InterfaceType == InterfaceType.WRAPPER ||
                            Client.Ciphering.Security != (byte)Security.None))
                        {
                            ReadDataBlock(Client.ReleaseRequest(), reply);
                        }
                    }
                    catch (Exception ex)
                    {
                        //All meters don't support Release.
                        Console.WriteLine("Release failed. " + ex.Message);
                    }
                    reply.Clear();
                    if (Client.ConnectionState != 0)
                    {
                        ReadDLMSPacket(Client.DisconnectRequest(), reply);
                    }
                    Media.Close();
                }
                catch
                {

                }
                Media = null;
                Client = null;
            }
        }

        /// <summary>
        /// Write trace.
        /// </summary>
        /// <param name="frame">Data frame as a hex string.</param>
        async Task WriteTrace(bool tx, string frame)
        {
            if (_consoleTrace > TraceLevel.Info)
            {
                if (tx)
                {
                    Console.WriteLine("TX:\t" + DateTime.Now.ToString("hh:mm:ss") + "\t" + frame);
                }
                else
                {
                    Console.WriteLine("RX:\t" + DateTime.Now.ToString("hh:mm:ss") + "\t" + frame);
                }
            }
            _logger?.LogInformation(frame);
            if (_device != null && _deviceTrace > TraceLevel.Info)
            {
                try
                {
                    GXDeviceTrace trace = new GXDeviceTrace();
                    trace.Device = new GXDevice() { Id = _device.Id };
                    trace.Send = tx;
                    trace.Frame = frame;
                    AddDeviceTrace it = new AddDeviceTrace();
                    it.Traces = new GXDeviceTrace[] { trace };
                    _ = await GXAgentWorker.client.PostAsJson<AddDeviceTraceResponse>("/api/DeviceTrace/Add", it);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    _logger?.LogError(ex.Message);
                }
            }
        }
    }
}
