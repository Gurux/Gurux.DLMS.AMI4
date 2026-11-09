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
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>
/// Retrieves authorization policies and their requirements.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class PolicyController(IPolicyRepository policyRepository, IAuthorizationService authorization,
    IDbConnectionFactory dbFactory) : ControllerBase
{
    /// <summary>Creates or updates policies with operation-specific authorization in the repository.</summary>
    [HttpPost("Add")]
    [HttpPost("Update")]
    [Authorize]
    public async Task<ActionResult<UpdatePolicyResponse>> Post(UpdatePolicy request, CancellationToken cancellationToken = default)
    {
        if (request.Policys?.Any() != true)
        {
            return BadRequest("No policies to save.");
        }
        return new UpdatePolicyResponse { Ids = await policyRepository.UpsertAsync(request.Policys, cancellationToken) };
    }

    /// <summary>Removes custom policies that are no longer referenced.</summary>
    [HttpPost("Delete")]
    [Authorize(Policy = GXPolicyPolicies.Delete)]
    public async Task<ActionResult<PolicyDeleteResponse>> Post(PolicyDelete request, CancellationToken cancellationToken = default)
    {
        if (request.Ids?.Any() != true)
        {
            return BadRequest("No policies to remove.");
        }
        var ids = new List<Guid>();
        foreach (var value in request.Ids)
        {
            if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
            {
                return BadRequest("Invalid policy identifier.");
            }
            ids.Add(id);
        }
        await policyRepository.DeleteAsync(ids, cancellationToken);
        return new PolicyDeleteResponse();
    }

    /// <summary>
    /// Lists a filtered page of policies.
    /// </summary>
    [HttpPost("List")]
    [Authorize(Policy = GXPolicyPolicies.View)]
    public async Task<ActionResult<ListPolicysResponse>> Post(ListPolicys? request,
        CancellationToken cancellationToken = default)
    {
        var response = new ListPolicysResponse();
        response.Policys = await policyRepository.ListAsync(request, response, cancellationToken);
        return response;
    }

    /// <summary>
    /// Reads a policy with its requirements.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = GXPolicyPolicies.View)]
    public async Task<ActionResult<GetPolicyResponse>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return new GetPolicyResponse { Item = await policyRepository.ReadAsync(id, null, cancellationToken) };
    }

    /// <summary>Lists active policies the signed-in user can satisfy.</summary>
    [HttpGet("Available")]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<string[]>> Available(CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        await using var connection = await dbFactory.GetConnectionAsync(cancellationToken);
        var policies = await connection.SelectAsync<GXPolicy>(
            GXSelectArgs.Select<GXPolicy>(p => p.Name, p => p.Removed == null), cancellationToken);
        var names = new List<string>();
        foreach (var policy in policies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(policy.Name)
                && (await authorization.AuthorizeAsync(User, null, policy.Name)).Succeeded)
            {
                names.Add(policy.Name);
            }
        }
        return names.Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Performs the check operation.</summary>
    /// <param name="request">The request parameters.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    [HttpPost("Check")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<CheckPoliciesResponse>> Check(CheckPolicies request, CancellationToken cancellationToken = default)
    {
        if (request.PolicyNames == null || request.PolicyNames.Length is < 1 or > 64
            || request.PolicyNames.Any(name => string.IsNullOrWhiteSpace(name) || name.Length > 128))
        {
            return BadRequest("Provide 1-64 policy names, each containing 1-128 characters.");
        }

        var response = new CheckPoliciesResponse();
        foreach (var name in request.PolicyNames.Distinct(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            response.Results[name] = (await authorization.AuthorizeAsync(User, null, name)).Succeeded;
        }
        return response;
    }

    /// <summary>
    /// Regenerates policy items based on the specified identifiers.
    /// </summary>
    /// <param name="request">The request containing the identifiers of the policy items to regenerate.</param>
    /// <returns>A response indicating the result of the regeneration operation.</returns>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    [HttpPost("Regenerate")]
    [Authorize(Policy = GXPolicyPolicies.Edit)]
    public async Task<ActionResult<RegeneratePolicyResponse>> Post(RegeneratePolicy request, CancellationToken cancellationToken = default)
    {
        if (request.Ids?.Any() == true)
        {
            await policyRepository.ResetToDefaultsAsync(request.Ids, cancellationToken);
        }
        else
        {
            await policyRepository.EnsureDefaultsAsync(cancellationToken);
        }
        return new RegeneratePolicyResponse();
    }
}
