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

using System.Security.Claims;
using System.Text.Encodings.Web;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Integrates module credentials with the host authentication pipeline.</summary>
internal sealed class GXModuleAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    IGXModuleAuthenticationRuntime runtime, IGXModulePrincipalValidator validator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string SchemeName = "AmiModule";
    private bool fallback;

    internal static string SelectScheme(HttpContext context) =>
        context.RequestServices.GetRequiredService<IGXModuleAuthenticationRuntime>().CanAuthenticate(context)
            ? SchemeName : GXPatValidator.SelectScheme(context);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var result = await runtime.AuthenticateAsync(Context, Context.RequestAborted);
        if (result.Status == AmiAuthenticationStatus.NotHandled)
        {
            fallback = true;
            return await Context.AuthenticateAsync(GXPatValidator.SelectScheme(Context));
        }
        if (result.Status == AmiAuthenticationStatus.Authenticated && result.Principal != null &&
            await validator.ValidateAsync(result.Principal, Context.RequestAborted))
            {
            return AuthenticateResult.Success(new AuthenticationTicket(result.Principal, SchemeName));
        }
        Context.Features.Set(new GXModuleAuthenticationRejected());
        return AuthenticateResult.Fail("Module authentication rejected.");
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        await HandleAuthenticateOnceAsync();
        if (fallback) {
            await Context.ChallengeAsync(GXPatValidator.SelectScheme(Context), properties);
        }
        else {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        await HandleAuthenticateOnceAsync();
        if (fallback) {
            await Context.ForbidAsync(GXPatValidator.SelectScheme(Context), properties);
        }
        else {
            Response.StatusCode = StatusCodes.Status403Forbidden;
        }
    }
}

/// <summary>Marks a rejected credential even when the selected endpoint permits anonymous access.</summary>
internal sealed class GXModuleAuthenticationRejected;

/// <summary>Prevents rejected module credentials from reaching anonymous endpoints.</summary>
internal sealed class GXModuleAuthenticationGuard(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (context.Features.Get<GXModuleAuthenticationRejected>() == null) {
            return next(context);
        }
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
}
