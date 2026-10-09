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
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Gurux.DLMS.AMI.Module;

/// <summary>
/// A module-owned settings snapshot; serializer metadata is not shared across modules.
/// </summary>
public sealed class AmiModuleSettings
{
    private string? value;
    private readonly JsonSerializerOptions json = new() { PropertyNameCaseInsensitive = true };
    /// <summary>Creates a settings snapshot from an optional serialized value.</summary>
    public AmiModuleSettings(string? value = null) => this.value = value;
    /// <summary>Serialized module settings, read and written atomically.</summary>
    public string? Value
    {
        get => Volatile.Read(ref value); set => Volatile.Write(ref this.value, value);
    }
    /// <summary>Deserializes the settings or creates default settings when no value is available.</summary>
    public T Read<T>() where T : new()
    {
        string? snapshot = Value;
        return string.IsNullOrWhiteSpace(snapshot) ? new T() : JsonSerializer.Deserialize<T>(snapshot, json) ?? new T();
    }
}

/// <summary>Services and cancellation owned by one module operation.</summary>
public sealed class AmiModuleContext
{
    /// <summary>Unique module identifier.</summary>
    public required string ModuleId
    {
        get; init;
    }
    /// <summary>Data removal options for module uninstallation.</summary>
    public AmiModuleUninstallOptions UninstallOptions { get; set; } = new();
    /// <summary>Services registered in the module scope.</summary>
    public required IServiceProvider Services
    {
        get; init;
    }
    /// <summary>Services provided by the host.</summary>
    public required IServiceProvider HostServices
    {
        get; init;
    }
    /// <summary>Module settings snapshot.</summary>
    public required AmiModuleSettings Settings
    {
        get; init;
    }
    /// <summary>Cancellation token for the current module operation.</summary>
    public required CancellationToken CancellationToken
    {
        get; init;
    }
    /// <summary>Serializer options for the current module operation.</summary>
    public required JsonSerializerOptions JsonOptions
    {
        get; init;
    }
    /// <summary>Package location; stream-loaded assemblies do not expose a usable Assembly.Location.</summary>
    public string? PackageDirectory
    {
        get; init;
    }
    /// <summary>Tracks externally created subscriptions/timers for disposal at module stop.</summary>
    public Action<IDisposable> Track { get; init; } = _ => throw new InvalidOperationException("This context has no resource owner.");
    /// <summary>Tracks a resource for disposal when the module stops.</summary>
    public void Own(IDisposable resource) => Track(resource);
}

/// <summary>HTTP context plus the module's own service scope.</summary>
public sealed record AmiModuleRequest(HttpContext HttpContext, AmiModuleContext Module);
