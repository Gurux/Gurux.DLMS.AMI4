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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <summary>
    /// Encrypts module secrets using the server cryptographic service.
    /// </summary>
    /// <remarks>Creates a controller using the server cryptographic service.</remarks>
    [ApiController]
    [Route("api/Encrypt")]
    [Authorize(Policy = GXModulePolicies.Edit)]
    public class EncryptController(IGXAmiCryptoService crypto) : ControllerBase
    {
        private readonly IGXAmiCryptoService _crypto = crypto;

        /// <summary>Encrypts the binary request body for module secret storage.</summary>
        [HttpPost]
        [Consumes("application/octet-stream")]
        [Produces("application/octet-stream")]
        public async Task<IActionResult> Post(CancellationToken cancellationToken)
        {
            using var data = new MemoryStream();
            await Request.Body.CopyToAsync(data, cancellationToken);
            var encrypted = await _crypto.EncryptAsync(data.ToArray());
            return File(encrypted, "application/octet-stream");
        }
    }
}
