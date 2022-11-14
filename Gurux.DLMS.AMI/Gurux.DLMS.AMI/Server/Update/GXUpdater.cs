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

using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Update;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Scheduler
{
    /// <summary>
    /// Updater checks if there are modules or agents updates available.
    /// </summary>
    internal class GXUpdater : IGXCronTask
    {
        private readonly IModuleRepository _moduleRepository;
        private readonly IAgentRepository _agentRepository;
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXUpdater(IModuleRepository moduleRepository,
            IAgentRepository agentRepository)
        {
            _moduleRepository = moduleRepository;
            _agentRepository = agentRepository;
        }

        private static void AddModuleVersion(Shared.DTOs.GXModule module, GXVersion version)
        {
            var ver = new Shared.DTOs.GXModuleVersion();
            ver.Module = module;
            ver.CreationTime = version.CreationTime;
            ver.Number = version.Number;
            ver.FileName = version.FileName;
            ver.Prerelease = version.Prerelease;
            ver.Description = version.Description;
            module.Versions.Add(ver);
            module.AvailableVersion = version.Number;
        }

        private static void AddAgentVersion(Shared.DTOs.GXAgent agent, GXVersion version)
        {
            var ver = new Shared.DTOs.GXAgentVersion();
            ver.Agent = agent;
            ver.CreationTime = version.CreationTime;
            ver.Number = version.Number;
            ver.FileName = version.FileName;
            ver.Prerelease = version.Prerelease;
            ver.Description = version.Description;
            agent.Versions.Add(ver);
            agent.AvailableVersion = version.Number;
        }

        /// <summary>
        /// Check new updates.
        /// </summary>
        public async Task RunAsync(ClaimsPrincipal user)
        {
            var inatalledModules = await _moduleRepository.ListWithVersionsAsync(user);
            var installerAgents = await _agentRepository.ListInstallersAsync(user, null, null);

            //Check modules.
            string address = "/ami4/modules/modules.json";
            new HttpClient();
            GXModule[]? availableModules = await GetAsync<GXModule[]>(address);
            List<Shared.DTOs.GXModule> newModules = new List<Shared.DTOs.GXModule>();
            if (availableModules != null)
            {
                foreach (var module in availableModules)
                {
                    Shared.DTOs.GXModule? mod = inatalledModules.Where(w => w.Id == module.Name).SingleOrDefault();
                    if (mod != null)
                    {
                        //Installed module.

                        //Check if url has changed.
                        bool updated = mod.Url != module.Url;
                        mod.Url = module.Url;
                        foreach (var version in module.Versions)
                        {
                            var installedVersion = mod.Versions.Where(q => q.Number == version.Number).SingleOrDefault();
                            if (installedVersion == null)
                            {
                                updated = true;
                                AddModuleVersion(mod, version);
                            }
                        }
                        if (updated)
                        {
                            await _moduleRepository.UpdateAsync(user, mod);
                        }
                    }
                    else
                    {
                        //If new module.
                        mod = new Shared.DTOs.GXModule();
                        mod.Active = false;
                        mod.Status = ModuleStatus.Installable;
                        mod.Id = module.Name;
                        mod.Url = module.Url;
                        foreach (var version in module.Versions)
                        {
                            AddModuleVersion(mod, version);
                        }
                        newModules.Add(mod);
                    }
                }
            }
            if (newModules.Any())
            {
                await _moduleRepository.AddAsync(user, newModules);
            }

            //Check agents and update agent templates.
            address = "/ami4/agent/agent.json";
            GXAgent? loadAgent = await GetAsync<GXAgent>(address);
            List<Shared.DTOs.GXAgent> newAgents = new List<Shared.DTOs.GXAgent>();
            if (loadAgent != null && loadAgent.Versions.Any() && installerAgents.Any())
            {
                foreach (var version in loadAgent.Versions)
                {
                    foreach (var a in installerAgents)
                    {
                        var installedVersion = a.Versions.Where(q => q.Number == version.Number).SingleOrDefault();
                        if (installedVersion == null)
                        {
                            Shared.DTOs.GXAgent agent;
                            if (!newAgents.Any())
                            {
                                agent = await _agentRepository.ReadAsync(user, a.Id);
                                //Add agent only once.
                                newAgents.Add(agent);
                            }
                            else
                            {
                                agent = a;
                            }
                            AddAgentVersion(agent, version);
                        }
                    }
                }
            }
            else
            {
                Shared.DTOs.GXAgent a = new Shared.DTOs.GXAgent();
                a.Template = true;
                a.Name = loadAgent.Name;
                if (a.Name == null)
                {
                    a.Name = "DLMS template";
                }
                a.Url = loadAgent.Url + "/" + loadAgent.FolderName;
                foreach (var version in loadAgent.Versions)
                {
                    AddAgentVersion(a, version);
                }
                newAgents.Add(a);
            }
            if (newAgents.Any())
            {
                await _agentRepository.UpdateAsync(user, newAgents);
                //Propose agents to use the latest version.
                string newVersion = newAgents.LastOrDefault().Versions.LastOrDefault().Number;
                var agents = await _agentRepository.ListAsync(user, null, null, CancellationToken.None);
                foreach (var agent in agents)
                {
                    agent.AvailableVersion = newVersion;
                }
                await _agentRepository.UpdateAsync(user, agents, u => u.AvailableVersion);
            }
        }

        public static async Task<T?> GetAsync<T>(string url)
        {
            var http = new HttpClient();
            http.BaseAddress = new Uri("https://www.gurux.fi");
            HttpResponseMessage response = await http.GetAsync(url + "?" + Guid.NewGuid());
            ClientHelpers.ValidateStatusCode(response);
            return await response.Content.ReadFromJsonAsync<T>();
        }
    }
}
