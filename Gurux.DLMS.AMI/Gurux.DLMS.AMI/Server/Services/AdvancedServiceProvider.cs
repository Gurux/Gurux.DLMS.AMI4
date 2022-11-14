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

namespace Gurux.DLMS.AMI.Services
{
    /// <summary>
    /// Creat new service provider for the modules.
    /// </summary>
    public class GXServiceProviderFactory : IServiceProviderFactory<GXContainerBuilder>
    {
        /// <inheritdoc />
        public GXContainerBuilder CreateBuilder(IServiceCollection services)
        {
            return new GXContainerBuilder(services);
        }

        /// <inheritdoc />
        public IServiceProvider CreateServiceProvider(GXContainerBuilder containerBuilder)
        {
            return new GXModuleServiceProvider(containerBuilder);
        }
    }

    public class GXContainerBuilder
    {
        /// <inheritdoc />
        public GXContainerBuilder(IServiceCollection serviceDescriptors)
        {
            ServiceDescriptors = serviceDescriptors;
        }

        internal IServiceCollection ServiceDescriptors { get; }
    }

    /// <summary>
    /// Module service provider.
    /// </summary>
    public class GXModuleServiceProvider : IServiceProvider
    {
        private IServiceProvider? ServiceProvider;

        public GXContainerBuilder ContainerBuilder { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="containerBuilder">Container builder.</param>
        public GXModuleServiceProvider(GXContainerBuilder containerBuilder)
        {
            ServiceProvider = containerBuilder.ServiceDescriptors.BuildServiceProvider(false);
        }

        /// <inheritdoc />
        public object? GetService(Type serviceType)
        {
            return ServiceProvider?.GetService(serviceType);
        }
    }
}
