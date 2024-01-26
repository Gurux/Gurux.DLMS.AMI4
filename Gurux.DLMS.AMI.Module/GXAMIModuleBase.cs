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
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Gurux DLMS AMI module base class.
    /// </summary>
    public abstract class GXAmiModuleBase : IAmiModule
    {
        /// <inheritdoc/>
        public abstract string Id
        {
            get;
        }

        /// <inheritdoc/>
        public abstract string Name
        {
            get;
        }

        /// <inheritdoc/>
        public virtual string? Protocols
        {
            get
            {
                return null;
            }
        }
        
        /// <inheritdoc/>
        public abstract string Description
        {
            get;
        }

        /// <inheritdoc/>
        public virtual string? Help
        {
            get
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual string? Icon
        {
            get
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual Type? Extension
        {
            get
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual void ConfigureMidlewares(IServiceCollection services)
        {

        }

        /// <inheritdoc/>
        public virtual bool CanSchedule
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public virtual Type? Schedule
        {
            get
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual Type? Configuration
        {
            get
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual void ConfigureFrameworkServices(IServiceCollection services)
        {
        }

        /// <inheritdoc/>
        public virtual void ConfigureModuleServices(IServiceCollection services, IConfiguration configuration)
        {
        }

        /// <inheritdoc/>
        public virtual void Execute(
            ClaimsPrincipal user,
            IServiceProvider services,
            string? settings,
            string? instanceSettings)
        {
        }

        /// <inheritdoc/>
        public virtual Task ExecuteAsync(
            ClaimsPrincipal user,
            IServiceProvider services,
            string? settings,
            string? instanceSettings)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void Install(ClaimsPrincipal user, IServiceProvider services, object module)
        {
        }

        /// <inheritdoc/>
        public virtual Task InstallAsync(ClaimsPrincipal user, IServiceProvider services, object module)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void Start(IServiceProvider services)
        {
        }

        /// <inheritdoc/>
        public virtual Task StartAsync(IServiceProvider services)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void Stop(IServiceProvider services)
        {
        }

        /// <inheritdoc/>
        public virtual Task StopAsync(IServiceProvider services)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void Uninstall(ClaimsPrincipal user, IServiceProvider services, object module)
        {
        }

        /// <inheritdoc/>
        public virtual Task UninstallAsync(ClaimsPrincipal user, IServiceProvider services, object module)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual void Update(ClaimsPrincipal user, IServiceProvider services, string current, string updated)
        {
        }

        /// <inheritdoc/>
        public virtual Task UpdateAsync(ClaimsPrincipal user, IServiceProvider services, object current, object updated)
        {
            return Task.CompletedTask;
        }
    }
}
