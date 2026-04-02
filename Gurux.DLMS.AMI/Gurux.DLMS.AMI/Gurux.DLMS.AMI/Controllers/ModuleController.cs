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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Server.Cron;
using System.Diagnostics;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Module;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the module settings.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ModuleController : ControllerBase
    {
        private readonly IGXModuleService _moduleService;
        private readonly IModuleRepository _moduleRepository;
        private readonly IGXCronTask _cron;
        private readonly IModuleLogRepository _moduleLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModuleController(
             IGXModuleService moduleService,
             IModuleRepository moduleRepository,
             IGXCronTask cron,
             IModuleLogRepository moduleLogRepository)
        {
            _moduleRepository = moduleRepository;
            _moduleService = moduleService;
            _cron = cron;
            _moduleLogRepository = moduleLogRepository;
        }

        /// <summary>
        /// Get module information.
        /// </summary>
        /// <param name="id">Module id.</param>
        /// <returns>Module information.</returns>
        [HttpGet]
        [Authorize(Policy = GXModulePolicies.View)]
        public async Task<ActionResult<GetModuleResponse>> Get(string id)
        {
            return new GetModuleResponse()
            {
                Item = await _moduleRepository.ReadAsync(id)
            };
        }

        /// <summary>
        /// Get user module information.
        /// </summary>
        /// <param name="id">Module id.</param>
        /// <returns>Module information for the user.</returns>
        [HttpGet("user/{id}")]
        [Authorize(Policy = GXModulePolicies.View)]
        public async Task<ActionResult<GetModuleResponse>> GetUser(string id)
        {
            return new GetModuleResponse()
            {
                Item = await _moduleRepository.ReadAsync(id)
            };
        }

        /// <summary>
        /// Get schedule module information.
        /// </summary>
        /// <param name="id">Module id.</param>
        /// <returns>Module information for the schedule.</returns>
        [HttpGet("schedule/{id}")]
        [Authorize(Policy = GXModulePolicies.View)]
        public async Task<ActionResult<GetModuleResponse>> GetSchedule(string id)
        {
            return new GetModuleResponse()
            {
                Item = await _moduleRepository.ReadAsync(id)
            };
        }

        /// <summary>
        /// Get installed modules.
        /// </summary>
        [HttpPost("List")]
        [Authorize(Policy = GXModulePolicies.View)]
        public async Task<ActionResult<ListModulesResponse>> Post(
            ListModules request,
            CancellationToken cancellationToken)
        {
            ListModulesResponse response = new ListModulesResponse();
            await _moduleRepository.ListAsync(request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Delete module.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXModulePolicies.Delete)]
        public async Task<ActionResult<RemoveModuleResponse>> Post(RemoveModule request)
        {
            RemoveModuleResponse ret = new RemoveModuleResponse();
            foreach (var it in request.Modules)
            {
                ret.Restart |= _moduleService.DeleteModule(new GXModule() { Id = it });
            }
            await _moduleRepository.DeleteAsync(request.Modules);
            return ret;
        }

        /// <summary>
        /// Add new custom module.
        /// </summary>
        /// <returns></returns>
        [HttpPost("Add")]
        [Authorize(Policy = GXModulePolicies.Add)]
        public async Task<ActionResult<AddModuleResponse>> AddRaw()
        {
            string zip = Path.GetTempFileName();
            AddModuleResponse res = new AddModuleResponse();
            try
            {
                using var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms);
                // Save the file
                System.IO.File.WriteAllBytes(zip, ms.ToArray());
                List<GXModule> modules = await _moduleService.AddModuleAsync(User, zip);
                res.Modules = modules.ToArray();
                return res;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Upload module.
        /// </summary>
        [HttpPost("Update")]
        [Authorize(Policy = GXModulePolicies.Edit)]
        public async Task<ActionResult<UpdateModuleResponse>> Post(UpdateModule request)
        {
            if (request.Module == null)
            {
                return BadRequest("Unknown target.");
            }
            UpdateModuleResponse ret = new UpdateModuleResponse();
            GXModule module = await _moduleRepository.ReadAsync(request.Module.Id);
            //Update status.
            request.Module.Status = module.Status;
            await _moduleRepository.UpdateAsync(request.Module);
            // Check is active state changed.
            if (request.Module.Active.HasValue && module.Active != request.Module.Active)
            {
                if (request.Module.Active.Value)
                {
                    await _moduleService.EnableModuleAsync(User, module);
                }
                else
                {
                    await _moduleService.DisableModuleAsync(User, request.Module);
                }
            }
            //Update module settings.
            await _moduleService.UpdateModuleSettingsAsync(request.Module);
            return ret;
        }

        /// <summary>
        /// Install module.
        /// </summary>
        [HttpPost("Install")]
        [Authorize(Policy = GXModulePolicies.Add)]
        public async Task<ActionResult<InstallModuleResponse>> Post(InstallModule request)
        {
            if (request.Module == null)
            {
                return BadRequest("Invalid target.");
            }
            if (string.IsNullOrEmpty(request.Module.Id))
            {
                return BadRequest("Invalid Url.");
            }
            GXModule? module = await _moduleRepository.ReadAsync(request.Module.Id);
            GXModuleVersion? version;
            if (string.IsNullOrEmpty(request.Module.Version))
            {
                //Install the latest version.
                version = module.Versions.Where(s => s.Number == module.AvailableVersion).SingleOrDefault();
            }
            else
            {
                //Install proposed version.
                version = module.Versions.Where(s => s.Number == request.Module.Version).SingleOrDefault();
            }
            if (version == null)
            {
                return BadRequest("Invalid version.");
            }
            //Load module from url.
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(version.Url);
            await AMI.Shared.Helpers.ValidateStatusCode(response, default);
            string zip = Path.GetTempFileName();
            InstallModuleResponse ret = new InstallModuleResponse();
            try
            {
                // Save the file
                using (var fileStream = new FileStream(zip, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
                if ((module.Status & Shared.DTOs.Enums.ModuleStatus.Installed) != 0)
                {
                    //Remove old version.
                    ret.Restart |= _moduleService.DeleteModule(module);
                    await _moduleRepository.DeleteAsync([module.Id]);
                }
                var module2 = (await _moduleService.AddModuleAsync(User, zip)).SingleOrDefault();
                module2.AvailableVersion = module.AvailableVersion;
                await _moduleRepository.UpdateAsync(module2);
                //Add log.
                await _moduleLogRepository.AddAsync(TargetType.Module, [
                new GXModuleLog(TraceLevel.Info)
                    {
                        CreationTime = DateTime.Now,
                        Module = module,
                        Message = Properties.Resources.ModuleInstalled
                    }
                ]);
                ret.Restart |= await _moduleService.EnableModuleAsync(User, module2);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return ret;
        }

        /// <summary>
        /// Check are there new module version available.
        /// </summary>
        [HttpPost("Check")]
        [Authorize(Policy = GXModulePolicies.Edit)]
        public async Task<ActionResult<CheckModuleResponse>> Post(
            CheckModule request)
        {
            await _cron.CheckModulesAsync();
            return new CheckModuleResponse();
        }
    }
}
