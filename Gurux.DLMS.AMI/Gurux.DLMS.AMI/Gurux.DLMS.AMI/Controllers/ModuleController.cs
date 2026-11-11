// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Client;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Db;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// This controller is used to handle the module settings.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ModuleController(
         IGXModuleService moduleService,
         IModuleRepository moduleRepository,
         IGXBackgroundService cron,
         ILogRepository moduleLogRepository,
         ILogger<ModuleController>? logger = null) : ControllerBase
    {
        private readonly IGXModuleService _moduleService = moduleService;
        private readonly IModuleRepository _moduleRepository = moduleRepository;
        private readonly IGXBackgroundService _cron = cron;
        private readonly ILogRepository _moduleLogRepository = moduleLogRepository.ForSource("module");

        private async Task LogOperationFailureAsync(string operation, string? moduleId, Exception error)
        {
            try
            {
                // Server-generated diagnostics must not require the operator's module-log.add permission.
                var factory = HttpContext.RequestServices.GetRequiredService<IDbConnectionFactory>();
                await using var connection = await factory.GetHistoryConnectionAsync(CancellationToken.None);
                await GXLogStore.InsertAsync(connection, "module", [new GXLog(TraceLevel.Error)
                {
                    CreationTime = DateTimeOffset.UtcNow,
                    SourceId = moduleId,
                    Message = $"Failed to {operation} module{(moduleId == null ? "" : $" '{moduleId}'")}: {error.Message}",
                    StackTrace = error.ToString()
                }], CancellationToken.None);
            }
            catch (Exception logError)
            {
                // A logging failure must not replace the original operation result.
                logger?.LogError(logError, "Could not record module {Operation} failure for {ModuleId}. Original error: {Error}",
                    operation, moduleId, error.ToString());
            }
        }

        /// <summary>
        /// Get module information.
        /// </summary>
        /// <param name="id">Module id.</param>
        /// <returns>Module information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXModulePolicies.View)]
        public async Task<ActionResult<GetModuleResponse>> Get(string id, CancellationToken cancellationToken = default)
        {
            return new GetModuleResponse()
            {
                Item = await _moduleRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Get user module information.
        /// </summary>
        /// <param name="id">Module id.</param>
        /// <returns>Module information for the user.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet("user/{id}")]
        [Authorize(Policy = GXModulePolicies.View)]
        public async Task<ActionResult<GetModuleResponse>> GetUser(string id, CancellationToken cancellationToken = default)
        {
            return new GetModuleResponse()
            {
                Item = await _moduleRepository.ReadAsync(id, null, cancellationToken)
            };
        }

        /// <summary>
        /// Get schedule module information.
        /// </summary>
        /// <param name="id">Module id.</param>
        /// <returns>Module information for the schedule.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet("schedule/{id}")]
        [Authorize(Policy = GXModulePolicies.View)]
        public async Task<ActionResult<GetModuleResponse>> GetSchedule(string id, CancellationToken cancellationToken = default)
        {
            return new GetModuleResponse()
            {
                Item = await _moduleRepository.ReadAsync(id, null, cancellationToken)
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
            if (request.CatalogOnly)
            {
                long index = request.Index, count = request.Count;
                var latest = request.Filter?.AvailableVersion;
                request.Index = 0;
                request.Count = 0;
                request.Filter?.AvailableVersion = null;

                try
                {
                    var modules = await _moduleRepository.ListAsync(request, null, cancellationToken);
                    var database = HttpContext.RequestServices.GetRequiredService<Gurux.DLMS.AMI.Services.Database.IGXDatabase>();
                    return await new Gurux.DLMS.AMI.Server.Module.GXModuleCatalogQuery(database).ProjectAsync(modules, request.IncludePrereleases, latest, index, count, request.OrderBy, request.Descending, cancellationToken);
                }
                finally
                {
                    request.Index = index;
                    request.Count = count;
                    request.Filter?.AvailableVersion = latest;
                }
            }
            ListModulesResponse response = new();
            await _moduleRepository.ListAsync(request, response, cancellationToken);
            return response;
        }

        /// <summary>
        /// Delete module.
        /// </summary>
        [HttpPost("Delete")]
        [Authorize(Policy = GXModulePolicies.Delete)]
        public async Task<ActionResult<RemoveModuleResponse>> Post(RemoveModule request, CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined((ModuleDataRemoval)request.DataRemoval) || request.Modules == null || request.Modules.Any(string.IsNullOrWhiteSpace))
            {
                return BadRequest("Invalid module removal request.");
            }

            var ret = new RemoveModuleResponse();
            var options = new Gurux.DLMS.AMI.Module.AmiModuleUninstallOptions((ModuleDataRemoval)request.DataRemoval);
            IReadOnlyList<string> removalOrder;
            try
            {
                removalOrder = _moduleService.GetRemovalOrder(request.Modules);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                foreach (var id in request.Modules.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                    await LogOperationFailureAsync("remove", id, error);
                }
                throw;
            }
            foreach (var id in removalOrder)
            {
                try
                {
                    await _moduleService.DeleteModuleAsync(new GXModule { Id = id }, cancellationToken, options);
                    await _moduleRepository.DeleteAsync([id], cancellationToken);
                    ret.Results.Add(new(id, true));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error)
                {
                    await LogOperationFailureAsync("remove", id, error);
                    ret.Results.Add(new(id, false, error.Message));
                }
            }
            return ret;
        }

        /// <summary>
        /// Add new custom module.
        /// </summary>
        /// <returns></returns>
        [HttpPost("Add")]
        [Authorize(Policy = GXModulePolicies.Add)]
        public async Task<ActionResult<AddModuleResponse>> AddRaw(CancellationToken cancellationToken = default)
        {
            string zip = Path.GetTempFileName();
            AddModuleResponse res = new();
            try
            {
                using var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms, cancellationToken);
                // Save the file
                System.IO.File.WriteAllBytes(zip, ms.ToArray());
                List<GXModule> modules = await _moduleService.AddModuleAsync(User, zip, cancellationToken);
                res.Modules = [.. modules];
                return res;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await LogOperationFailureAsync("add", null, ex);
                return BadRequest(ex.Message);
            }
            finally
            {
                System.IO.File.Delete(zip);
            }
        }

        /// <summary>
        /// Upload module.
        /// </summary>
        [HttpPost("Update")]
        [Authorize(Policy = GXModulePolicies.Edit)]
        public async Task<ActionResult<UpdateModuleResponse>> Post(UpdateModule request, CancellationToken cancellationToken = default)
        {
            if (request.Module == null)
            {
                return BadRequest("Unknown target.");
            }
            // Authentication methods are security settings, even for users delegated module.edit.
            if (string.Equals(request.Module.Id, "QRCode", StringComparison.OrdinalIgnoreCase) && !User.IsInRole(GXRoles.Admin))
            {
                return Forbid();
            }
            UpdateModuleResponse ret = new();
            GXModule module = await _moduleRepository.ReadAsync(request.Module.Id, null, cancellationToken);
            //Installation and lifecycle state are server-owned; only Enabled is a deployment choice.
            request.Module.Status = module.Status;
            request.Module.RuntimeState = module.RuntimeState;
            // Saving an enabled failed/stopped module also retries startup.
            if (request.Module.Enabled.HasValue && (module.Enabled != request.Module.Enabled ||
                request.Module.Enabled == true && module.RuntimeState is null or (byte)AmiModuleState.Failed or (byte)AmiModuleState.Stopped))
            {
                if (request.Module.Enabled.Value)
                {
                    module.Properties = request.Module.Properties;
                    await _moduleService.EnableModuleAsync(User, module, cancellationToken);
                }
                else
                {
                    await _moduleService.DisableModuleAsync(User, request.Module, cancellationToken);
                }
            }
            //Update module settings.
            await _moduleService.UpdateModuleSettingsAsync(request.Module, cancellationToken);
            await _moduleRepository.UpsertAsync(request.Module, null, cancellationToken);
            return ret;
        }

        /// <summary>
        /// Install module.
        /// </summary>
        [HttpPost("Install")]
        [Authorize(Policy = GXModulePolicies.Add)]
        [Authorize(Policy = GXModulePolicies.Edit)]
        public async Task<ActionResult<InstallModuleResponse>> Post(InstallModule request, CancellationToken cancellationToken = default)
        {
            if (request.Module == null)
            {
                return BadRequest("Invalid target.");
            }
            if (string.IsNullOrEmpty(request.Module.Id))
            {
                return BadRequest("Invalid Url.");
            }
            using var client = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("ModulePackages");
            var installer = new Gurux.DLMS.AMI.Server.Module.GXModuleCatalogInstaller(_moduleRepository, _moduleService, client,
                async (module, token) => await _moduleLogRepository.AddAsync(TargetType.Module, [
                    new GXLog(TraceLevel.Info)
                    {
                        CreationTime = DateTime.Now,
                        SourceId = module.Id,
                        Message = Properties.Resources.ModuleInstalled
                    }
                ], token));
            try
            {
                await installer.InstallAsync(User, request.Module.Id, request.VersionId, cancellationToken);
                return new InstallModuleResponse();
            }
            catch (Exception error) when (error is not OperationCanceledException and not UnauthorizedAccessException
                and not Gurux.DLMS.AMI.Shared.GXAmiForbiddenException and not Gurux.DLMS.AMI.Shared.GXAmiNotFoundException)
            {
                await LogOperationFailureAsync("install", request.Module.Id, error);
                return BadRequest(error.Message);
            }
        }

        /// <summary>
        /// Check are there new module version available.
        /// </summary>
        [HttpPost("Check")]
        [Authorize(Policy = GXModulePolicies.Edit)]
        public async Task<ActionResult<CheckModuleResponse>> Post(
            CheckModule request, CancellationToken cancellationToken = default)
        {
            _ = request;
            var runId = await _cron.StartAsync(GXBackgroundTaskIds.ModuleUpdate, User, cancellationToken);
            return new CheckModuleResponse { RunId = runId };
        }



        /// <summary>
        /// Check are there new module version available.
        /// </summary>
        [HttpGet("Runtime")]
        [AllowAnonymous]
        public async Task<ActionResult<IReadOnlyList<AmiModuleManifest>>> Runtime(CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            var result = new List<AmiModuleManifest>();
            foreach (var manifest in _moduleService.GetManifests())
            {
                if (await CanReadRuntimeAsync(manifest))
                {
                    result.Add(await FilterUiAsync(manifest));
                }
            }

            return result;
        }

        /// <summary>Performs the target operation.</summary>
        /// <param name="target">The target.</param>
        /// <param name="targetId">The target id.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet("Target/{target}/{targetId}")]
        [AllowAnonymous]
        public async Task<ActionResult<GXAmiTarget>> Target(string target, string targetId, CancellationToken cancellationToken = default)
        {
            try
            {
                var dispatcher = HttpContext.RequestServices.GetRequiredService<IGXAmiModuleServices>();
                var projection = await dispatcher.InvokeAsync<IGXAmiTargetProvider, GXAmiTarget?>((provider, token) => provider.ResolveAsync(User, target, targetId, token), cancellationToken);
                return projection == null ? NotFound() : projection;
            }
            catch (AmiModuleUnavailableException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound();
            }
        }

        /// <summary>Returns safe operational status for an accessible module.</summary>
        [HttpGet("Health/{id}")]
        [Authorize(Policy = GXModulePolicies.View)]
        public async Task<ActionResult<Gurux.DLMS.AMI.Module.Health.AmiModuleHealth>> Health(string id, CancellationToken cancellationToken = default)
        {
            if (!await GXModulePolicyAccess.AllowsAsync(HttpContext.RequestServices, User, id, cancellationToken))
            {
                return NotFound();
            }
            await _moduleRepository.ReadAsync(id, null, cancellationToken);
            var runtime = HttpContext.RequestServices.GetRequiredService<Gurux.DLMS.AMI.Server.Module.GXModuleRuntime>();
            var health = runtime.GetHealth(id);
            return health == null ? NotFound() : health;
        }

        private async Task<bool> CanReadRuntimeAsync(AmiModuleManifest manifest)
        {
            if (!await GXModulePolicyAccess.AllowsAsync(HttpContext.RequestServices, User, manifest.Id, HttpContext.RequestAborted))
            {
                return false;
            }
            var authorization = HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
            foreach (var page in manifest.Pages)
            {
                if ((await authorization.AuthorizeAsync(User, null, page.Policy)).Succeeded)
                {
                    return true;
                }
            }

            foreach (var tab in manifest.Views)
            {
                if ((await authorization.AuthorizeAsync(User, null, tab.Policy)).Succeeded)
                {
                    return true;
                }
            }

            if (!(await authorization.AuthorizeAsync(User, null, GXModulePolicies.View)).Succeeded)
            {
                return false;
            }

            try
            {
                await _moduleRepository.ReadAsync(manifest.Id);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (Gurux.DLMS.AMI.Shared.GXAmiForbiddenException)
            {
                // Unloaded custom modules retain diagnostic manifests after their database row is removed.
                // An inaccessible/deleted module must not make the complete runtime catalog unavailable.
                return false;
            }
        }

        private async Task<AmiModuleManifest> FilterUiAsync(AmiModuleManifest manifest)
        {
            var authorization = HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
            var tabs = new List<AmiModuleView>();
            var pages = new List<AmiModulePage>();
            foreach (var tab in manifest.Views)
            {
                if ((await authorization.AuthorizeAsync(User, null, tab.Policy)).Succeeded)
                {
                    tabs.Add(tab);
                }
            }

            foreach (var page in manifest.Pages)
            {
                if ((await authorization.AuthorizeAsync(User, null, page.Policy)).Succeeded)
                {
                    pages.Add(page);
                }
            }

            return manifest with
            {
                Views = tabs,
                Pages = pages
            };
        }

        /// <summary>Performs the runtime operation.</summary>
        /// <param name="id">The item identifier.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet("Runtime/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<AmiModuleManifest>> Runtime(string id, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            var manifest = _moduleService.GetManifest(id);
            if (manifest == null)
            {
                return NotFound();
            }

            return await CanReadRuntimeAsync(manifest) ? await FilterUiAsync(manifest) : NotFound();
        }

        /// <summary>Performs the asset operation.</summary>
        /// <param name="id">The item identifier.</param>
        /// <param name="contentId">The content id.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet("Assets/{id}/{contentId}/{fileName}")]
        [AllowAnonymous]
        public async Task<IActionResult> Asset(string id, string contentId, string fileName, CancellationToken cancellationToken = default)
        {
            var manifest = _moduleService.GetManifest(id);
            if (manifest == null)
            {
                return NotFound();
            }

            if (!await CanReadRuntimeAsync(manifest))
            {
                return NotFound();
            }

            try
            {
                return File(await _moduleService.ReadAssetAsync(id, contentId, fileName, cancellationToken), "application/octet-stream", fileName);
            }
            catch (AmiModuleUnavailableException)
            {
                return NotFound();
            }
        }
    }
}


