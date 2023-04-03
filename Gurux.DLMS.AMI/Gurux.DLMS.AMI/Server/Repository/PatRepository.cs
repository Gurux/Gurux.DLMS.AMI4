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

using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.Service.Orm;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc cref="IPatRepository"/>
    public class PatRepository : IPatRepository
    {
        private readonly IGXHost _host;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        /// <summary>
        /// Constructor.
        /// </summary>
        public PatRepository(IGXHost host,
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService)
        {
            _host = host;
            _userManager = userManager;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Add personal access token.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="token">Token parameters.</param>
        /// <returns>Generated token.</returns>
        public async Task<string> AddPersonalTokenAsync(ClaimsPrincipal User, GXPersonalToken token)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            List<string> roles = new List<string>();
            if (token.Scopes != null && token.Scopes.Any())
            {
                roles.Clear();              
            }
            else
            {
                roles.AddRange(await _userManager.GetRolesAsync(user));
            }
            return await CreatePat(user, roles, token.Scopes, token.Expiration, token.Name);
        }

        /// <summary>
        /// Create personal access token.
        /// </summary>
        private async Task<string> CreatePat(ApplicationUser user, List<string> roleList, string[]? scopes, DateTimeOffset? expiration, string description)
        {
            DateTime now = DateTime.Now;
            if (expiration == null)
            {
                //If expiration time is not given, it's one year.
                expiration = now.AddYears(-1);
            }
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim(JwtClaimTypes.Subject, user.Id));
            claims.Add(new Claim(JwtClaimTypes.Scope, "Gurux.DLMS.AMI.ServerAPI"));
            if (scopes == null || !scopes.Any())
            {
                foreach (string role in roleList)
                {
                    claims.Add(new Claim(JwtClaimTypes.Role, role));
                }
            }
            else
            {
                foreach (string it in scopes)
                {
                    claims.Add(new Claim(JwtClaimTypes.Scope, it));
                }
            }
            claims.Add(new Claim(JwtClaimTypes.ClientId, "Gurux.DLMS.AMI.Client"));
            int lifetime = 0;
            lifetime = (int)(now - expiration.Value).TotalSeconds;
            var token = new Token
            {
                Audiences = new string[] { "Gurux.DLMS.AMI.ServerAPI" },
                ClientId = "Gurux.DLMS.AMI.Client",
                CreationTime = now,
                Issuer = ServerSettings.ServerAddress,
                Lifetime = lifetime,
                Type = OidcConstants.TokenTypes.AccessToken,
                AccessTokenType = AccessTokenType.Reference,
                Claims = new HashSet<Claim>(claims, new ClaimComparer()),
                Description = description
            };
            return await _tokenService.CreateSecurityTokenAsync(token);
        }

        public async Task<GXPersonalToken> GetPersonalTokenByIdAsync(ClaimsPrincipal User, string id)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            ListTokensResponse ret = new ListTokensResponse();
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXPersistedGrants>(where => where.Id == id && where.SubjectId == user.Id && where.Type == "reference_token");
            arg.Distinct = true;
            GXPersistedGrants it = await _host.Connection.SingleOrDefaultAsync<GXPersistedGrants>(arg);
            if (it == null)
            {
                return null;
            }
            return new GXPersonalToken() { Id = it.Id, Name = it.Description, CreationTime = it.CreationTime, Expiration = it.Expiration };
        }

        public async Task<GXPersonalToken[]> GetPersonalTokensAsync(ClaimsPrincipal User, ListTokens? request)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            ListTokensResponse ret = new ListTokensResponse();
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXPersistedGrants>(where => where.SubjectId == user.Id && where.Type == "reference_token");
            arg.Distinct = true;
            arg.OrderBy.Add<GXPersistedGrants>(q => q.Description);
            if (request != null)
            {
                arg.Where.FilterBy(request.Filter);
                if (request.Count != 0)
                {
                    //Return total row count. This can be used for paging.
                    GXSelectArgs total = GXSelectArgs.Select<GXPersistedGrants>(q => GXSql.DistinctCount(q.Id));
                    total.Where.Append(arg.Where);
                    ret.Count = _host.Connection.SingleOrDefault<int>(total);
                    arg.Index = (UInt32)request.Index;
                    arg.Count = (UInt32)request.Count;
                }
            }
            List<GXPersistedGrants> list = _host.Connection.Select<GXPersistedGrants>(arg);
            List<GXPersonalToken> tokens = new List<GXPersonalToken>(list.Count);
            foreach (var it in list)
            {
                tokens.Add(new GXPersonalToken() { Id = it.Id, Name = it.Description, CreationTime = it.CreationTime, Expiration = it.Expiration });
            }
            return tokens.ToArray();
        }

        public async Task<GXPersonalToken> RemovePersonalTokenAsync(ClaimsPrincipal User, string id)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            GXSelectArgs s = GXSelectArgs.SelectAll<GXPersistedGrants>(where => where.Id == id && where.SubjectId == user.Id);
            GXPersistedGrants it = await _host.Connection.SingleOrDefaultAsync<GXPersistedGrants>(s);
            if (it == null)
            {
                throw new Exception("Unknown token: " + id);
            }
            GXDeleteArgs args = GXDeleteArgs.Delete<GXPersistedGrants>(where => where.Id == id && where.SubjectId == user.Id);
            await _host.Connection.DeleteAsync(args);
            return new GXPersonalToken() { Id = it.Id, Name = it.Description, CreationTime = it.CreationTime, Expiration = it.Expiration }; ;
        }
    }
}
