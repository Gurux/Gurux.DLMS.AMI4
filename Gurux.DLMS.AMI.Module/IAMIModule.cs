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
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Gurux DLMS AMI module interface.
    /// </summary>
    public interface IAmiModule
    {
        /// <summary>
        /// Name of the module.
        /// </summary>
        /// <remarks>
        /// Don't localize the name or settings are not saved correctly.
        /// </remarks>
        string Name
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
        /// Css icon.
        /// </summary>
        string? Icon
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
        /// Module is installed.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="services">Available services.</param>
        /// <param name="module">Installed module.</param>
        void Install(ClaimsPrincipal user, IServiceProvider services, object module);

        /// <summary>
        /// Module is updated.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="services">Available services.</param>
        /// <param name="current">Current module.</param>
        /// <param name="updated">Updated module.</param>
        void Update(ClaimsPrincipal user, IServiceProvider services, object current, object updated);

        /// <summary>
        /// Module is uninstalled.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="services">Available services.</param>
        /// <param name="module">Installed module.</param>
        void Uninstall(ClaimsPrincipal user, IServiceProvider services, object module);

        /// <summary>
        /// Framework calls this so module can configure intenal services.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <param name="configuration">Configuration settings.</param>
        void ConfigureModuleServices(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// Framework calls this so module can implement services that the framework can use.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <remarks>
        /// The list of the services is null if module doesn't offer any services for the framework.
        /// Framework services requite that application must reboot after install or update.
        /// </remarks>
        void ConfigureFrameworkServices(IServiceCollection services);

        /// <summary>
        /// Configure module middlewares.
        /// </summary>
        /// <param name="builder">Builder.</param>
        void ConfigureMidlewares(IApplicationBuilder builder);

        /// <summary>
        /// Module is started.
        /// </summary>
        /// <param name="services">Available services.</param>
        void Start(IServiceProvider services);

        /// <summary>
        /// Execute module operations.
        /// </summary>
        /// <param name="services">Available services.</param>
        /// <remarks>
        /// This can be used to execute module operations from schedule or workflow.
        /// </remarks>
        void Execute(IServiceProvider services);

        /// <summary>
        /// Module is stopped.
        /// </summary>
        /// <param name="services">Available services.</param>
        void Stop(IServiceProvider services);
    }
}
