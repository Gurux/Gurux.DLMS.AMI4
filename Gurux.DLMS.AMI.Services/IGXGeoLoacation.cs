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
    /// All properties use the WGS 84 coordinate system. 
    /// Optional properties may be null if the corresponding data is unavailable.</remarks>
    public class GXGeoLocation
    {
        /// <summary>
        /// Latitude.
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// Longitude.
        /// </summary>
        public double Longitude { get; set; }
        /// <summary>
        /// Altitude.
        /// </summary>
        public double? Altitude { get; set; }
        /// <summary>
        /// Accuracy.
        /// </summary>
        public double Accuracy { get; set; }
        /// <summary>
        /// Altitude accuracy.
        /// </summary>
        public double? AltitudeAccuracy { get; set; }
        /// <summary>
        /// Heading.
        /// </summary>
        public double? Heading { get; set; }
        /// <summary>
        /// Speed.
        /// </summary>
        public double? Speed { get; set; }
    }

    /// <summary>
    /// This interface is used to send SMS messages.
    /// </summary>
    public interface IGeolocation
    {
        /// <summary>
        /// Returns coordinates of the current position.
        /// </summary>
        /// <returns>Location info.</returns>
        Task<GXGeoLocation> GetCurrentPosition();
    }
}
