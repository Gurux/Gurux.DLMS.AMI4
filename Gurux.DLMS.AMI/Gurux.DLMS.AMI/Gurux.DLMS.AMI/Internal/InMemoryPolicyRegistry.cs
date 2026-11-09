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

using System.Collections.Concurrent;
using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Authorization;
namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>Registers authorization policies provided by runtime modules.</summary>
    public class InMemoryPolicyRegistry : IGXAmiPolicyRegistry
    {
        private readonly ConcurrentDictionary<string, AuthorizationPolicy> _policies = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public AuthorizationPolicy? Get(string name)
        {
            _policies.TryGetValue(name, out var policy);
            return policy;
        }

        /// <inheritdoc/>
        public void Add(string name, AuthorizationPolicy policy)
        {
            _policies[name] = policy;
        }

        /// <inheritdoc/>
        public bool Remove(string name)
        {
            return _policies.TryRemove(name, out _);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<string> GetAllPolicyNames()
        {
            return [.. _policies.Keys];
        }
    }
}
