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

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Options;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Host-specific storage for uploaded appearance images.</summary>
public sealed class ImageStorageOptions
{
    /// <summary>Absolute storage directory outside the web root; empty uses the service account's local application data.</summary>
    public string RootPath { get; set; } = "";
    /// <summary>Maximum decoded file size in bytes.</summary>
    public int MaxFileSize { get; set; } = 10 * 1024 * 1024;
}
