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

using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Gurux.DLMS.AMI.Services
{

    public sealed class GXDbProxyConfigProvider : IProxyConfigProvider, IDisposable
    {
        private volatile IProxyConfig _config;
        private CancellationTokenSource _cts = new();
        private readonly IGXHost _host;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDbProxyConfigProvider(IGXHost host)
        {
            _host = host;
            _config = new InMemoryProxyConfig([], [], new CancellationChangeToken(_cts.Token));
        }

        /// <summary>
        /// Proxy config.
        /// </summary>
        public IProxyConfig GetConfig() => _config;

        public async Task ReloadAsync(CancellationToken ct = default)
        {
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXCluster>();
            arg.Columns.Add<GXRoute>();
            arg.Columns.Add<GXDestination>();
            arg.Joins.AddInnerJoin<GXCluster, GXRoute>(j => j.Id, j => j.Cluster);
            arg.Joins.AddInnerJoin<GXCluster, GXDestination>(j => j.Id, j => j.Cluster);
            var clustersDb = await _host.Connection.SelectAsync<GXCluster>(arg);
            // Convert to YARP.
            var clusters = clustersDb.Select(c => new ClusterConfig
            {
                ClusterId = c.Id,
                LoadBalancingPolicy = c.LoadBalancingPolicy == 0
                    ? LoadBalancingPolicy.RoundRobin.ToString() : c.LoadBalancingPolicy.ToString(),
                SessionAffinity = new SessionAffinityConfig
                {
                    Enabled = c.SessionAffinityEnabled,
                    Policy = "Cookie",
                    AffinityKeyName = "Affinity",
                    Cookie = new SessionAffinityCookieConfig
                    {
                        Path = "/",
                        SameSite = SameSiteMode.Lax,
                        HttpOnly = true,
                        IsEssential = true
                    }
                },
                Destinations = c.Destinations?.ToDictionary(d => d.Id, d => new DestinationConfig
                {
                    Address = d.Address
                })
            }).ToList();

            arg = GXSelectArgs.SelectAll<GXRoute>();
            arg.Joins.AddInnerJoin<GXRoute, GXCluster>(j => j.Cluster, j => j.Id);
            var routesDb = await _host.Connection.SelectAsync<GXRoute>(arg);
            var routes = routesDb.Select(r =>
            {
                var rc = new RouteConfig
                {
                    RouteId = r.Id,
                    ClusterId = r.Cluster?.Id,
                    Match = new RouteMatch { Path = r.MatchPath },
                    Transforms = PathPatternTransforms.Build(r.TransformPathPattern, r.CustomPathPattern)
                };
                return rc;
            }).ToList();
            // Release new ChangeToken.
            var old = _cts;
            _cts = new CancellationTokenSource();
            _config = new InMemoryProxyConfig(routes, clusters, new CancellationChangeToken(_cts.Token));
            old.Cancel();
            old.Dispose();
        }

        public void Dispose() => _cts.Dispose();

        private sealed class InMemoryProxyConfig : IProxyConfig
        {
            public InMemoryProxyConfig(IReadOnlyList<RouteConfig> routes,
                                       IReadOnlyList<ClusterConfig> clusters,
                                       IChangeToken changeToken)
            {
                Routes = routes; Clusters = clusters; ChangeToken = changeToken;
            }
            public IReadOnlyList<RouteConfig> Routes { get; }
            public IReadOnlyList<ClusterConfig> Clusters { get; }
            public IChangeToken ChangeToken { get; }
        }

        public static class PathPatternTransforms
        {
            /// <summary>
            /// Builds the YARP Transforms list for the given enum value.
            /// Returns null if no transform should be applied.
            /// </summary>
            public static List<Dictionary<string, string>>? Build(TransformPathPattern kind, string? customPattern = null)
            {
                // No transform => return null so RouteConfig.Transforms can stay null/empty.
                if (kind == TransformPathPattern.None)
                    return null;

                // Resolve the actual PathPattern string.
                var pathPattern = kind switch
                {
                    TransformPathPattern.StripPrefix => "/{**catch-all}",
                    TransformPathPattern.RewriteToRoot => "/",
                    TransformPathPattern.RewriteToFixed => customPattern ?? "/",
                    TransformPathPattern.Custom => customPattern ?? "/{**catch-all}",
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(pathPattern))
                    return null;

                // YARP expects a list of transform dictionaries, e.g. [{ "PathPattern": "/{**catch-all}" }]
                return new List<Dictionary<string, string>>
        {
            new() { ["PathPattern"] = pathPattern }
        };
            }
        }
    }

}
