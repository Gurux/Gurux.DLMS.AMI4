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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Gurux DLMS AMI module interface.
    /// </summary>
    public interface IAmiModule
    {
        /// <summary>
        /// Module identifier.
        /// </summary>
        /// <remarks>
        /// Don't localize the Id or settings are not saved correctly.
        /// </remarks>
        string Id
        {
            get;
        }

        /// <summary>
        /// Name of the module.
        /// </summary>
        /// <remarks>
        /// Name can be localized.
        /// </remarks>
        string Name
        {
            get;
        }

        /// <summary>
        /// Protocols that can use this module.
        /// </summary>
        /// <remarks>
        /// If protocol is null it's available for all protocols..
        /// </remarks>
        string? Protocols
        {
            get;
        }

        /// <summary>
        /// Description of the module.
        /// </summary>
        string Description
        {
            get;
        }

        /// <summary>
        /// Module help Url.
        /// </summary>
        string? Help
        {
            get;
        }

        /// <summary>
        /// Used icon.
        /// </summary>
        /// <remarks>
        /// Icon can be HTML emoji, SVG or empty.
        /// </remarks>
        string? Icon
        {
            get;
        }

        /// <summary>
        /// Registers the specified module context with the application, enabling its services and event handlers.
        /// </summary>
        /// <param name="context">The <see cref="AmiModuleContext"/> representing the module to be registered.  Must not be <c>null</c>.</param>
        void Register(AmiModuleContext context);

        /// <summary>
        /// Extension UI.
        /// </summary>
        /// <remarks>
        /// Null, if the extension doesn't have UI.
        /// </remarks>
        Type? Extension
        {
            get;
        }

        /// <summary>
        /// Can the scheduler invoke this module.
        /// </summary>
        /// <seealso cref="Execute"/>
        /// <seealso cref="Schedule"/>
        bool CanSchedule
        {
            get;
        }

        /// <summary>
        /// Schedule module UI.
        /// </summary>
        /// <seealso cref="CanSchedule"/>
        /// <seealso cref="Schedule"/>
        Type? Schedule
        {
            get;
        }

        /// <summary>
        /// Configuration UI.
        /// </summary>
        /// <remarks>
        /// Null, if the module doesn't have configuration UI.
        /// </remarks>
        Type? Configuration
        {
            get;
        }

        /// <summary>
        /// Framework calls this so module can configure intenal services.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <param name="configuration">Configuration settings.</param>
        void ConfigureModuleServices(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// Module is installed.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <param name="module">Installed module.</param>
        void Install(IServiceProvider services, object module);

        /// <summary>
        /// Module is installed.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <param name="module">Installed module.</param>
        Task InstallAsync(IServiceProvider services, object module);

        /// <summary>
        /// Module is updated.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <param name="current">Current module version.</param>
        /// <param name="updated">Updated module version.</param>
        void Update(IServiceProvider services, string current, string updated);

        /// <summary>
        /// Module is updated.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <param name="current">Current module.</param>
        /// <param name="updated">Updated module.</param>
        Task UpdateAsync(IServiceProvider services, object current, object updated);

        /// <summary>
        /// Module is uninstalled.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <param name="module">Installed module.</param>
        void Uninstall(IServiceProvider services, object module);

        /// <summary>
        /// Module is uninstalled.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <param name="module">Installed module.</param>
        Task UninstallAsync(IServiceProvider services, object module);

        /// <summary>
        /// Module is started.
        /// </summary>
        /// <param name="services">Available services.</param>
        void Start(IServiceProvider services);

        /// <summary>
        /// Module is started.
        /// </summary>
        /// <param name="services">Available services.</param>
        Task StartAsync(IServiceProvider services);

        /// <summary>
        /// Execute module operations.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <param name="settings">Schedule settings.</param>
        /// <param name="instanceSettings">Schedule settings.</param>
        /// <remarks>
        /// This can be used to execute module operations from schedule or workflow.
        /// </remarks>
        /// <seealso cref="CanSchedule"/>
        /// <seealso cref="Schedule"/>
        void Execute(
            IServiceProvider services,
            string? settings,
            string? instanceSettings);

        /// <summary>
        /// Execute module operations.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <param name="settings">Module global settings.</param>
        /// <param name="instanceSettings">Schedule settings.</param>
        /// <remarks>
        /// This can be used to execute module operations from schedule or workflow.
        /// </remarks>
        /// <seealso cref="CanSchedule"/>
        Task ExecuteAsync(
            IServiceProvider services,
            string? settings,
            string? instanceSettings);

        /// <summary>
        /// Module is stopped.
        /// </summary>
        /// <param name="services">Available services.</param>
        void Stop(IServiceProvider services);

        /// <summary>
        /// Module is stopped.
        /// </summary>
        /// <param name="services">Available services.</param>
        Task StopAsync(IServiceProvider services);
    }
}
