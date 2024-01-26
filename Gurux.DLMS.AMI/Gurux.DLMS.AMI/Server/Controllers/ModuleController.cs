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
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Server.Cron;
using System.Diagnostics;
using Gurux.DLMS.AMI.Shared.DTOs.Module;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the module settings.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ModuleController : ControllerBase
    {
        private readonly IGXHost host;
        private readonly CancellationToken _cancellationToken;
        private readonly IGXModuleService _moduleService;
        private readonly IModuleRepository _moduleRepository;
        private readonly IGXCronTask _cron;
        private readonly IModuleLogRepository _moduleLogRepository;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ModuleController(IGXHost value,
            IHostApplicationLifetime applicationLifetime,
             IGXModuleService moduleService,
             IModuleRepository moduleRepository,
             IGXCronTask cron,
             IModuleLogRepository moduleLogRepository)
        {
            host = value;
            _moduleRepository = moduleRepository;
            _cancellationToken = applicationLifetime.ApplicationStopping;
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
                Item = await _moduleRepository.ReadAsync(User, id)
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
                Item = await _moduleRepository.ReadAsync(User, id)
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
                Item = await _moduleRepository.ReadAsync(User, id)
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
            await _moduleRepository.ListAsync(User, request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Get module configuration UI.
        /// </summary>
        [HttpPost("ModuleUI")]
        [Authorize(Policy = GXModulePolicies.View)]
        public async Task<ActionResult<ModuleConfigurationUIResponse>> Post(ModuleConfigurationUI request)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXModule>(where => where.Id.Equals(request.Name));
            arg.Columns.Add<GXModuleAssembly>();
            arg.Joins.AddInnerJoin<GXModule, GXModuleAssembly>(j => j.Id, j => j.Module);
            GXModule module = (await host.Connection.SingleOrDefaultAsync<GXModule>(arg));
            if (module == null)
            {
                return BadRequest("Unknown module. " + request.Name);
            }
            ListModulesResponse ret = new ListModulesResponse();
            List<string> list = await GetModuleUI(module);
            return new ModuleConfigurationUIResponse() { Modules = list.ToArray() };
        }

        /// <summary>
        /// Get module configuration UI.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXModulePolicies.Delete)]
        public async Task<ActionResult<RemoveModuleResponse>> Post(RemoveModule request)
        {
            RemoveModuleResponse ret = new RemoveModuleResponse();
            foreach (var it in request.Modules)
            {
                ret.Restart |= _moduleService.DeleteModule(User, new GXModule() { Id = it });
            }
            await _moduleRepository.DeleteAsync(User, request.Modules);
            return ret;
        }


        /// <summary>
        /// Load module modules.
        /// </summary>
        /// <returns></returns>
        private async Task<List<string>> GetModuleUI(GXModule module)
        {
            if (string.IsNullOrEmpty(module.Version))
            {
                throw new ArgumentException("Invalid version.");
            }
            string path2;
            byte[] bytes;
            List<string> list = new List<string>();
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Modules");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            //Load shared files.
            foreach (var it in module.Assemblies)
            {
                path2 = Path.Combine(path, module.Id, module.Version, it.FileName);
                try
                {
                    bytes = await System.IO.File.ReadAllBytesAsync(path2, _cancellationToken);
                    list.Add(Convert.ToBase64String(bytes));
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return list;
        }

        /// <summary>
        /// Add new custom module.
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        [HttpPost("Add")]
        [Authorize(Policy = GXModulePolicies.Add)]
        public async Task<ActionResult<AddModuleResponse>> PostFile([FromForm] IEnumerable<IFormFile> files)
        {
            string zip = Path.GetTempFileName();
            List<GXModule> modules = new List<GXModule>();
            AddModuleResponse res = new AddModuleResponse();
            foreach (var file in files)
            {
                try
                {
                    // Save the file
                    using (var fileStream = new FileStream(zip, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    GXModule module = await _moduleService.AddModuleAsync(User, zip, file.FileName);
                    modules.Add(module);
                    //Enable module.
                    res.Restart |= _moduleService.EnableModule(User, module);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            res.Modules = modules.ToArray();
            return res;
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
            GXModule module = await _moduleRepository.ReadAsync(User, request.Module.Id);
            //Update status.
            request.Module.Status = module.Status;
            await _moduleRepository.UpdateAsync(User, request.Module);
            // Check is active state changed.
            if (request.Module.Active.HasValue && module.Active != request.Module.Active)
            {
                if (request.Module.Active.Value)
                {
                    _moduleService.EnableModule(User, module);
                }
                else
                {
                    _moduleService.DisableModule(User, request.Module);
                }
            }
            //Update module settings.
            _moduleService.UpdateModuleSettings(request.Module);
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
            GXModule? module = await _moduleRepository.ReadAsync(User, request.Module.Id);
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
            ClientHelpers.ValidateStatusCode(response);
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
                    ret.Restart |= _moduleService.DeleteModule(User, module);
                    await _moduleRepository.DeleteAsync(User, new string[] { module.Id });
                }
                GXModule? module2 = await _moduleService.AddModuleAsync(User, zip, version.FileName);
                module2.AvailableVersion = module.AvailableVersion;
                await _moduleRepository.UpdateAsync(User, module2);
                //Add log.
                await _moduleLogRepository.AddAsync(User, new GXModuleLog[]
                {
                    new GXModuleLog(TraceLevel.Info)
                    {
                        CreationTime = DateTime.Now,
                        Module = module,
                        Message = Properties.Resources.ModuleInstalled
                    }
                });
                ret.Restart |= _moduleService.EnableModule(User, module2);
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
            await _cron.CheckModulesAsync(User);
            return new CheckModuleResponse();
        }
    }
}
