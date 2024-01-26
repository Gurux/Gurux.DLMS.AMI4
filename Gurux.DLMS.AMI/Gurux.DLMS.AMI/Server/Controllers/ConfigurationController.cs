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
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Server.Cron;
using System.Diagnostics;
using System.Reflection;
using Gurux.DLMS.AMI.Shared.DTOs.Script;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the system configurations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfigurationRepository _configurationRepository;
        private readonly IScriptRepository _scriptRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime;
        /// <summary>
        /// Constructor.
        /// </summary>
        public ConfigurationController(IConfigurationRepository configurationRepository,
            IScriptRepository scriptRepository,
            IHostApplicationLifetime applicationLifetime,
            IServiceProvider serviceProvider)
        {
            _configurationRepository = configurationRepository;
            _serviceProvider = serviceProvider;
            _scriptRepository = scriptRepository;
            _applicationLifetime = applicationLifetime;
        }

        /// <summary>
        /// Get system version number.
        /// </summary>
        [HttpGet("Version")]
        [AllowAnonymous]
        public ActionResult<string?> Get()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(asm.Location);
            return info.FileVersion;
        }

        /// <summary>
        /// Get configuration information.
        /// </summary>
        /// <param name="id">Configuration id.</param>
        /// <returns>Configuration information.</returns>
        [HttpGet]
        [Authorize(Policy = GXConfigurationPolicies.View)]

        public async Task<ActionResult<GetConfigurationResponse>> Get(Guid id)
        {
            return new GetConfigurationResponse()
            {
                Item = await _configurationRepository.ReadAsync(User, id, null)
            };
        }

        /// <summary>
        /// List configuration information.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Configuration settings.</returns>
        [HttpPost("List")]
        [Authorize(Policy = GXConfigurationPolicies.View)]
        public async Task<ActionResult<ListConfigurationResponse>> Post(
            ListConfiguration request,
            CancellationToken cancellationToken)
        {
            ListConfigurationResponse ret = new ListConfigurationResponse();
            await _configurationRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Update configuration values.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXConfigurationPolicies.Add)]
        public async Task<ActionResult<ConfigurationUpdateResponse>> Post(ConfigurationUpdate request)
        {
            if (request.Configurations == null || !request.Configurations.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _configurationRepository.UpdateAsync(User, request.Configurations, true);
            return new ConfigurationUpdateResponse();
        }

        /// <summary>
        /// Run cron.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("Cron")]
        [Authorize(Policy = GXConfigurationPolicies.Cron)]
        public async Task<ActionResult<ConfigurationRunCronResponse>> Post(ConfigurationRunCron request)
        {
            IGXCronService cronService = _serviceProvider.GetRequiredService<IGXCronService>();
            await cronService.RunAsync(User);
            return new ConfigurationRunCronResponse();
        }


        /// <summary>
        /// Get configuration version information.
        /// </summary>
        /// <param name="request">Configuration request.</param>
        /// <returns>Configuration Versions.</returns>
        [Authorize(Policy = GXConfigurationPolicies.View)]
        [HttpPost("Assemblies")]

        public async Task<ActionResult<LoadedAssembliesResponse>> Post(LoadedAssemblies request)
        {
            for (int i = 0; i != 10; ++i)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            List<GXAssembly> list = new List<GXAssembly>();
            string? filterName = null;
            if (request != null && request.Filter != null && !string.IsNullOrEmpty(request.Filter.Name))
            {
                filterName = request.Filter.Name.ToLower();
            }
            //Find module from loaded assemblies.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (assembly.GetName() == null || assembly.IsDynamic)
                    {
                        continue;
                    }
                    string? name = assembly.GetName().Name;
                    if (name == null)
                    {
                        continue;
                    }
                    string? version = Convert.ToString(assembly.GetName().Version);
                    if (version == null)
                    {
                        continue;
                    }
                    bool isScript = false;
                    if (string.IsNullOrEmpty(assembly.Location) &&
                        Guid.TryParse(name, out Guid id)
                        && id != Guid.Empty)
                    {
                        //If this is a script.
                        GXScript script = await _scriptRepository.ReadAsync(User, id);
                        if (script != null && !string.IsNullOrEmpty(script.Name))
                        {
                            name = script.Name;
                            isScript = true;
                        }
                    }
                    if (request != null && request.Filter != null)
                    {
                        if (filterName != null)
                        {
                            if (!name.ToLower().Contains(filterName))
                            {
                                continue;
                            }
                        }
                        if (!string.IsNullOrEmpty(request.Filter.Version))
                        {
                            if (!version.Contains(request.Filter.Version))
                            {
                                continue;
                            }
                        }
                        if (request.Filter.IsScript != null)
                        {
                            //If only scripts are shown.
                            if (request.Filter.IsScript != isScript)
                            {
                                continue;
                            }
                        }
                    }

                    list.Add(new GXAssembly()
                    {
                        Name = name,
                        Version = version,
                        Location = assembly.Location,
                        IsScript = isScript
                    });
                }
                catch (Exception)
                {
                    //It's OK if this fails.
                }
            }
            return new LoadedAssembliesResponse()
            {
                Assemblies = list.OrderBy(x => x.Name).ToArray()
            };
        }

        /// <summary>
        /// Restart application.
        /// </summary>
        /// <returns>Configuration Versions.</returns>
        [Authorize(Policy = GXConfigurationPolicies.Edit)]
        [HttpPost("Restart")]
        public ActionResult Post(StopApplicationRequest request)
        {
            _applicationLifetime.StopApplication();
            return Ok();
        }
    }
}
