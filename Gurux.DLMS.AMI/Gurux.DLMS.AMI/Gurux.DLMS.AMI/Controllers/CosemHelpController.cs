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

using Gurux.DLMS.UI.Components.Help;
using Gurux.UI.Components.Help;
using Gurux.UI.Components.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Bridges browser COSEM help requests to the shared library's official article reader.</summary>
[ApiController]
[Route("api/Help")]
[Authorize]
public sealed class CosemHelpController : ControllerBase
{
    /// <summary>Returns the localized official HTML address for a known COSEM topic.</summary>
    [HttpGet("Cosem")]
    public ActionResult<GXHelpContent> Get(string topic, string? culture = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var address = GXDLMSObjectHelpCatalog.GetAddress(new GXHelpRequest(topic, Culture: culture));
        if (address == null)
        {
            return NotFound();
        }
        return new GXHelpContent(HelpKind.Link, address.AbsoluteUri);
    }
}
