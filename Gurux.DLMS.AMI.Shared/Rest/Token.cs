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
using System.Runtime.Serialization;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Requests changes to the specified tokens.</summary>
    [DataContract]
    [Description("Requests changes to the specified tokens.")]
    public class UpdateToken : IGXRequest<UpdateTokenResponse>
    {
        /// <summary>
        /// List of tokens.
        /// </summary>
        [Description("List of tokens.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXPersonalToken), nameof(GXPersonalToken.CreationTime))]
        public IEnumerable<GXPersonalToken>? Tokens { get; set; }
    }

    /// <summary>Response to updating the specified tokens.</summary>
    [DataContract]
    [Description("Response to updating the specified tokens.")]
    public class UpdateTokenResponse
    {
        /// <summary>
        /// Generated token.
        /// </summary>
        [Description("Generated token.")]
        [DataMember]
        public string? Token { get; set; }
    }

    /// <summary>Requests a list of tokens.</summary>
    [DataContract]
    [Description("Requests a list of tokens.")]
    public class ListTokens : IGXRequest<ListTokensResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of token to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of token to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter tokens.
        /// </summary>
        [Description("Optional criteria used to filter tokens.")]
        [ExcludeOpenApi(typeof(GXPersonalToken),
            nameof(GXPersonalToken.Id), nameof(GXPersonalToken.Name), nameof(GXPersonalToken.CreationTime),
            nameof(GXPersonalToken.Expiration), nameof(GXPersonalToken.Scopes))]
        public GXPersonalToken? Filter { get; set; }
    }

    /// <summary>Response containing the requested tokens.</summary>
    [DataContract]
    [Description("Response containing the requested tokens.")]
    public class ListTokensResponse
    {
        /// <summary>
        /// List of tokens.
        /// </summary>
        [Description("List of tokens.")]
        [DataMember]
        public IEnumerable<GXPersonalToken>? Tokens { get; set; }

        /// <summary>
        /// Total number of matching tokens before paging.
        /// </summary>
        [DataMember]
        [Description("Total number of matching tokens before paging.")]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected tokens.</summary>
    [DataContract]
    [Description("Requests deletion of the selected tokens.")]
    public class DeleteToken : IGXRequest<DeleteTokenResponse>
    {
        /// <summary>Identifiers of the token records to delete.</summary>
        [DataMember]
        [Description("Identifiers of the token records to delete.")]
        public IEnumerable<string>? Ids { get; set; }
    }

    /// <summary>Response to deleting the selected tokens.</summary>
    [DataContract]
    [Description("Response to deleting the selected tokens.")]
    public class DeleteTokenResponse
    {
    }
}
