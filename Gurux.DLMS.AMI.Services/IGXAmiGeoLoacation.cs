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

namespace Gurux.DLMS.AMI.Services
{
    /// <summary>
    /// Represents a geographic location, including latitude, longitude, and optional altitude and accuracy information.
    /// </summary>
    /// <remarks>This class is used to store or transfer geolocation data, 
    /// such as that obtained from a GPS sensor or location service. 
    /// Latitude and longitude identify the position; the remaining properties describe measurement quality and movement. 
    /// Nullable properties are null when the provider does not supply the corresponding data.</remarks>
    public class GXAmiGeoLocation
    {
        /// <summary>
        /// Gets or sets the latitude reported by the location provider.
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// Gets or sets the longitude reported by the location provider.
        /// </summary>
        public double Longitude { get; set; }
        /// <summary>
        /// Gets or sets the reported altitude, or null when unavailable.
        /// </summary>
        public double? Altitude { get; set; }
        /// <summary>
        /// Gets or sets the reported horizontal position accuracy.
        /// </summary>
        public double Accuracy { get; set; }
        /// <summary>
        /// Gets or sets the reported vertical position accuracy, or null when unavailable.
        /// </summary>
        public double? AltitudeAccuracy { get; set; }
        /// <summary>
        /// Gets or sets the reported direction of travel, or null when unavailable.
        /// </summary>
        public double? Heading { get; set; }
        /// <summary>
        /// Gets or sets the reported travel speed, or null when unavailable.
        /// </summary>
        public double? Speed { get; set; }
    }

    /// <summary>
    /// Resolves an IP address using an optional geolocation module.
    /// </summary>
    public interface IGXAmiGeolocation
    {
        /// <summary>
        /// Resolves a public IP address; returns null when no location is available.
        /// </summary>
        /// <param name="ipAddress">IPv4 or IPv6 address to resolve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The approximate position, or null.</returns>
        Task<GXAmiGeoLocation?> GetPositionAsync(string ipAddress, CancellationToken cancellationToken = default);
    }
}
