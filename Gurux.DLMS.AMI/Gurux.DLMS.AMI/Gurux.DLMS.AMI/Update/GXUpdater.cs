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
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Update;
using CatalogProductType = Gurux.Updater.Enums.CatalogProductType;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Server.Internal;
namespace Gurux.DLMS.AMI.Scheduler;

/// <summary>Coordinates catalog discovery, installation, and persisted update status.</summary>
/// <param name="catalogClient">Client used to query catalog product and release information.</param>
/// <param name="synchronizer">Synchronizes installed products with catalog metadata.</param>
/// <param name="services">Services available to identity or update operations.</param>
/// <param name="accessor">Provides the current authenticated HTTP request.</param>
/// <param name="clients">Creates configured outgoing HTTP clients.</param>
/// <param name="statusStore">Persists update progress and terminal status.</param>
internal sealed class GXUpdater(GXCatalogClient catalogClient, GXCatalogSynchronizer synchronizer, IServiceProvider services, IHttpContextAccessor accessor, IHttpClientFactory clients, GXUpdateStatusStore statusStore) : IGXBackgroundTask
{
    /// <summary>Prevents concurrent full update discovery operations.</summary>
    static readonly SemaphoreSlim _fullChecks = new(1, 1);
    /// <inheritdoc />
    public string TaskId => GXBackgroundTaskIds.Update;
    /// <summary>Resolves the manufacturer repository used when synchronizing manufacturer catalog data.</summary>
    IManufacturerRepository ManufacturerRepository => services.GetRequiredService<IManufacturerRepository>();
    /// <summary>Resolves configuration storage used by catalog update operations.</summary>
    IConfigurationRepository ConfigurationRepository => services.GetRequiredService<IConfigurationRepository>();
    /// <summary>Current request principal, or an empty unauthenticated principal.</summary>
    ClaimsPrincipal User => accessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
    /// <summary>Dispatches a registered background update task to its application, module, agent, or manufacturer updater.</summary>
    /// <param name="taskId">The registered application, module, agent, or manufacturer update task identifier.</param>
    /// <param name="cancellationToken">Cancels the asynchronous operation.</param>
    /// <returns>A task that completes after the selected background update operation finishes.</returns>
    /// <exception cref="ArgumentException">The task identifier is not a registered update operation.</exception>
    public async Task RunAsync(string taskId, CancellationToken cancellationToken = default)
    {
        switch (taskId)
        {
            case GXBackgroundTaskIds.Update:
                await RunAsync(cancellationToken);
                break;
            case GXBackgroundTaskIds.ModuleUpdate:
                await synchronizer.SynchronizeAsync(await catalogClient.GetAsync(cancellationToken), CatalogProductType.Module, cancellationToken);
                break;
            case GXBackgroundTaskIds.AgentUpdate:
                await synchronizer.SynchronizeAsync(await catalogClient.GetAsync(cancellationToken), CatalogProductType.Agent, cancellationToken);
                break;
            case GXBackgroundTaskIds.ManufacturerUpdate:
                await CheckManufacturersCoreAsync(cancellationToken);
                break;
            default:
                throw new ArgumentException("Unknown update task.", nameof(taskId));
        }
    }
    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await _fullChecks.WaitAsync(cancellationToken);
        try
        {
            var catalog = await catalogClient.GetAsync(cancellationToken);
            await synchronizer.SynchronizeAsync(catalog, null, cancellationToken);
            var profiles = await CheckManufacturersCoreAsync(cancellationToken);
            await statusStore.SaveCheckAsync(catalog, profiles, cancellationToken);
            await services.GetRequiredService<GXUpdateSettingsStore>().MarkCheckedAsync(cancellationToken);
            await services.GetRequiredService<GXUpdateEmailNotifier>().NotifyAsync(await statusStore.ReadAsync(cancellationToken), cancellationToken);
        }
        finally
        {
            _fullChecks.Release();
        }
    }
    /// <summary>Compares published manufacturer versions with installed profiles and updates the cached availability status.</summary>
    /// <param name="cancellationToken">Cancels the asynchronous operation.</param>
    /// <returns>A task containing the manufacturer update availability determined from the published index.</returns>
    async Task<GXAvailableUpdate> CheckManufacturersCoreAsync(CancellationToken cancellationToken)
    {
        using var client = clients.CreateClient("ManufacturerUpdates");
        using var response = await client.GetAsync("https://gurux.github.io/Gurux.DLMS.DeviceProfiles/manufacturers.json", cancellationToken);
        response.EnsureSuccessStatusCode();
        var availableManufacturers = GXManufacturerIndex.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        int availableCount = availableManufacturers.Length;
        List<GXManufacturer> installedManufacturers = [];
        ListManufacturers request = new()
        {
            //Get all template manufacturers.
            AllUsers = true,
            Filter = new GXManufacturer()
            {
                Template = true
            },
            Select = ["Version"]
        };
        installedManufacturers.AddRange(await ManufacturerRepository.ListAsync(request, null, cancellationToken));
        //Check manufacturers.

        List<GXManufacturer> updatedManufacturers = [];
        if (availableManufacturers != null)
        {
            //Available manufacturer.
            foreach (var aManufacturer in availableManufacturers)
            {
                //Installed manufacturer.
                GXManufacturer? iManufacturer = installedManufacturers.Where(w => w.Name == aManufacturer.Name).SingleOrDefault();
                if (iManufacturer != null)
                {
                    bool installed = (iManufacturer.Models ?? []).SelectMany(model => model.Versions ?? []).SelectMany(version => version.Settings ?? []).Any(settings => settings.Removed == null && settings.InstallationTime != null && settings.Template?.Id != Guid.Empty && settings.Template != null);
                    bool initializeVersion = installed && string.IsNullOrWhiteSpace(iManufacturer.Version);
                    if (initializeVersion)
                    {
                        iManufacturer.Version = iManufacturer.AvailableVersion ?? aManufacturer.AvailableVersion;
                    }

                    if (initializeVersion || iManufacturer.AvailableVersion != aManufacturer.AvailableVersion || iManufacturer.ProfilesInstalled != installed)
                    {
                        iManufacturer.AvailableVersion = aManufacturer.AvailableVersion;
                        iManufacturer.ProfilesInstalled = installed;
                        updatedManufacturers.Add(iManufacturer);
                    }
                }

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
                            iManufacturer.Models ??= [];

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
                                    iModel.Versions ??= [];

                                    iModel.Versions.Add(aVersion);
                                    updatedManufacturers.Add(iManufacturer);
                                }
                                else if (aVersion.Settings != null)
                                {
                                    //Check if there are new settings.
                                    foreach (var aSettings in aVersion.Settings)
                                    {
                                        iVersion.Settings ??= [];
                                        if (GXManufacturerIndex.MergeProfile(iVersion.Settings, aSettings))
                                        {
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
        if (updatedManufacturers.Count != 0)
        {
            await ManufacturerRepository.UpsertAsync(updatedManufacturers.Distinct(), null, cancellationToken);
        }
        //Update check time.
        ListConfiguration req = new()
        {
            Filter = new
            Shared.DTOs.GXConfiguration()
            {
                Name = GXConfigurations.Manufacturer
            }
        };
        var confs = await ConfigurationRepository.ListAsync(User, req, null, cancellationToken);
        foreach (var conf in confs)
        {
            if (conf.Name == GXConfigurations.Manufacturer)
            {
                var settings = string.IsNullOrWhiteSpace(conf.Settings) ? [] : JsonNode.Parse(conf.Settings)?.AsObject() ?? [];
                settings["Checked"] = DateTimeOffset.UtcNow;
                conf.Settings = settings.ToJsonString();
                await ConfigurationRepository.UpsertAsync(User, [conf], true, cancellationToken);
                break;
            }
        }
        int localCount = installedManufacturers.Select(m => m.Name).Union(updatedManufacturers.Select(m => m.Name)).Count();
        return new GXAvailableUpdate
        {
            Id = catalogClient.ManufacturerProductId,
            Name = "Device profiles",
            Status = GXUpdateStatus.Unknown,
            Description = $"Local manufacturer templates: {localCount}. Public index: {availableCount} manufacturers. The public device-profile index does not publish release versions."
        };
    }


}
