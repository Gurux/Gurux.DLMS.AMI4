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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;


namespace Gurux.DLMS.AMI.Server.Repository
{
    /// <inheritdoc />
    public class PatRepository : IPatRepository
    {
        private readonly ClaimsPrincipal User;
        private readonly IDbConnectionFactory _dbFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOptions<CryptoOptions> _options;
        private readonly IAuthorizationService _authorizationService;
        private readonly GXPatConnections? _connections;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PatRepository(IAuthorizationService authorizationService,
            IDbConnectionFactory dbFactory,
            IOptions<CryptoOptions> options,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            GXPatConnections? connections = null)
        {
            if (httpContextAccessor.HttpContext?.User == null)
            {
                throw new UnauthorizedAccessException();
            }
            User = httpContextAccessor.HttpContext.User;
            _options = options;
            _dbFactory = dbFactory;
            _userManager = userManager;
            _authorizationService = authorizationService;
            _connections = connections;
        }

        /// <summary>
        /// Add personal access token.
        /// </summary>
        /// <param name="token">Token parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Generated token.</returns>
        public async Task<string> AddPersonalTokenAsync(GXPersonalToken token, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _authorizationService.EnsureAnyPolicyAsync(User, GXUserPolicies.View);
            ApplicationUser? user = await _userManager.GetUserAsync(User) ?? throw new Exception("Invalid user.");
            List<string> roles = [];
            if (token.Scopes != null && token.Scopes.Any())
            {
                roles.Clear();
            }
            else
            {
                roles.AddRange(await _userManager.GetRolesAsync(user));
            }
            return await CreatePat(user, roles, token.Scopes, token.Expiration, token.Name, cancellationToken);
        }

        /// <summary>
        /// Create personal access token.
        /// </summary>
        private async Task<string> CreatePat(ApplicationUser user,
            IEnumerable<string> roles,
            IEnumerable<string>? scopes,
            DateTimeOffset? expiration,
            string? description,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = roles;
            DateTime now = DateTime.UtcNow;
            string id = Guid.NewGuid().ToString("N");
            if (expiration == null)
            {
                //If expiration time is not given, it's one year.
                expiration = now.AddYears(1);
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Convert.FromHexString(_options.Value.Key ?? throw new InvalidOperationException("Token signing key is not configured."));
            var claims = new List<Claim>
            {
                //Sub is used to identify the user.
                //This is required by the OAuth2 specification.
                new("sub", user.Id),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(JwtRegisteredClaimNames.Jti, id),
            };
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            var effective = await UserPermissions.ReadEffectiveAsync(connection, user.Id);
            var activeRoles = await UserPermissions.ReadRolesAsync(connection, user.Id);
            if (!User.IsInRole(GXRoles.Admin) || !activeRoles.Contains(GXRoles.Admin, StringComparer.Ordinal))
            {
                // A restricted bearer token must not regain its owner's broader database grants when minting another token.
                effective = [.. effective.Intersect(User.FindAll("Permission").Select(c => c.Value), StringComparer.Ordinal)];
            }
            var requested = scopes?.Distinct(StringComparer.Ordinal).ToArray();
            bool restricted = requested?.Length > 0;
            if (restricted)
            {
                var invalid = requested!.Except(effective, StringComparer.Ordinal).ToArray();
                if (invalid.Length != 0)
                {
                    throw new UnauthorizedAccessException($"Permissions not granted: {string.Join(", ", invalid)}");
                }
            }
            else
            {
                foreach (string role in activeRoles.Where(User.IsInRole))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
            foreach (string permission in restricted ? requested!.AsEnumerable() : effective)
            {
                claims.Add(new Claim("Permission", permission));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                IssuedAt = now,
                NotBefore = now,
                Expires = expiration.Value.UtcDateTime,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
                Audience = "Gurux.DLMS.AMI.ServerAPI",
                Issuer = ServerSettings.ServerAddress,
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var value = tokenHandler.WriteToken(token);
            // Persist only discoverable metadata. Never store the JWT value or its signature.
            var metadata = new GXPersistedGrants
            {
                Id = id,
                Type = "reference_token",
                SubjectId = user.Id,
                SessionId = "",
                ClientId = "Gurux.DLMS.AMI.ServerAPI",
                Description = description ?? "Personal access token",
                CreationTime = now,
                Expiration = token.ValidTo,
                Data = JsonSerializer.Serialize(new
                {
                    Scopes = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToArray()
                })
            };
            await connection.InsertAsync(GXInsertArgs.Insert(metadata), cancellationToken);
            return value;
        }

        /// <inheritdoc />      
        public async Task<GXPersonalToken?> GetPersonalTokenByIdAsync(string id, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXUserPolicies.View);
            ApplicationUser? user = await _userManager.GetUserAsync(User);
            ListTokensResponse ret = new();
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXPersistedGrants>(where => where.Id == id && where.SubjectId == user!.Id && where.Type == "reference_token");
            arg.Distinct = true;
            await using var connection = await _dbFactory.GetHistoryConnectionAsync();
            GXPersistedGrants? it = await connection.SingleOrDefaultAsync<GXPersistedGrants>(arg);
            if (it == null)
            {
                return null;
            }
            return ToPersonalToken(it);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<GXPersonalToken>> GetPersonalTokensAsync(ListTokens? request, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXUserPolicies.View);
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTokenPolicies.View);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            var userId = ServerHelpers.GetUserId(User);
            ListTokensResponse ret = new();
            GXSelectArgs arg = GXSelectArgs.SelectAll<GXPersistedGrants>(where => where.SubjectId == userId && where.Type == "reference_token" && where.ClientId == "Gurux.DLMS.AMI.ServerAPI");
            arg.Distinct = true;
            arg.OrderBy.Add<GXPersistedGrants>(q => q.Description);
            GXDateTimeFilters.Apply<GXPersonalToken, GXPersistedGrants>(arg, request?.DateFilters);
            if (request != null)
            {
                if (!string.IsNullOrWhiteSpace(request.Filter?.Name))
                    {
                    arg.Where.And<GXPersistedGrants>(g => g.Description.Contains(request.Filter.Name));
                }
                if (request.Count != 0)
                {
                    //Return total row count. This can be used for paging.
                    GXSelectArgs total = GXSelectArgs.Select<GXPersistedGrants>(q => GXSql.Count(1));
                    total.Where.Append(arg.Where);
                    ret.Count = connection.SingleOrDefault<long>(total);
                    arg.Index = request.Index;
                    arg.Count = request.Count;
                }
            }
            List<GXPersistedGrants> list = await connection.SelectAsync<GXPersistedGrants>(arg, cancellationToken);
            List<GXPersonalToken> tokens = new(list.Count);
            foreach (var it in list)
            {
                tokens.Add(ToPersonalToken(it));
            }
            return [.. tokens];
        }

        /// <inheritdoc />
        public async Task<GXPersonalToken> RemovePersonalTokenAsync(string id, CancellationToken cancellationToken)
        {
            await _authorizationService.EnsureAnyPolicyAsync(User, GXUserPolicies.View);
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTokenPolicies.Delete);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            var userId = ServerHelpers.GetUserId(User);
            GXSelectArgs s = GXSelectArgs.SelectAll<GXPersistedGrants>(where => where.Id == id && where.SubjectId == userId &&
                where.Type == "reference_token" && where.ClientId == "Gurux.DLMS.AMI.ServerAPI");
            GXPersistedGrants? it = await connection.SingleOrDefaultAsync<GXPersistedGrants>(s) ?? throw new Exception("Unknown token: " + id);
            GXDeleteArgs args = GXDeleteArgs.Delete<GXPersistedGrants>(where => where.Id == id && where.SubjectId == userId &&
                where.Type == "reference_token" && where.ClientId == "Gurux.DLMS.AMI.ServerAPI");
            await connection.DeleteAsync(args, cancellationToken);
            _connections?.Revoke(id);
            return ToPersonalToken(it);
        }

        /// <inheritdoc />
        public async Task<GXPersonalToken> ExtendPersonalTokenAsync(string id, DateTimeOffset expiration, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _authorizationService.EnsureAnyPolicyAsync(User, GXUserPolicies.View);
            await _authorizationService.EnsureAnyPolicyAsync(User, GXTokenPolicies.Edit);
            if (string.IsNullOrWhiteSpace(id) || expiration <= DateTimeOffset.UtcNow)
                {
                throw new ArgumentException("A token identifier and a future expiration are required.");
            }
            var userId = ServerHelpers.GetUserId(User);
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            var metadata = await connection.SingleOrDefaultAsync<GXPersistedGrants>(GXSelectArgs.SelectAll<GXPersistedGrants>(g =>
                g.Id == id && g.SubjectId == userId && g.Type == "reference_token" && g.ClientId == "Gurux.DLMS.AMI.ServerAPI"), cancellationToken)
                ?? throw new InvalidOperationException("The token is unavailable.");
            var previousExpiration = metadata.Expiration;
            var now = DateTime.UtcNow;
            if (GXPatValidator.IsConsumed(connection, metadata.ConsumedTime) || metadata.CreationTime > now || previousExpiration <= now)
                {
                throw new InvalidOperationException("Only an active token can be extended.");
            }
            if (expiration.UtcDateTime <= previousExpiration)
                {
                throw new ArgumentException("The new expiration must be later than the current expiration.");
            }
            metadata.Expiration = expiration.UtcDateTime;
            var unconsumed = metadata.ConsumedTime;
            var update = GXUpdateArgs.Update(metadata, g => g.Expiration);
            // Compare the old deadline so a simultaneous extension can never be shortened.
            update.Where.And<GXPersistedGrants>(g => g.Id == id && g.SubjectId == userId && g.Type == "reference_token" &&
                g.ClientId == "Gurux.DLMS.AMI.ServerAPI" && g.Expiration == previousExpiration && g.ConsumedTime == unconsumed);
            if (await connection.UpdateAsync(update, cancellationToken) != 1)
                {
                throw new InvalidOperationException("The token changed. Reload its current expiration and try again.");
            }
            return ToPersonalToken(metadata);
        }

        private static GXPersonalToken ToPersonalToken(GXPersistedGrants metadata) => new()
        {
            Id = metadata.Id,
            Name = metadata.Description,
            // Database DateTime values may be returned without their UTC Kind.
            CreationTime = new DateTimeOffset(DateTime.SpecifyKind(metadata.CreationTime, DateTimeKind.Utc)),
            Expiration = new DateTimeOffset(DateTime.SpecifyKind(metadata.Expiration, DateTimeKind.Utc)),
            Scopes = ReadScopes(metadata.Data)
        };

        private static string[] ReadScopes(string? data)
        {
            if (string.IsNullOrWhiteSpace(data)) {
                return [];
            }
            try
            {
                using var document = JsonDocument.Parse(data);
                if (document.RootElement.ValueKind != JsonValueKind.Object ||
                    !document.RootElement.TryGetProperty("Scopes", out var scopes) || scopes.ValueKind != JsonValueKind.Array) {
                    return [];
                }
                return scopes.EnumerateArray().Where(s => s.ValueKind == JsonValueKind.String)
                    .Select(s => s.GetString()!).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.Ordinal).ToArray();
            }
            catch (JsonException) { return []; }
        }
    }
}
