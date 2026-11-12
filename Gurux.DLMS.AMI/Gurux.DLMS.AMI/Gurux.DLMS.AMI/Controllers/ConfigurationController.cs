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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Prune;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;
using Gurux.DLMS.AMI.Shared.DTOs.Script;

namespace Gurux.DLMS.AMI.Server.Controllers
{
    /// <summary>
    /// This controller is used to handle the system configurations.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigurationController(IConfigurationRepository configurationRepository,
        IScriptRepository scriptRepository,
        IHostApplicationLifetime applicationLifetime,
        IServiceProvider serviceProvider) : ControllerBase
    {
        private static readonly SemaphoreSlim SchemaUpdateLock = new(1, 1);
        private readonly IConfigurationRepository _configurationRepository = configurationRepository;
        private readonly IScriptRepository _scriptRepository = scriptRepository;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime;

        /// <summary>
        /// Get system version number.
        /// </summary>
        [HttpGet("Version")]
        [AllowAnonymous]
        public ActionResult<string?> Get(CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(asm.Location);
            return Content(info.FileVersion!, "text/plain");
        }

        /// <summary>Returns only the public maintenance notice and schedule.</summary>
        [HttpGet("Maintenance")]
        [AllowAnonymous]
        public async Task<ActionResult<MaintenanceSettings>> Maintenance(CancellationToken cancellationToken)
        {
            Response.Headers.CacheControl = "no-store";
            var configurations = await _configurationRepository.ListAsync(User,
                new ListConfiguration { Filter = new GXConfiguration { Name = "maintenance" } }, null, cancellationToken);
            var settings = configurations.FirstOrDefault()?.Settings;
            var maintenance = string.IsNullOrEmpty(settings) ? new MaintenanceSettings() :
                System.Text.Json.JsonSerializer.Deserialize<MaintenanceSettings>(settings)
                    ?? new MaintenanceSettings();
            if (maintenance.Message is { Length: > 0 and <= 65536 })
                {
                maintenance.Message = await _serviceProvider.GetRequiredService<Gurux.DLMS.AMI.Module.IGXAmiTemplateTokenResolver>()
                    .ResolveAsync(maintenance.Message, new Gurux.DLMS.AMI.Module.AmiTemplateTokenContext
                    {
                        User = User,
                        Culture = System.Globalization.CultureInfo.CurrentCulture,
                        Page = "maintenance",
                        Values = maintenance.GetTokenValues()
                    }, cancellationToken);
            }
            return maintenance;
        }

        /// <summary>
        /// Get configuration information.
        /// </summary>
        /// <param name="id">Configuration id.</param>
        /// <returns>Configuration information.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpGet]
        [Authorize(Policy = GXConfigurationPolicies.View)]

        public async Task<ActionResult<GetConfigurationResponse>> Get(Guid id, CancellationToken cancellationToken = default)
        {
            return new GetConfigurationResponse()
            {
                Item = await _configurationRepository.ReadAsync(User, id, null, cancellationToken)
            };
        }

        /// <summary>
        /// List configuration information.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Configuration settings.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("List")]
        [Authorize(Policy = GXConfigurationPolicies.View)]
        public async Task<ActionResult<ListConfigurationResponse>> Post(
            ListConfiguration request,
            CancellationToken cancellationToken)
        {
            ListConfigurationResponse ret = new();
            await _configurationRepository.ListAsync(User, request, ret, cancellationToken);
            return ret;
        }

        /// <summary>Lists configuration identifiers and names for menu link selection, without settings.</summary>
        [HttpGet("References")]
        [Authorize(Policy = GXConfigurationPolicies.View)]
        public async Task<ActionResult<GXConfiguration[]>> References(CancellationToken cancellationToken = default)
        {
            var configurations = await _configurationRepository.ListAsync(User,
                new ListConfiguration { OrderBy = nameof(GXConfiguration.Name) }, null, cancellationToken);
            return configurations.Select(c => new GXConfiguration { Id = c.Id, Name = c.Name }).ToArray();
        }

        /// <summary>
        /// Update configuration values.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Add")]
        [HttpPost("Update")]
        [Authorize(Policy = GXConfigurationPolicies.Add)]
        public async Task<ActionResult<ConfigurationUpdateResponse>> Post(ConfigurationUpdate request, CancellationToken cancellationToken = default)
        {
            if (request.Configurations == null || !request.Configurations.Any())
            {
                return BadRequest(Properties.Resources.ArrayIsEmpty);
            }
            await _configurationRepository.UpsertAsync(User, request.Configurations, true, cancellationToken);
            return new ConfigurationUpdateResponse();
        }

        /// <summary>
        /// Run cron.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Cron")]
        [Authorize(Policy = GXConfigurationPolicies.Cron)]
        public async Task<ActionResult<ConfigurationRunCronResponse>> Post(ConfigurationRunCron request, CancellationToken cancellationToken = default)
        {
            _ = request;
            _ = cancellationToken;
            IGXBackgroundService cronService = _serviceProvider.GetRequiredService<IGXBackgroundService>();
            await cronService.RunAsync(User);
            return new ConfigurationRunCronResponse();
        }

        /// <summary>
        /// Run prune.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Prune")]
        [Authorize(Policy = GXConfigurationPolicies.Prune)]
        public async Task<ActionResult<ConfigurationRunPruneResponse>> Post(ConfigurationRunPrune request, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            _ = request;
            IGXPruneService pruneService = _serviceProvider.GetRequiredService<IGXPruneService>();
            await pruneService.RunAsync();
            return new ConfigurationRunPruneResponse();
        }

        /// <summary>
        /// Checks whether the database schema has changed.
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetTablesWithSchemaChangesAsync")]
        [Authorize(Policy = GXConfigurationPolicies.GetTablesWithSchemaChanges)]
        public async Task<ActionResult<IEnumerable<string>?>> Post(CancellationToken cancellationToken = default)
        {
            return Ok(await _configurationRepository.GetTablesWithSchemaChangesAsync(cancellationToken));
        }

        /// <summary>Describes pending database schema changes without applying them.</summary>
        [HttpPost("GetTableChanges")]
        [Authorize(Policy = GXConfigurationPolicies.GetTablesWithSchemaChanges)]
        public async Task<ActionResult<IEnumerable<GXConfigurationTableChange>>> GetTableChanges(CancellationToken cancellationToken = default)
            => Ok(await _configurationRepository.GetTableChangesAsync(cancellationToken));

        /// <summary>Applies the existing application database schema upgrade and returns remaining changes.</summary>
        [HttpPost("UpdateTables")]
        [Authorize(Roles = GXRoles.Admin, Policy = GXUpdatePolicies.Admin)]
        public async Task<ActionResult<IEnumerable<GXConfigurationTableChange>>> UpdateTables(
            [FromServices] IDbConnectionFactory database,
            [FromServices] ILogger<GXConfiguration> logger,
            CancellationToken cancellationToken = default)
        {
            await SchemaUpdateLock.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var connection = await database.GetConnectionAsync(cancellationToken);
                ServerSettings.UpdateDatabase(connection, logger);
                var remaining = await _configurationRepository.GetTableChangesAsync(cancellationToken);
                var users = await _serviceProvider.GetRequiredService<IUserRepository>()
                    .GetUserIdsInRoleAsync([GXRoles.Admin]);
                _serviceProvider.GetRequiredService<NotificationCache>().Add(users,
                    new GXTarget(GXConfigurationPolicies.Edit, "database-schema"));
                return Ok(remaining);
            }
            finally
            {
                SchemaUpdateLock.Release();
            }
        }

        /// <summary>
        /// Get configuration version information.
        /// </summary>
        /// <param name="request">Configuration request.</param>
        /// <returns>Configuration Versions.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [Authorize(Policy = GXConfigurationPolicies.View)]
        [HttpPost("Assemblies")]

        public async Task<ActionResult<LoadedAssembliesResponse>> Post(LoadedAssemblies request, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i != 10; ++i)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            List<GXAssembly> list = [];
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
                        GXScript script = await _scriptRepository.ReadAsync(id, null, cancellationToken);
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
                            if (!name.Contains(filterName, StringComparison.CurrentCultureIgnoreCase))
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
                Assemblies = [.. list.OrderBy(x => x.Name)]
            };
        }

        /// <summary>
        /// Restart application.
        /// </summary>
        /// <returns>Configuration Versions.</returns>
        [Authorize(Policy = GXConfigurationPolicies.Edit)]
        [HttpPost("Restart")]
        public ActionResult Post(StopApplicationRequest request, CancellationToken cancellationToken = default)
        {
            _ = request;
            _ = cancellationToken;
            Response.OnCompleted(() =>
            {
                _applicationLifetime.StopApplication();
                return Task.CompletedTask;
            });
            return Accepted();
        }

        /// <summary>
        /// Regenerates configuration items based on the specified identifiers.
        /// </summary>
        /// <param name="request">The request containing the identifiers of the menu items to regenerate.</param>
        /// <returns>A response indicating the result of the regeneration operation.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [HttpPost("Regenerate")]
        [Authorize(Policy = GXConfigurationPolicies.Edit)]
        public async Task<ActionResult<RegenerateConfigurationResponse>> Post(RegenerateConfiguration request, CancellationToken cancellationToken = default)
        {
            if (request.Ids?.Any() == true)
            {
                await _configurationRepository.ResetToDefaultsAsync(User, request.Ids, cancellationToken);
            }
            else
            {
                await _configurationRepository.EnsureDefaultsAsync(User, cancellationToken);
            }

            return new RegenerateConfigurationResponse();
        }
    }
}
