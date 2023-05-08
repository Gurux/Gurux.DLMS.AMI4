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

using System.ComponentModel;
using System.Xml;

namespace Gurux.DLMS.AMI.Client.Pages.Media
{
    class GXSerialTemplate
    {
        /// <summary>
        /// Gets or sets the port for communications, including but not limited to all available COM ports.
        /// </summary>
        public string? PortName
        {
            get;
            set;
        }

        /// <summary>
        /// Used baud rate for communication.
        /// </summary>
        /// <remarks>Can be changed without disconnecting.</remarks>
        [DefaultValue(9600)]
        public int BaudRate
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the standard number of stopbits per byte.
        /// </summary>
        [DefaultValue(1)]
        public int StopBits
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the parity-checking protocol.
        /// </summary>
        public int Parity
        {
            get;
            set;
        }


        /// <summary>
        /// Gets or sets the standard length of data bits per byte.
        /// </summary>
        [DefaultValue(8)]
        public int DataBits
        {
            get;
            set;
        }


        /// <summary>
        /// Gets or sets the handshaking protocol for serial port transmission of data.
        /// </summary>
        public int Handshake
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXSerialTemplate()
        {
            BaudRate = 9600;
            StopBits = 1;
            Parity = 0;
            DataBits = 8;
        }

        /// <summary>
        /// Media settings as a XML string.
        /// </summary>
        public string? Settings
        {
            get
            {
                string tmp = "";
                if (!string.IsNullOrEmpty(PortName))
                {
                    tmp += "<Port>" + PortName + "</Port>";
                }
                if (BaudRate != 9600)
                {
                    tmp += "<Bps>" + BaudRate + "</Bps>";
                }
                if (StopBits != 1)
                {
                    tmp += "<StopBits>" + StopBits + "</StopBits>";
                }
                if (Parity != 0)
                {
                    tmp += "<Parity>" + Parity + "</Parity>";
                }
                if (DataBits != 8)
                {
                    tmp += "<ByteSize>" + DataBits + "</ByteSize>";
                }
                if (Handshake != 0)
                {
                    tmp += "<Handshake>" + Handshake + "</Handshake>";
                }
                return tmp;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.ConformanceLevel = ConformanceLevel.Fragment;
                    //Set default settings.
                    BaudRate = 9600;
                    DataBits = 8;
                    Parity = 0;
                    StopBits = 1;
                    using (XmlReader xmlReader = XmlReader.Create(new System.IO.StringReader(value), settings))
                    {
                        while (xmlReader.Read())
                        {
                            if (xmlReader.IsStartElement())
                            {
                                switch (xmlReader.Name)
                                {
                                    case "Port":
                                        PortName = xmlReader.ReadString();
                                        break;
                                    case "Bps":
                                        BaudRate = Convert.ToInt32(xmlReader.ReadString());
                                        break;
                                    case "StopBits":
                                        StopBits = int.Parse(xmlReader.ReadString());
                                        break;
                                    case "Parity":
                                        Parity = int.Parse(xmlReader.ReadString());
                                        break;
                                    case "ByteSize":
                                        DataBits = int.Parse(xmlReader.ReadString());
                                        break;
                                    case "Handshake":
                                        Handshake = int.Parse(xmlReader.ReadString());
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
