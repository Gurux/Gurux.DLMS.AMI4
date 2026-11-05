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

namespace Gurux.DLMS.AMI.Module;

/// <summary>QR image generation exported by an optional runtime module.</summary>
public interface IGXAmiQrCodeGenerator
{
    /// <summary>Encodes the supplied text as a PNG image, including the QR quiet zone.</summary>
    /// <param name="text">Text to encode. Callers must not log authenticator secrets.</param>
    /// <returns>PNG bytes owned by the caller, independent of the module lifetime.</returns>
    byte[] GeneratePng(string text);
}
