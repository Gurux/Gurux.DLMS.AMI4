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

using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Client.Development.ClockUpdater
{
    /// <summary>
    /// Clock updater module.
    /// </summary>
    public class ClockUpdaterModule : GXAmiModuleBase
    {
        /// <inheritdoc/>
        public override string Id
        {
            get
            {
                return "Gurux.Clock.Updater";
            }
        }

        /// <inheritdoc/>
        public override string Name
        {
            get
            {
                return "Gurux Clock Updater";
            }
        }

        /// <inheritdoc/>
        public override string Description
        {
            get
            {
                return "This module is used to update meters clock.";
            }
        }

        /// <inheritdoc/>
        public override string? Help
        {
            get
            {
                return "https://www.gurux.fi";
            }
        }

        /// <inheritdoc/>
        public override Type? Extension
        {
            get
            {
                return typeof(ClockUpdaterExtension);
            }
        }

        /// <inheritdoc/>
        public override Type? Schedule
        {
            get
            {
                return typeof(ClockUpdaterExtension);
            }
        }

        /// <inheritdoc/>
        public override bool CanSchedule
        {
            get
            {
                return true;
            }
        }

        [Parameter]
        public ClockUpdaterSettings Settings
        {
            get;
            set;
        } = new ClockUpdaterSettings();

        /// <inheritdoc/>
        public override async Task InstallAsync(ClaimsPrincipal user, IServiceProvider services, object module)
        {
            var roles = services.GetService<IRoleRepository>();
            if (roles != null)
            {
                GXRole role = new GXRole("ClockUpdater");
                GXScope scope1 = new GXScope("Add");
                GXScope scope2 = new GXScope("Run");
                role.Scopes.Add(scope1);
                role.Scopes.Add(scope2);
                await roles.AddAsync(user, new GXRole[] { role });
            }
        }

        /// <inheritdoc/>
        public override async Task ExecuteAsync(
            ClaimsPrincipal user,
            IServiceProvider services,
            string? settings,
            string? instanceSettings)
        {
            var tmp = JsonSerializer.Deserialize<ClockUpdaterSettings>(instanceSettings);
            if (tmp != null && tmp.DeviceGroups.Any())
            {
                List<GXTask> tasks = new List<GXTask>();
                foreach (var it in tmp.DeviceGroups)
                {
                    GXTask task = new GXTask();
                    task.TaskType = AMI.Shared.Enums.TaskType.Write;
                    task.DeviceGroup = new GXDeviceGroup()
                    {
                        Id = it
                    };
                    GXAttribute att = new GXAttribute(2, null);
                    task.Object = new((int)Enums.ObjectType.Clock, "0.0.1.0.0.255")
                    {
                        Attributes = new List<GXAttribute>() { att }
                    };
                    tasks.Add(task);
                }
                ITaskRepository? repository = services.GetService<ITaskRepository>();
                if (repository != null)
                {
                    await repository.AddAsync(user, tasks);
                }
            }
        }
    }
}
