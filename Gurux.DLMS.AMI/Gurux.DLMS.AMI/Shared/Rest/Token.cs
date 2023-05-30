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
using Gurux.Common;
using System.Runtime.Serialization;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Add or update token.
    /// </summary>
    [DataContract]
    [Description("Add or update token.")]
    public class UpdateToken : IGXRequest<UpdateTokenResponse>
    {
        /// <summary>
        /// List of tokens.
        /// </summary>
        [Description("List of tokens to add or update.")]
        [DataMember]
        public GXPersonalToken[] Tokens
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add or update token reply.
    /// </summary>
    [DataContract]
    [Description("Add or update token reply.")]
    public class UpdateTokenResponse
    {
        /// <summary>
        /// Generated token.
        /// </summary>
        [DataMember]
        public string? Token
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get available tokens.
    /// </summary>
    [DataContract]
    [Description("Get available tokens.")]
    public class ListTokens : IGXRequest<ListTokensResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the token to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter tokens.
        /// </summary>
        public GXPersonalToken? Filter
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get tokens reply.
    /// </summary>
    [DataContract]
    [Description("Get tokens reply.")]
    public class ListTokensResponse
    {
        /// <summary>
        /// List of tokens.
        /// </summary>
        [Description("List of tokens.")]
        [DataMember]
        public GXPersonalToken[] Tokens
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the tokens.
        /// </summary>
        [DataMember]
        [Description("Total count of the tokens.")]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete selected tokens-
    /// </summary>
    [DataContract]
    [Description("Delete selected tokens.")]
    public class DeleteToken : IGXRequest<DeleteTokenResponse>
    {
        /// <summary>
        /// Removed tokens.
        /// </summary>
        [DataMember]
        [Description("Removed tokens.")]
        public string[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete tokens response.
    /// </summary>
    [DataContract]
    [Description("Delete tokens response.")]
    public class DeleteTokenResponse
    {
    }

    /// <summary>
    /// Regenerate token.
    /// </summary>
    public class RegenerateToken : IGXRequest<RegenerateTokenResponse>
    {
        /// <summary>
        /// Regenerated personal access token.
        /// </summary>
        [DataMember]
        [Description("Regenerated personal access token.")]
        public GXPersonalToken? Token
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Regenerated token response.
    /// </summary>
    [DataContract]
    [Description("Regenerated token response.")]
    public class RegenerateTokenResponse
    {
    }
}
