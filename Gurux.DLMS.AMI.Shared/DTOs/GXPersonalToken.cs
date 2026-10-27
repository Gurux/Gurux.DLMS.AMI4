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

using Gurux.Service.Orm.Common;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A personal access token and its ownership, expiry, and usage information.</summary>
    [System.ComponentModel.Description("A personal access token and its ownership, expiry, and usage information.")]
    public class GXPersonalToken
    {
        /// <summary>
        /// Token identifier.
        /// </summary>
        [System.ComponentModel.Description("Token identifier.")]
        public string Id { get; set; }

        /// <summary>
        /// Token Name.
        /// </summary>
        [System.ComponentModel.Description("Token Name.")]
        public string? Name { get; set; }

        /// <summary>Time when the record was created.</summary>
        [System.ComponentModel.Description("Time when the record was created.")]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Expiration time.
        /// </summary>
        [System.ComponentModel.Description("Expiration time.")]
        public DateTimeOffset? Expiration { get; set; }

        /// <summary>
        /// Custom scopes.
        /// </summary>
        /// <remarks>
        /// Custom scopes can be used if full access is not wanted to give for the client.
        /// </remarks>
        [System.ComponentModel.Description("Custom scopes.")]
        public IEnumerable<string>? Scopes { get; set; }

        /// <summary>Initializes a new instance of GXPersonalToken.</summary>
        [System.ComponentModel.Description("Initializes a new instance of GXPersonalToken.")]
        public GXPersonalToken()
        {
            Id = "";
        }
    }
}
