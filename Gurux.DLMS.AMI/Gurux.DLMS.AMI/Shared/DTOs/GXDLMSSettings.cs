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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// DLMS Settings
    /// </summary>
    public class GXDLMSSettings
    {
        /// <summary>
        /// Client system title.
        /// </summary>
        [DataMember, StringLength(16)]
        public string? ClientSystemTitle
        {
            get;
            set;
        }

        /// <summary>
        /// Device system title.
        /// </summary>
        [StringLength(16)]
        [Description("Device system title.")]
        public string? DeviceSystemTitle
        {
            get;
            set;
        }

        /// <summary>
        /// Block cipher key.
        /// </summary>
        [StringLength(32)]
        [Description("Block cipher key.")]
        public string? BlockCipherKey
        {
            get;
            set;
        }

        /// <summary>
        /// Authentication key.
        /// </summary>
        [StringLength(32)]
        [Description("Authentication key.")]
        public string? AuthenticationKey
        {
            get;
            set;
        }

        /// <summary>
        /// Dedicated Key.
        /// </summary>
        [DefaultValue(null)]
        public string? DedicatedKey
        {
            get;
            set;
        }

        /// <summary>
        /// Maximum used baud rate.
        /// </summary>
        [DefaultValue(0)]
        public int MaximumBaudRate
        {
            get;
            set;
        }

        /// <summary>
        /// Authentication Level.
        /// </summary>
        [Description("Authentication Level.")]
        public byte Authentication
        {
            get;
            set;
        }

        /// <summary>
        /// Authentication Level.
        /// </summary>
        [Description("Name of authentication level.")]
        public string? AuthenticationName
        {
            get;
            set;
        }

        /// <summary>
        /// Used standard.
        /// </summary>
        public byte Standard
        {
            get;
            set;
        }

        /// <summary>
        /// Password is used only if authentication is used.
        /// </summary>
        [DefaultValue(null)]
        public string? Password
        {
            get;
            set;
        }

        /// <summary>
        /// Is hex password used.
        /// </summary>
        public bool IsHex
        {
            get;
            set;
        }

        /// <summary>
        /// Password is used only if authentication is used.
        /// </summary>
        [DefaultValue(null)]
        public byte[]? HexPassword
        {
            get;
            set;
        }

        /// <summary>
        /// Used communication security.
        /// </summary>
        public byte Security
        {
            get;
            set;
        }

        /// <summary>
        /// Used Security Suite.
        /// </summary>
        public int SecuritySuite
        {
            get;
            set;
        }

        /// <summary>
        /// Use pre-established application associations.
        /// </summary>
        [DefaultValue(false)]
        public bool PreEstablished
        {
            get;
            set;
        }

        /// <summary>
        /// Invocation counter.
        /// </summary>
        [DefaultValue(0)]
        public UInt32 InvocationCounter
        {
            get;
            set;
        }

        /// <summary>
        /// Frame counter is used to update InvocationCounter automatically.
        /// </summary>
        [DefaultValue(null)]
        [StringLength(25)]
        public string? FrameCounter
        {
            get;
            set;
        }

        /// <summary>
        /// Static challenge.
        /// </summary>
        [DefaultValue(null)]
        [StringLength(128)]
        public string? Challenge
        {
            get;
            set;
        }

        /// <summary>
        /// Signing type.
        /// </summary>
        public int Signing
        {
            get;
            set;
        }

        /// <summary>
        /// Signing key of the client.
        /// </summary>
        [DefaultValue(null)]
        public string? ClientSigningKey
        {
            get;
            set;
        }

        /// <summary>
        /// Agreement key of the client.
        /// </summary>
        [DefaultValue(null)]
        public string? ClientAgreementKey
        {
            get;
            set;
        }

        /// <summary>
        /// Signing key of the server.
        /// </summary>
        [DefaultValue(null)]
        public string? ServerSigningKey
        {
            get;
            set;
        }

        /// <summary>
        /// Agreement key of the server.
        /// </summary>
        [DefaultValue(null)]
        public string? ServerAgreementKey
        {
            get;
            set;
        }

        /// <summary>
        /// Used logical client ID.
        /// </summary>
        /// <remarks>
        /// Client ID is always 1 byte long in HDLC and 2 bytes long when WRAPPER is used.
        /// </remarks>
        [DefaultValue(0x10)]
        [Description("Client address.")]
        public int ClientAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Used Physical address.
        /// </summary>
        /// <remarks>
        /// Server HDLC Address (Logical + Physical address)  might be 1,2 or 4 bytes long.
        /// </remarks>
        [DefaultValue(1)]
        virtual public int PhysicalAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Used logical address.
        /// </summary>
        [DefaultValue(0)]
        public int LogicalAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Communication profiles.
        /// </summary>
        public List<GXCommunicationProfile> Profiles { get; set; } = new List<GXCommunicationProfile>();

        /// <summary>
        /// Standard says that Time zone is from normal time to UTC in minutes.
        /// If meter is configured to use UTC time (UTC to normal time) set this to true.
        /// Example. Italy, Saudi Arabia and India standards are using UTC time zone, not DLMS standard time zone.
        /// </summary>
        [DefaultValue(false)]
        [Description("Use UTC time zone.")]
        public bool UtcTimeZone
        {
            get;
            set;
        }

        /// <summary>
        /// Skipped date time fields. This value can be used if meter can't handle deviation or status.
        /// </summary>
        [Description("Skipped date time fields. This value can be used if meter can't handle deviation or status.")]
        public int DateTimeSkips
        {
            get;
            set;
        }

        /// <summary>
        /// Is serial port access through TCP/IP or UDP converter.
        /// </summary>
        [DefaultValue(false)]
        public bool UseRemoteSerial
        {
            get;
            set;
        }

        /// <summary>
        /// Used interface type.
        /// </summary>
        [Description("Interface type.")]
        public int InterfaceType
        {
            get;
            set;
        }

        /// <summary>
        /// The maximum information field length in transmit.
        /// </summary>
        /// <remarks>
        /// DefaultValue is 128. Minimum value is 32 and max value is 2030.
        /// </remarks>
        [DefaultValue(128)]
        public UInt16 MaxInfoTX
        {
            get;
            set;
        }

        /// <summary>
        /// The maximum information field length in receive.
        /// </summary>
        /// <remarks>
        /// DefaultValue is 128. Minimum value is 32 and max value is 2030.
        /// </remarks>
        [DefaultValue(128)]
        public UInt16 MaxInfoRX
        {
            get;
            set;
        }

        /// <summary>
        /// The window size in transmit.
        /// </summary>
        /// <remarks>
        /// DefaultValue is 1.
        /// </remarks>
        [DefaultValue(1)]
        public byte WindowSizeTX
        {
            get;
            set;
        }

        /// <summary>
        /// The window size in receive.
        /// </summary>
        /// <remarks>
        /// DefaultValue is 1.
        /// </remarks>
        [DefaultValue(1)]
        public byte WindowSizeRX
        {
            get;
            set;
        }

        /// <summary>
        /// PLC MAC Source Address
        /// </summary>
        /// <remarks>
        /// DefaultValue is 0xC00.
        /// </remarks>
        [DefaultValue(0xC00)]
        public UInt16 MACSourceAddress
        {
            get;
            set;
        }

        /// <summary>
        /// PLC MAC Target Address.
        /// </summary>
        public UInt16 MacDestinationAddress
        {
            get;
            set;
        }


        /// <summary>
        /// Proposed maximum size of PDU.
        /// </summary>
        /// <remarks>
        /// DefaultValue is 0xFFFF.
        /// </remarks>
        [DefaultValue(0xFFFF)]
        public UInt16 PduSize
        {
            get;
            set;
        }

        /// <summary>
        /// User Id.
        /// </summary>
        /// <remarks>
        /// In default user id is not used.
        /// </remarks>
        public short UserId
        {
            get;
            set;
        }

        /// <summary>
        /// Network ID.
        /// </summary>
        public byte NetworkId
        {
            get;
            set;
        }

        /// <summary>
        ///Inactivity timeout.
        /// </summary>
        public int InactivityTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Used Service class.
        /// </summary>
        public byte ServiceClass
        {
            get;
            set;
        }

        /// <summary>
        /// Used priority.
        /// </summary>
        public byte Priority
        {
            get;
            set;
        }


        /// <summary>
        /// Server address size.
        /// </summary>
        /// <remarks>
        /// This is not udes in default. Some meters require that server address size is constant.
        /// </remarks>
        [DefaultValue(0)]
        public byte ServerAddressSize
        {
            get;
            set;
        }

        /// <summary>
        /// Challenge size.
        /// </summary>
        /// <returns></returns>
        [DefaultValue(16)]
        public byte ChallengeSize
        {
            get;
            set;
        }

        /// <summary>
        /// Public key certificate is send in part of initialize messages (AARQ and AARE).
        /// </summary>
        /// <returns></returns>
        [DefaultValue(false)]
        public bool PublicKeyInInitialize
        {
            get;
            set;
        }
        /// <summary>
        /// Are Initiate Request and Response (AARQ and AARE) signed.
        /// </summary>
        /// <returns></returns>
        [DefaultValue(false)]
        public bool SignInitiateRequestResponse
        {
            get;
            set;
        }

        /// <summary>
        /// Used signing and ciphering order.
        /// </summary>
        public int SignCipherOrder
        {
            get;
            set;
        }

        /// <summary>
        /// Proposed Conformance.
        /// </summary>
        [Description("Proposed Conformance.")]
        public int Conformance
        {
            get;
            set;
        }

        /// <summary>
        /// FLAG ID.
        /// </summary>
        [Description("FLAG ID.")]
        [StringLength(3)]
        public string? Manufacturer
        {
            get;
            set;
        }

        /// <summary>
        /// What HDLC Addressing is used.
        /// </summary>
        public int HDLCAddressing
        {
            get;
            set;
        }

        /// <summary>
        /// Serial number formula.
        /// </summary>
        public string? SerialNumberFormula
        {
            get;
            set;
        }


        /// <summary>
        /// If protected release is used release is including a ciphered xDLMS Initiate request.
        /// </summary>
        public bool UseProtectedRelease
        {
            get;
            set;
        }

        /// <summary>
        /// Is Logical name referencing used.
        /// </summary>
        public bool UseLogicalNameReferencing
        {
            get;
            set;
        }
        /// <summary>
        /// Supporterd interfaces.
        /// </summary>
        [Description("Supporterd interfaces.")]
        public int SupporterdInterfaces
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDLMSSettings()
        {
            PduSize = 0xFFFF;
            UserId = -1;
            MaxInfoRX = MaxInfoTX = 128;
            WindowSizeRX = WindowSizeTX = 1;
            MACSourceAddress = 0xC00;
            ChallengeSize = 16;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is GXDLMSSettings source)
            {
                return MaximumBaudRate == source.MaximumBaudRate &&
                Authentication == source.Authentication &&
                AuthenticationName == source.AuthenticationName &&
                Standard == source.Standard &&
                Password == source.Password &&
                HexPassword == source.HexPassword &&
                Security == source.Security &&
                ClientSystemTitle == source.ClientSystemTitle &&
                DeviceSystemTitle == source.DeviceSystemTitle &&
                DedicatedKey == source.DedicatedKey &&
                PreEstablished == source.PreEstablished &&
                BlockCipherKey == source.BlockCipherKey &&
                AuthenticationKey == source.AuthenticationKey &&
                InvocationCounter == source.InvocationCounter &&
                FrameCounter == source.FrameCounter &&
                Challenge == source.Challenge &&
                Profiles.Equals(source.Profiles) &&
                UtcTimeZone == source.UtcTimeZone &&
                UseRemoteSerial == source.UseRemoteSerial &&
                MaxInfoTX == source.MaxInfoTX &&
                MaxInfoRX == source.MaxInfoRX &&
                WindowSizeTX == source.WindowSizeTX &&
                WindowSizeRX == source.WindowSizeRX &&
                PduSize == source.PduSize &&
                UserId == source.UserId &&
                NetworkId == source.NetworkId &&
                InactivityTimeout == source.InactivityTimeout &&
                ServiceClass == source.ServiceClass &&
                Priority == source.Priority &&
                ServerAddressSize == source.ServerAddressSize &&
                Conformance == source.Conformance &&
                Manufacturer == source.Manufacturer &&
                HDLCAddressing == source.HDLCAddressing &&
                UseLogicalNameReferencing == source.UseLogicalNameReferencing &&
                UseProtectedRelease == source.UseProtectedRelease;
            }
            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
