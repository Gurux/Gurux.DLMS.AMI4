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
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>
    /// This class is used to check is client notified.
    /// </summary>
    public class GXPerformanceSettings
    {
        private string[]? IgnoreNotification;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXPerformanceSettings(IGXHost host, IConfigurationRepository configurationRepository)
        {
            var User = ServerSettings.GetDefaultAdminUser(host);
            if (User != null)
            {
                ListConfiguration req = new ListConfiguration() { Filter = new GXConfiguration() { Name = GXConfigurations.Performance } };
                GXConfiguration[] confs = configurationRepository.ListAsync(req, null, CancellationToken.None).Result;
                if (confs.Length == 1 && confs[0].Settings is string settings)
                {
                    try
                    {
                        IgnoreNotification = JsonSerializer.Deserialize<PerformanceSettings>(settings)?.IgnoreNotification;
                    }
                    catch (Exception)
                    {
                        //Ignore notification is changed from UInt64 to string.
                    }
                }
            }
            configurationRepository.Updated += (configurations) =>
            {
                //If maintenance configuration is updated.
                foreach (GXConfiguration it in configurations)
                {
                    if (it.Name == GXConfigurations.Performance && it.Settings != null)
                    {
                        IgnoreNotification = JsonSerializer.Deserialize<PerformanceSettings>(it.Settings)?.IgnoreNotification;
                        break;
                    }
                }
            };
        }

        /// <summary>
        /// Is client notified from the event.
        /// </summary>
        /// <param name="type">Notification type.</param>
        /// <returns></returns>
        public bool Notification(string type)
        {
            return IgnoreNotification == null ||
                !IgnoreNotification.Contains(type);
        }
    }
}
