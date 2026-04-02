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
namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Provides context information and endpoint mapping delegates for an AMI module, including base API path and
    /// handlers for HTTP GET and POST requests.
    /// </summary>
    /// <remarks>This class supplies the base path for the module's API endpoints and delegates for mapping
    /// HTTP GET and POST routes to their respective handlers. It is typically used to configure routing and request
    /// handling within an AMI module.</remarks>
    public sealed class AmiModuleContext
    {
        /// <summary>
        /// Gets or sets the base directory path used for file operations. E.g. "/api/smtp"
        /// </summary>
        public required string BasePath { get; init; }

        /// <summary>
        /// Gets or sets the delegate used to map an HTTP GET route to a handler.
        /// </summary>
        /// <remarks>The returned object typically represents the route mapping and may be used for
        /// further configuration or chaining. The handler delegate should match the expected signature for the
        /// route.</remarks>
        public required Func<string, Delegate, object> MapGet { get; init; }

        /// <summary>
        /// Gets or sets the delegate used to map HTTP POST endpoints to their corresponding handlers.
        /// </summary>
        /// <remarks>Use this property to configure how POST routes are registered and associated with
        /// their handlers. The returned object typically represents the endpoint registration and may be used for
        /// further configuration.</remarks>
        public required Func<string, Delegate, object> MapPost { get; init; }
    }
}
