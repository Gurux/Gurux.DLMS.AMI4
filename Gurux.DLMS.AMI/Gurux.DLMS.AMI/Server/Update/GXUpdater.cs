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
using Gurux.DLMS.AMI.Client.Pages.Manufacturer;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Update;
using System.Security.Claims;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Scheduler
{
    /// <summary>
    /// Updater checks if there are modules or agents updates available.
    /// </summary>
    internal class GXUpdater : IGXCronTask
    {
        private readonly IModuleRepository _moduleRepository;
        private readonly IAgentRepository _agentRepository;
        private readonly ISystemLogRepository _systemLogRepository;
        private readonly IManufacturerRepository _manufacturerRepository;
        private readonly IConfigurationRepository _configurationRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXUpdater(IModuleRepository moduleRepository,
            IAgentRepository agentRepository,
            ISystemLogRepository systemLogRepository,
            IManufacturerRepository manufacturerRepository,
            IConfigurationRepository configurationRepository)
        {
            _moduleRepository = moduleRepository;
            _agentRepository = agentRepository;
            _systemLogRepository = systemLogRepository;
            _manufacturerRepository = manufacturerRepository;
            _configurationRepository = configurationRepository;
        }

        private void AddModuleVersion(ClaimsPrincipal user, Shared.DTOs.GXModule module, Update.GXVersion version)
        {
            var ver = new Shared.DTOs.GXModuleVersion();
            ver.Module = module;
            ver.CreationTime = version.CreationTime;
            ver.Number = version.Number;
            ver.Url = version.Url;
            ver.FileName = version.FileName;
            ver.Prerelease = version.Prerelease;
            ver.Description = version.Description;
            module.Versions.Add(ver);
            try
            {
                if (string.IsNullOrEmpty(module.AvailableVersion) ||
                    new Version(module.AvailableVersion) < new Version(version.Number))
                {
                    module.AvailableVersion = version.Number;
                    module.NewVersion = true;
                }
            }
            catch (Exception ex)
            {
                _systemLogRepository.AddAsync(user, ex).Wait();
            }
        }

        private static void AddAgentVersion(Shared.DTOs.GXAgent agent, Update.GXVersion version)
        {
            var ver = new Shared.DTOs.GXAgentVersion();
            ver.Agent = agent;
            ver.CreationTime = version.CreationTime;
            ver.Number = version.Number;
            ver.Url = version.Url;
            ver.FileName = version.FileName;
            ver.Prerelease = version.Prerelease;
            ver.Description = version.Description;
            if (agent.Versions == null)
            {
                agent.Versions = new List<Shared.DTOs.GXAgentVersion>();
            }
            agent.Versions.Add(ver);
            agent.AvailableVersion = version.Number;
        }

        /// <summary>
        /// Check new agent versions.
        /// </summary>
        /// <param name="user">Current user.</param>
        public async Task CheckAgentsAsync(ClaimsPrincipal user)
        {
            var installerAgents = await _agentRepository.ListInstallersAsync(user, null, true, null);
            //Check agents and update agent templates.
            string address = "/ami4/agent/agent.json";
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
                Shared.DTOs.GXAgent a = new Shared.DTOs.GXAgent(loadAgent.Name);
                a.Template = true;
                if (a.Name == null)
                {
                    a.Name = "DLMS agent";
                }
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
            //Update check time.
            ListConfiguration req = new ListConfiguration()
            {
                Filter = new
                Shared.DTOs.GXConfiguration()
                { Name = GXConfigurations.Agents }
            };
            var confs = _configurationRepository.ListAsync(user, req, null, CancellationToken.None).Result;
            foreach (var conf in confs)
            {
                if (conf.Name == GXConfigurations.Agents)
                {
                    AgentSettings? s = null;
                    if (!string.IsNullOrEmpty(conf.Settings))
                    {
                        s = JsonSerializer.Deserialize<AgentSettings>(conf.Settings);
                    }
                    if (s == null)
                    {
                        s = new AgentSettings();
                    }
                    s.Checked = DateTime.Now;
                    conf.Settings = JsonSerializer.Serialize(s);
                    _configurationRepository.UpdateAsync(user,
                        new Shared.DTOs.GXConfiguration[] { conf }, true).Wait();
                    break;
                }
            }
        }

        /// <summary>
        /// Check new modules.
        /// </summary>
        /// <param name="user">Current user.</param>
        public async Task CheckModulesAsync(ClaimsPrincipal user)
        {
            DateTime now = DateTime.Now;
            var inatalledModules = await _moduleRepository.ListWithVersionsAsync(user);
            //Check modules.
            string address = "/ami4/modules/modules.json";
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
                        bool updated = false;
                        foreach (var version in module.Versions)
                        {
                            var installedVersion = mod.Versions.Where(q => q.Number == version.Number).SingleOrDefault();
                            if (installedVersion == null)
                            {
                                updated = true;
                                AddModuleVersion(user, mod, version);
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
                        mod = new Shared.DTOs.GXModule(module.Name);
                        mod.CreationTime = now;
                        mod.Active = false;
                        mod.Status = ModuleStatus.Installable;
                        foreach (var version in module.Versions)
                        {
                            AddModuleVersion(user, mod, version);
                        }
                        newModules.Add(mod);
                    }
                }
            }
            if (newModules.Any())
            {
                await _moduleRepository.AddAsync(user, newModules);
            }
            //Update check time.
            ListConfiguration req = new ListConfiguration()
            {
                Filter = new
                Shared.DTOs.GXConfiguration()
                { Name = GXConfigurations.Modules }
            };
            var confs = _configurationRepository.ListAsync(user, req, null, CancellationToken.None).Result;
            foreach (var conf in confs)
            {
                if (conf.Name == GXConfigurations.Modules)
                {
                    ModuleSettings? s = null;
                    if (!string.IsNullOrEmpty(conf.Settings))
                    {
                        s = JsonSerializer.Deserialize<ModuleSettings>(conf.Settings);
                    }
                    if (s == null)
                    {
                        s = new ModuleSettings();
                    }
                    s.Checked = DateTime.Now;
                    conf.Settings = JsonSerializer.Serialize(s);
                    _configurationRepository.UpdateAsync(user,
                        new Shared.DTOs.GXConfiguration[] { conf }, true).Wait();
                    break;
                }
            }
        }

        /// <summary>
        /// Check manufacturer settings.
        /// </summary>
        /// <param name="user">Current user.</param>
        public async Task CheckManufacturersAsync(ClaimsPrincipal user)
        {
            List<GXManufacturer> installedManufacturers = new List<GXManufacturer>();
            ListManufacturers request = new ListManufacturers()
            {
                //Get all template manufacturers.
                AllUsers = true,
                Filter = new GXManufacturer()
                {
                    Template = true
                },
                Select = Shared.Enums.TargetType.Version
            };
            installedManufacturers.AddRange(await _manufacturerRepository.ListAsync(user, request, null, CancellationToken.None));
            //Check manufacturers.
            string address = "/ami4/manufacturers/manufacturers.json";
            GXManufacturer[]? availableManufacturers = await GetAsync<GXManufacturer[]>(address);
            List<GXManufacturer> updatedManufacturers = new List<GXManufacturer>();
            if (availableManufacturers != null)
            {
                //Available manufacturer.
                foreach (var aManufacturer in availableManufacturers)
                {
                    //Installed manufacturer.
                    GXManufacturer? iManufacturer = installedManufacturers.Where(w => w.Name == aManufacturer.Name).SingleOrDefault();
                    if (iManufacturer == null)
                    {
                        aManufacturer.Template = true;
                        //If new manufacturer.
                        updatedManufacturers.Add(aManufacturer);
                    }
                    else if (aManufacturer.Models != null)
                    {
                        //Check if there are new models.
                        foreach (var aModel in aManufacturer.Models)
                        {
                            //Installed model.
                            GXDeviceModel? iModel = iManufacturer.Models?.Where(w => w.Name == aModel.Name).SingleOrDefault();
                            if (iModel == null)
                            {
                                //If new model.
                                if (iManufacturer.Models == null)
                                {
                                    iManufacturer.Models = new List<GXDeviceModel>();
                                }
                                iManufacturer.Models.Add(aModel);
                                updatedManufacturers.Add(iManufacturer);
                            }
                            else if (aModel.Versions != null)
                            {
                                //Check if there are new versions.
                                foreach (var aVersion in aModel.Versions)
                                {
                                    //Installed manufacturer.
                                    GXDeviceVersion? iVersion = iModel.Versions?.Where(w => w.Name == aVersion.Name).SingleOrDefault();
                                    if (iVersion == null)
                                    {
                                        //If new version.
                                        if (iModel.Versions == null)
                                        {
                                            iModel.Versions = new List<GXDeviceVersion>();
                                        }
                                        iModel.Versions.Add(aVersion);
                                        updatedManufacturers.Add(iManufacturer);
                                    }
                                    else if (aVersion.Settings != null)
                                    {
                                        //Check if there are new settings.
                                        foreach (var aSettings in aVersion.Settings)
                                        {
                                            var iSetting = iVersion.Settings?.Where(w => string.Compare(w.Location, aSettings.Location, true) == 0).SingleOrDefault();
                                            if (iSetting == null)
                                            {
                                                //If new setting.
                                                if (iVersion.Settings == null)
                                                {
                                                    iVersion.Settings = new List<GXDeviceSettings>();
                                                }
                                                iVersion.Settings.Add(aSettings);
                                                updatedManufacturers.Add(iManufacturer);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //Add new settings.
                                        iVersion.Settings = aVersion.Settings;
                                        updatedManufacturers.Add(iManufacturer);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (updatedManufacturers.Any())
            {
                await _manufacturerRepository.UpdateAsync(user, updatedManufacturers);
            }
            //Update check time.
            ListConfiguration req = new ListConfiguration()
            {
                Filter = new
                Shared.DTOs.GXConfiguration()
                { Name = GXConfigurations.Manufacturers }
            };
            var confs = _configurationRepository.ListAsync(user, req, null, CancellationToken.None).Result;
            foreach (var conf in confs)
            {
                if (conf.Name == GXConfigurations.Manufacturers)
                {
                    Client.Shared.ManufacturerSettings? s = null;
                    if (!string.IsNullOrEmpty(conf.Settings))
                    {
                        s = JsonSerializer.Deserialize<Client.Shared.ManufacturerSettings>(conf.Settings);
                    }
                    if (s == null)
                    {
                        s = new Client.Shared.ManufacturerSettings();
                    }
                    s.Checked = DateTime.Now;
                    conf.Settings = JsonSerializer.Serialize(s);
                    _configurationRepository.UpdateAsync(user,
                        new Shared.DTOs.GXConfiguration[] { conf }, true).Wait();
                    break;
                }
            }
        }

        /// <summary>
        /// Check new updates.
        /// </summary>
        public async Task RunAsync(ClaimsPrincipal user)
        {
            await CheckModulesAsync(user);
            await CheckAgentsAsync(user);
            await CheckManufacturersAsync(user);
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
