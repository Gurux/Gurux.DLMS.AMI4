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
    /// <summary>
    /// Represents a geographic location, including latitude, longitude, and optional altitude and accuracy information.
    /// </summary>
    /// <remarks>This class is used to store or transfer geolocation data, 
    /// such as that obtained from a GPS sensor or location service. 
    /// Latitude and longitude identify the position; the remaining properties describe measurement quality and movement. 
    /// Nullable properties are null when the provider does not supply the corresponding data.</remarks>
    public class GXGeoLocation
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
    /// Provides the current geographic position from a location service.
    /// </summary>
    public interface IGeolocation
    {
        /// <summary>
        /// Asynchronously obtains the current position and available measurement details.
        /// </summary>
        /// <returns>A task whose result contains the position reported by the provider.</returns>
        Task<GXGeoLocation> GetCurrentPosition();
    }
}
