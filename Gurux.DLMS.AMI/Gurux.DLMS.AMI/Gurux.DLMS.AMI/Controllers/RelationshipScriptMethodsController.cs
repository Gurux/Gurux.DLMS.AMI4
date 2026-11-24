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

using Gurux.DLMS.AMI.Client.Helpers.Selection;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Pages script methods through the repository's existing membership and resource-policy checks.</summary>
/// <param name="scripts">The authorized script repository.</param>
[ApiController]
[Route("api/RelationshipScriptMethods")]
public sealed class RelationshipScriptMethodsController(IScriptRepository scripts) : ControllerBase
{
    /// <summary>Lists methods using method-level identity restrictions and bounded script batches.</summary>
    /// <param name="request">The method page, script filter and identity restrictions.</param>
    /// <param name="cancellationToken">Cancels repository queries.</param>
    /// <returns>The method page and matching method count, rather than a script count.</returns>
    [HttpPost("List")]
    [Authorize(Policy = GXScriptPolicies.View)]
    public Task<GXRelationshipMethodPage> ListAsync(GXRelationshipMethodRequest request,
        CancellationToken cancellationToken) => GXRelationshipMethodPaging.ReadAsync(request, async (query, token) =>
        {
            var response = new ListScriptsResponse();
            await scripts.ListAsync(query, response, token);
            return response;
        }, cancellationToken);
}
