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
    public sealed class GXProxyConfigReloader : BackgroundService
    {
        private readonly GXDbProxyConfigProvider _provider;
        private readonly ILogger<GXProxyConfigReloader> _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="provider">Db proxy config provider</param>
        /// <param name="logger">Logger</param>
        public GXProxyConfigReloader(GXDbProxyConfigProvider provider,
            ILogger<GXProxyConfigReloader> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            // Initial load.
            await _provider.ReloadAsync(stoppingToken);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await _provider.ReloadAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "YARP config reload failed");
                }
            }
        }
    }
}
