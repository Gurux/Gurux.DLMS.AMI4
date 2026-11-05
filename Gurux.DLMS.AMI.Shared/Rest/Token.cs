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

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }
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

    /// <summary>Requests a later expiration for an active owned personal access token.</summary>
    [DataContract, Description("Requests a later expiration for an active owned personal access token.")]
    public class ExtendToken : IGXRequest<ExtendTokenResponse>
    {
        /// <summary>The identifier of the token to extend.</summary>
        [DataMember, Description("The identifier of the token to extend.")]
        public string? Id { get; set; }

        /// <summary>The new expiration instant, including the local UTC offset.</summary>
        [DataMember, Description("The new expiration instant, including the local UTC offset.")]
        public DateTimeOffset? Expiration { get; set; }
    }

    /// <summary>The updated metadata of the extended token; contains no token secret.</summary>
    [DataContract, Description("The updated metadata of the extended token; contains no token secret.")]
    public class ExtendTokenResponse
    {
        /// <summary>The updated token metadata.</summary>
        [DataMember, Description("The updated token metadata.")]
        public GXPersonalToken? Item { get; set; }
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
