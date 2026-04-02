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

using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Enums
{
    /// <summary>
    /// Use in: YARP custom config provider (e.g., when building RouteConfig.Transforms)
    /// </summary>
    public enum TransformPathPattern : int
    {
        /// <summary>
        /// No PathPattern transform is applied. The original request path
        /// (e.g., "/api/orders/42") is forwarded as-is to the destination.
        /// </summary>
        [XmlEnum("0")]
        None = 0,

        /// <summary>
        /// Strips the leading route prefix (e.g., "/api/") and forwards only the trailing part.
        /// Example: incoming "/api/orders/42" -> destination "/orders/42".
        /// Implemented as PathPattern = "/{**catch-all}".
        /// </summary>
        [XmlEnum("1")]
        StripPrefix = 1,

        /// <summary>
        /// Rewrites everything to the destination root "/".
        /// Example: any incoming path -> destination "/".
        /// Useful for single-endpoint backends.
        /// Implemented as PathPattern = "/".
        /// </summary>
        [XmlEnum("2")]
        RewriteToRoot = 2,

        /// <summary>
        /// Rewrites to a fixed, literal path (e.g., "/hello" or "/v1").
        /// Provide the literal with the 'customPattern' argument in the helper below.
        /// </summary>
        [XmlEnum("3")]
        RewriteToFixed = 3,

        /// <summary>
        /// Fully custom PathPattern (may include catch-all).
        /// Provide the pattern with the 'customPattern' argument in the helper below.
        /// Example: "/v2/{**catch-all}".
        /// </summary>
        [XmlEnum("999")]
        Custom = 999
    }
}
