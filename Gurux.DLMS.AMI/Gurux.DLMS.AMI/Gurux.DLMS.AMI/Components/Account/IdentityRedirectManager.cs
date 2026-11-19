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

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Gurux.DLMS.AMI.Components.Account
{
    /// <summary>Redirects identity account operations while preserving status messages in short-lived cookies.</summary>
    /// <param name="navigationManager">Resolves application-relative redirect destinations.</param>
    internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
    {
        /// <summary>Cookie name used to pass identity status messages across redirects.</summary>
        public const string StatusCookieName = "Identity.StatusMessage";

        /// <summary>Creates short-lived, HTTP-only cookies carrying identity status messages.</summary>
        private static readonly CookieBuilder _statusCookieBuilder = new()
        {
            SameSite = SameSiteMode.Strict,
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromSeconds(5),
        };

        /// <summary>Redirects a statically rendered identity component to an application-local destination.</summary>
        /// <param name="uri">Relative destination, or an absolute application URI converted to a base-relative path.</param>
        /// <exception cref="InvalidOperationException">Navigation returns because the component is not being rendered statically.</exception>
        [DoesNotReturn]
        public void RedirectTo(string? uri)
        {
            uri ??= "";

            // Prevent open redirects.
            if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
            {
                uri = navigationManager.ToBaseRelativePath(uri);
            }

            // During static rendering, NavigateTo throws a NavigationException which is handled by the framework as a redirect.
            // So as long as this is called from a statically rendered Identity component, the InvalidOperationException is never thrown.
            navigationManager.NavigateTo(uri);
            throw new InvalidOperationException($"{nameof(IdentityRedirectManager)} can only be used during static rendering.");
        }

        /// <summary>Replaces a destination's query string with the supplied values and redirects during static rendering.</summary>
        /// <param name="uri">Destination whose path is retained.</param>
        /// <param name="queryParameters">Query values added to the destination path.</param>
        /// <exception cref="InvalidOperationException">The component is not being rendered statically.</exception>
        [DoesNotReturn]
        public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
        {
            var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
            var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
            RedirectTo(newUri);
        }

        /// <summary>Stores a short-lived identity status cookie before redirecting to the destination.</summary>
        /// <param name="uri">Application-local redirect destination.</param>
        /// <param name="message">Status message read by the destination page.</param>
        /// <param name="context">Response receiving the status cookie.</param>
        /// <exception cref="InvalidOperationException">The component is not being rendered statically.</exception>
        [DoesNotReturn]
        public void RedirectToWithStatus(string uri, string message, HttpContext context)
        {
            context.Response.Cookies.Append(StatusCookieName, message, _statusCookieBuilder.Build(context));
            RedirectTo(uri);
        }

        /// <summary>Current absolute page path without query parameters or a fragment.</summary>
        private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

        /// <summary>Redirects to the current page path without retaining its query parameters or fragment.</summary>
        /// <exception cref="InvalidOperationException">The component is not being rendered statically.</exception>
        [DoesNotReturn]
        public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

        /// <summary>Stores an identity status cookie and redirects to the current page without its query string.</summary>
        /// <param name="message">Status message to display after the redirect.</param>
        /// <param name="context">Response receiving the status cookie.</param>
        /// <exception cref="InvalidOperationException">The component is not being rendered statically.</exception>
        [DoesNotReturn]
        public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
            => RedirectToWithStatus(CurrentPath, message, context);
    }
}
